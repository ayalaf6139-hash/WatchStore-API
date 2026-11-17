from datetime import datetime
from typing import Any, Optional

BUSINESS_NAME = "Friedman Watches"

HEBREW_WEEKDAYS = ["שני", "שלישי", "רביעי", "חמישי", "שישי", "שבת", "ראשון"]

MISSING_FIELD_QUESTIONS = {
    "full_name": "כדי לקבוע תור, איך קוראים לך?",
    "appointment_purpose": "לאיזו פגישה תרצו להגיע? ייעוץ ובחירת שעון, רכישת שעון יוקרה, מסירת שעון לתיקון או התאמת רצועה?",
    "budget_max": "כדי שנכין לפגישה דגמים מתאימים, מה התקציב בערך?",
}


def format_slot(slot: dict[str, Any]) -> str:
    start = datetime.fromisoformat(slot["start_local"])
    return f"יום {HEBREW_WEEKDAYS[start.weekday()]} {start:%d/%m} בשעה {start:%H:%M}"


def greeting(customer: Optional[dict[str, Any]]) -> str:
    if customer and customer.get("full_name"):
        return f"שלום {customer['full_name']}, הגעת ל-{BUSINESS_NAME}. איך אפשר לעזור?"
    return f"שלום, הגעת ל-{BUSINESS_NAME}. איך אפשר לעזור?"


def ask_missing(field: str) -> str:
    return MISSING_FIELD_QUESTIONS[field]


def offer_slots(slots: list[dict[str, Any]]) -> str:
    lines = [f"{index}. {format_slot(slot)}" for index, slot in enumerate(slots, start=1)]
    return "אלה המועדים הפנויים הקרובים:\n" + "\n".join(lines) + "\nאיזה מועד מתאים לכם?"


def no_slots() -> str:
    return "לא מצאתי מועד פנוי בימים הקרובים. אעביר את השיחה למנהל כדי שימצא לכם מועד."


def choose_slot_again(slots: list[dict[str, Any]]) -> str:
    return "לא הצלחתי להבין איזה מועד בחרתם. " + offer_slots(slots)


def confirm_slot(slot: dict[str, Any]) -> str:
    return f"לאשר תור ל{format_slot(slot)}?"


def booked(appointment: dict[str, Any]) -> str:
    return f"התור נקבע ל{format_slot(appointment)}, למשך {appointment['duration_minutes']} דקות. נתראה!"


def slot_taken(alternatives: list[dict[str, Any]]) -> str:
    if not alternatives:
        return "המועד שבחרתם נתפס הרגע, ולא מצאתי מועד חלופי. אעביר את השיחה למנהל."
    return "המועד שבחרתם נתפס הרגע. " + offer_slots(alternatives)


def booking_failed() -> str:
    return "הייתה תקלה בקביעת התור, והתור לא נקבע. אעביר את השיחה למנהל."


def transfer_to_manager() -> str:
    return "אני מעביר את השיחה למנהל. כל מה שסיפרתם נשמר אצלו."


def back_from_manager() -> str:
    return "חזרתי לשיחה. במה עוד אפשר לעזור?"


def product_results(products: list[dict[str, Any]]) -> str:
    lines = []
    for product in products:
        stock = "במלאי" if product["in_stock"] else "לא במלאי כרגע"
        lines.append(f"- {product['brand']} {product['name']}: {product['price']:,.0f} ש\"ח, {stock}")
    return "הנה מה שמצאתי בקטלוג:\n" + "\n".join(lines) + "\nתרצו לקבוע פגישה כדי לראות אותם?"


def product_unknown() -> str:
    return "אין לי מידע מדויק על זה בקטלוג. אעביר את השאלה למנהל."


def clarify() -> str:
    return "לא בטוח שהבנתי. אפשר לקבוע פגישה, לשאול על שעון מסוים או לדבר עם המנהל."


def goodbye() -> str:
    return f"תודה שפניתם ל-{BUSINESS_NAME}. יום טוב!"


def too_long() -> str:
    return "השיחה ארוכה מהרגיל, אז אעביר אותה למנהל כדי שיעזור לכם."
