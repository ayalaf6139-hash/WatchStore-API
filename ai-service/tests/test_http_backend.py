from datetime import date

import httpx
import pytest

from watchflow_ai.backend import BackendError, HttpBackendClient, SlotUnavailable

SLOT = {"startUtc": "2027-03-07T07:00:00Z", "endUtc": "2027-03-07T07:30:00Z",
        "startLocal": "2027-03-07T09:00:00+02:00", "endLocal": "2027-03-07T09:30:00+02:00", "durationMinutes": 30}


def make_client(handler):
    return HttpBackendClient("http://api.test/api", "admin", "secret", transport=httpx.MockTransport(handler))


def with_login(routes):
    calls = []

    def handler(request: httpx.Request) -> httpx.Response:
        calls.append(request)
        if request.url.path == "/api/auth/login":
            return httpx.Response(200, json={"token": "jwt-token"})
        assert request.headers["Authorization"] == "Bearer jwt-token"
        return routes(request)

    return handler, calls


def test_lookup_returns_none_on_404():
    handler, _ = with_login(lambda request: httpx.Response(404))
    assert make_client(handler).find_customer("050-1234567") is None


def test_lookup_maps_customer_fields():
    handler, calls = with_login(lambda request: httpx.Response(200, json={"id": 7, "fullName": "רחל", "customerType": "Collector"}))

    customer = make_client(handler).find_customer("050-1234567")

    assert customer == {"id": 7, "full_name": "רחל", "customer_type": "Collector"}
    assert calls[-1].url.params["phone"] == "050-1234567"


def test_availability_sends_query_and_maps_slots():
    def routes(request):
        assert request.url.path == "/api/appointments/availability"
        assert request.url.params["fromDate"] == "2027-03-07"
        assert request.url.params["requiresSpecialPreparation"] == "false"
        return httpx.Response(200, json={"slots": [SLOT]})

    handler, _ = with_login(routes)
    slots = make_client(handler).get_availability(1, date(2027, 3, 7), "Private", False, 3)

    assert slots == [{"start_utc": "2027-03-07T07:00:00Z", "start_local": "2027-03-07T09:00:00+02:00", "duration_minutes": 30}]


def test_booking_conflict_raises_slot_unavailable_with_alternatives():
    handler, _ = with_login(lambda request: httpx.Response(409, json={"status": 409, "message": "taken", "alternatives": [SLOT]}))

    with pytest.raises(SlotUnavailable) as error:
        make_client(handler).book_appointment(1, 1, "2027-03-07T08:00:00Z", False, "key-1", None)

    assert error.value.alternatives[0]["start_utc"] == "2027-03-07T07:00:00Z"


def test_server_error_raises_backend_error():
    handler, _ = with_login(lambda request: httpx.Response(500, json={"message": "boom"}))

    with pytest.raises(BackendError):
        make_client(handler).book_appointment(1, 1, "2027-03-07T08:00:00Z", False, "key-1", None)


def test_products_never_expose_wholesale_price():
    item = {"id": 1, "modelNumber": "ROLEX-001", "name": "Submariner Date", "brand": "Rolex", "categoryName": "Luxury",
            "wholesalePrice": 32000, "recommendedRetailPrice": 45000, "stockQuantity": 1, "isActive": True}
    handler, _ = with_login(lambda request: httpx.Response(200, json={"items": [item]}))

    products = make_client(handler).search_products("Rolex")

    assert products == [{"name": "Submariner Date", "brand": "Rolex", "model_number": "ROLEX-001", "price": 45000, "in_stock": True}]
    assert "wholesale" not in str(products).lower()


def test_expired_token_triggers_single_relogin():
    state = {"logins": 0, "requests": 0}

    def handler(request):
        if request.url.path == "/api/auth/login":
            state["logins"] += 1
            return httpx.Response(200, json={"token": "jwt-token"})
        state["requests"] += 1
        return httpx.Response(401) if state["requests"] == 1 else httpx.Response(404)

    assert make_client(handler).find_customer("050") is None
    assert state == {"logins": 2, "requests": 2}
