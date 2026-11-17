from typing import Any, Callable

FIELDS = (
    "intent",
    "appointment_purpose",
    "budget_max",
    "preferred_brands",
    "selected_slot_number",
    "confirmation",
)


def _same(field: str, actual: Any, expected: Any) -> bool:
    if field == "budget_max":
        if actual is None or expected is None:
            return actual is None and expected is None
        return abs(float(actual) - float(expected)) <= 1
    if field == "preferred_brands":
        return {b.strip().lower() for b in actual or []} == {b.strip().lower() for b in expected or []}
    return actual == expected


def _field_evaluator(field: str, key: str) -> Callable[[dict, dict], dict]:
    def evaluator(outputs: dict, reference_outputs: dict) -> dict:
        return {"key": key, "score": int(_same(field, outputs.get(field), reference_outputs.get(field)))}

    evaluator.__name__ = key
    return evaluator


intent_correct = _field_evaluator("intent", "intent_correct")
purpose_correct = _field_evaluator("appointment_purpose", "purpose_correct")
budget_correct = _field_evaluator("budget_max", "budget_correct")
brands_correct = _field_evaluator("preferred_brands", "brands_correct")
slot_correct = _field_evaluator("selected_slot_number", "slot_correct")
confirmation_correct = _field_evaluator("confirmation", "confirmation_correct")


def all_fields_correct(outputs: dict, reference_outputs: dict) -> dict:
    score = all(_same(field, outputs.get(field), reference_outputs.get(field)) for field in FIELDS)
    return {"key": "all_fields_correct", "score": int(score)}


EVALUATORS = [
    intent_correct,
    purpose_correct,
    budget_correct,
    brands_correct,
    slot_correct,
    confirmation_correct,
    all_fields_correct,
]
