from datetime import date
from enum import Enum
from typing import Literal, Optional

from pydantic import BaseModel, Field


class Intent(str, Enum):
    BOOK_APPOINTMENT = "book_appointment"
    PRODUCT_QUESTION = "product_question"
    TALK_TO_HUMAN = "talk_to_human"
    END_CONVERSATION = "end_conversation"
    OTHER = "other"


class AppointmentPurpose(str, Enum):
    CONSULTATION = "consultation"
    LUXURY_PURCHASE = "luxury_purchase"
    REPAIR = "repair"
    STRAP_FITTING = "strap_fitting"


class CustomerType(str, Enum):
    PRIVATE = "Private"
    COLLECTOR = "Collector"
    CORPORATE = "Corporate"


REQUIREMENT_FIELDS = (
    "full_name",
    "appointment_purpose",
    "budget_max",
    "preferred_brands",
    "features",
    "customer_type",
    "special_preparation",
    "preferred_date",
)


class Requirements(BaseModel):
    full_name: Optional[str] = Field(default=None, description="Customer's name, only if the customer said it.")
    appointment_purpose: Optional[AppointmentPurpose] = Field(default=None, description="Why the customer wants to come in.")
    budget_max: Optional[float] = Field(default=None, description="Maximum budget in ILS as a plain number.")
    preferred_brands: list[str] = Field(default_factory=list, description="Watch brands in English, e.g. Rolex, Casio.")
    features: list[str] = Field(default_factory=list, description="Requested watch characteristics, e.g. diving, automatic.")
    customer_type: Optional[CustomerType] = Field(default=None, description="Collector or Corporate only when clearly stated.")
    special_preparation: Optional[bool] = Field(default=None, description="True if the manager must prepare specific models in advance.")
    preferred_date: Optional[date] = Field(default=None, description="Requested day for the appointment, ISO date.")
    inferred_fields: list[str] = Field(default_factory=list, description="Names of fields above that were inferred rather than stated explicitly.")


class TurnAnalysis(BaseModel):
    intent: Intent = Field(description="What the customer wants in their last message.")
    requirements: Requirements = Field(default_factory=Requirements, description="Only information found in the last customer message.")
    selected_slot_number: Optional[int] = Field(default=None, description="1-based number of the offered slot the customer chose, if any.")
    confirmation: Optional[Literal["yes", "no"]] = Field(default=None, description="Explicit answer to a pending confirmation question.")
    product_query: Optional[str] = Field(default=None, description="Short search text for a product question, e.g. 'G-Shock'.")


def merge_requirements(current: Requirements, update: Requirements) -> Requirements:
    merged = current.model_copy(deep=True)
    inferred = set(current.inferred_fields)

    for name in REQUIREMENT_FIELDS:
        value = getattr(update, name)
        if isinstance(value, list):
            if not value:
                continue
            existing = getattr(merged, name)
            combined = existing + [item for item in value if item.lower() not in {e.lower() for e in existing}]
            setattr(merged, name, combined)
        elif value is not None:
            setattr(merged, name, value)
        else:
            continue

        if name in update.inferred_fields:
            inferred.add(name)
        else:
            inferred.discard(name)

    merged.inferred_fields = sorted(inferred)
    return merged
