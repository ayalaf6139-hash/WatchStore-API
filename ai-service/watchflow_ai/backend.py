import re
from datetime import date, datetime, timedelta, timezone
from typing import Any, Optional, Protocol

import httpx


class BackendError(Exception):
    pass


class SlotUnavailable(Exception):
    def __init__(self, message: str, alternatives: list[dict[str, Any]]) -> None:
        super().__init__(message)
        self.alternatives = alternatives


class BackendClient(Protocol):
    def find_customer(self, phone: str) -> Optional[dict[str, Any]]:
        ...

    def create_customer(self, phone: str, full_name: Optional[str], customer_type: str) -> dict[str, Any]:
        ...

    def get_availability(
        self,
        appointment_type_id: int,
        from_date: date,
        customer_type: str,
        special_preparation: bool,
        max_results: int,
    ) -> list[dict[str, Any]]:
        ...

    def book_appointment(
        self,
        customer_id: int,
        appointment_type_id: int,
        start_utc: str,
        special_preparation: bool,
        idempotency_key: str,
        customer_notes: Optional[str],
    ) -> dict[str, Any]:
        ...

    def search_products(self, query: str) -> list[dict[str, Any]]:
        ...


def _slot_from_api(slot: dict[str, Any]) -> dict[str, Any]:
    return {
        "start_utc": slot["startUtc"],
        "start_local": slot["startLocal"],
        "duration_minutes": slot["durationMinutes"],
    }


def _customer_from_api(customer: dict[str, Any]) -> dict[str, Any]:
    return {
        "id": customer["id"],
        "full_name": customer.get("fullName"),
        "customer_type": customer.get("customerType", "Private"),
    }


def _product_from_api(product: dict[str, Any]) -> dict[str, Any]:
    return {
        "name": product["name"],
        "brand": product["brand"],
        "model_number": product["modelNumber"],
        "price": product["recommendedRetailPrice"],
        "in_stock": product.get("stockQuantity", 0) > 0,
    }


class HttpBackendClient:
    def __init__(
        self,
        base_url: str,
        username: str,
        password: str,
        transport: Optional[httpx.BaseTransport] = None,
        timeout_seconds: float = 10.0,
    ) -> None:
        self._client = httpx.Client(base_url=base_url.rstrip("/") + "/", timeout=timeout_seconds, transport=transport)
        self._username = username
        self._password = password
        self._token: Optional[str] = None

    def _login(self) -> None:
        response = self._client.post("auth/login", json={"username": self._username, "password": self._password})
        if response.status_code != 200:
            raise BackendError(f"Login to WatchFlow API failed with status {response.status_code}.")
        self._token = response.json()["token"]

    def _request(self, method: str, path: str, **kwargs: Any) -> httpx.Response:
        if self._token is None:
            self._login()

        response = self._client.request(method, path, headers={"Authorization": f"Bearer {self._token}"}, **kwargs)

        if response.status_code == 401:
            self._login()
            response = self._client.request(method, path, headers={"Authorization": f"Bearer {self._token}"}, **kwargs)

        return response

    @staticmethod
    def _ensure_success(response: httpx.Response) -> None:
        if response.is_success:
            return
        try:
            message = response.json().get("message", response.text)
        except ValueError:
            message = response.text
        raise BackendError(f"WatchFlow API returned {response.status_code}: {message}")

    def find_customer(self, phone: str) -> Optional[dict[str, Any]]:
        response = self._request("GET", "customers/lookup", params={"phone": phone})
        if response.status_code == 404:
            return None
        self._ensure_success(response)
        return _customer_from_api(response.json())

    def create_customer(self, phone: str, full_name: Optional[str], customer_type: str) -> dict[str, Any]:
        response = self._request(
            "POST",
            "customers",
            json={"phoneNumber": phone, "fullName": full_name, "customerType": customer_type},
        )
        self._ensure_success(response)
        return _customer_from_api(response.json())

    def get_availability(
        self,
        appointment_type_id: int,
        from_date: date,
        customer_type: str,
        special_preparation: bool,
        max_results: int,
    ) -> list[dict[str, Any]]:
        response = self._request(
            "GET",
            "appointments/availability",
            params={
                "appointmentTypeId": appointment_type_id,
                "fromDate": from_date.isoformat(),
                "customerType": customer_type,
                "requiresSpecialPreparation": str(special_preparation).lower(),
                "maxResults": max_results,
            },
        )
        self._ensure_success(response)
        return [_slot_from_api(slot) for slot in response.json()["slots"]]

    def book_appointment(
        self,
        customer_id: int,
        appointment_type_id: int,
        start_utc: str,
        special_preparation: bool,
        idempotency_key: str,
        customer_notes: Optional[str],
    ) -> dict[str, Any]:
        response = self._request(
            "POST",
            "appointments",
            json={
                "customerId": customer_id,
                "appointmentTypeId": appointment_type_id,
                "startUtc": start_utc,
                "requiresSpecialPreparation": special_preparation,
                "idempotencyKey": idempotency_key,
                "customerNotes": customer_notes,
            },
        )

        if response.status_code == 409:
            body = response.json()
            raise SlotUnavailable(body.get("message", "Slot unavailable"), [_slot_from_api(s) for s in body.get("alternatives", [])])

        self._ensure_success(response)
        appointment = response.json()
        return {
            "id": appointment["id"],
            "start_utc": appointment["startUtc"],
            "start_local": appointment["startLocal"],
            "duration_minutes": appointment["durationMinutes"],
            "appointment_type_name": appointment["appointmentTypeName"],
        }

    def search_products(self, query: str) -> list[dict[str, Any]]:
        response = self._request("GET", "products", params={"search": query, "pageSize": 5})
        self._ensure_success(response)
        return [_product_from_api(product) for product in response.json()["items"]]


def normalize_phone(phone: str) -> str:
    digits = re.sub(r"\D", "", phone)
    if digits.startswith("00"):
        digits = digits[2:]
    if digits.startswith("0"):
        digits = "972" + digits[1:]
    return "+" + digits


class InMemoryBackend:
    def __init__(self, first_slot_local: datetime, slot_count: int = 12, slot_minutes: int = 30) -> None:
        self.customers: dict[str, dict[str, Any]] = {}
        self.appointments: list[dict[str, Any]] = []
        self.products: list[dict[str, Any]] = []
        self.taken: set[str] = set()
        self._idempotency: dict[str, dict[str, Any]] = {}
        self._slots = [
            {
                "start_utc": (first_slot_local + timedelta(minutes=slot_minutes * i)).astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
                "start_local": (first_slot_local + timedelta(minutes=slot_minutes * i)).isoformat(),
                "duration_minutes": slot_minutes,
            }
            for i in range(slot_count)
        ]

    def add_customer(self, phone: str, full_name: Optional[str], customer_type: str = "Private") -> dict[str, Any]:
        customer = {"id": len(self.customers) + 1, "full_name": full_name, "customer_type": customer_type}
        self.customers[normalize_phone(phone)] = customer
        return customer

    def find_customer(self, phone: str) -> Optional[dict[str, Any]]:
        return self.customers.get(normalize_phone(phone))

    def create_customer(self, phone: str, full_name: Optional[str], customer_type: str) -> dict[str, Any]:
        if normalize_phone(phone) in self.customers:
            raise BackendError("Customer already exists.")
        return self.add_customer(phone, full_name, customer_type)

    def _free_slots(self) -> list[dict[str, Any]]:
        return [slot for slot in self._slots if slot["start_utc"] not in self.taken]

    def get_availability(
        self,
        appointment_type_id: int,
        from_date: date,
        customer_type: str,
        special_preparation: bool,
        max_results: int,
    ) -> list[dict[str, Any]]:
        return self._free_slots()[:max_results]

    def book_appointment(
        self,
        customer_id: int,
        appointment_type_id: int,
        start_utc: str,
        special_preparation: bool,
        idempotency_key: str,
        customer_notes: Optional[str],
    ) -> dict[str, Any]:
        if idempotency_key in self._idempotency:
            return self._idempotency[idempotency_key]

        slot = next((s for s in self._free_slots() if s["start_utc"] == start_utc), None)
        if slot is None:
            raise SlotUnavailable("Slot unavailable", self._free_slots()[:3])

        self.taken.add(start_utc)
        appointment = {
            "id": len(self.appointments) + 1,
            "customer_id": customer_id,
            "appointment_type_id": appointment_type_id,
            "start_utc": slot["start_utc"],
            "start_local": slot["start_local"],
            "duration_minutes": slot["duration_minutes"],
            "appointment_type_name": "פגישה",
        }
        self.appointments.append(appointment)
        self._idempotency[idempotency_key] = appointment
        return appointment

    def search_products(self, query: str) -> list[dict[str, Any]]:
        needle = query.lower()
        return [p for p in self.products if needle in p["name"].lower() or needle in p["brand"].lower()]
