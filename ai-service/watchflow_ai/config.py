import os
from dataclasses import dataclass, field

import truststore
from dotenv import load_dotenv

from watchflow_ai.schemas import AppointmentPurpose


def _default_purpose_mapping() -> dict[AppointmentPurpose, int]:
    return {
        AppointmentPurpose.CONSULTATION: 1,
        AppointmentPurpose.LUXURY_PURCHASE: 2,
        AppointmentPurpose.REPAIR: 3,
        AppointmentPurpose.STRAP_FITTING: 4,
    }


@dataclass(frozen=True)
class Settings:
    llm_provider: str = "google"
    llm_model: str = "gemini-flash-lite-latest"
    google_api_key: str = ""
    prompt_version: str = "v2"
    api_url: str = "http://localhost:5140/api"
    api_username: str = "admin"
    api_password: str = ""
    max_turns: int = 30
    max_followups: int = 4
    slots_to_offer: int = 3
    purpose_to_appointment_type: dict[AppointmentPurpose, int] = field(default_factory=_default_purpose_mapping)

    @classmethod
    def from_env(cls) -> "Settings":
        truststore.inject_into_ssl()
        load_dotenv()
        return cls(
            llm_provider=os.getenv("LLM_PROVIDER", "google"),
            llm_model=os.getenv("LLM_MODEL", "gemini-flash-lite-latest"),
            google_api_key=os.getenv("GOOGLE_API_KEY", ""),
            prompt_version=os.getenv("PROMPT_VERSION", "v2"),
            api_url=os.getenv("WATCHFLOW_API_URL", "http://localhost:5140/api"),
            api_username=os.getenv("WATCHFLOW_API_USERNAME", "admin"),
            api_password=os.getenv("WATCHFLOW_API_PASSWORD", ""),
        )
