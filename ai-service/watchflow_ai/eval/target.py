from datetime import date
from typing import Any

from langchain_core.messages import AIMessage, AnyMessage, HumanMessage

from watchflow_ai.analyzer import Analyzer
from watchflow_ai.schemas import Requirements, TurnAnalysis


def state_from_inputs(inputs: dict[str, Any]) -> dict[str, Any]:
    messages: list[AnyMessage] = []
    for line in inputs.get("history", []):
        if line["role"] == "customer":
            messages.append(HumanMessage(content=line["text"]))
        else:
            messages.append(AIMessage(content=line["text"]))
    messages.append(HumanMessage(content=inputs["message"]))

    return {
        "messages": messages,
        "requirements": Requirements.model_validate(inputs.get("known_requirements") or {}),
        "offered_slots": inputs.get("offered_slots") or [],
        "pending_slot": inputs.get("pending_slot"),
    }


def outputs_from_analysis(analysis: TurnAnalysis) -> dict[str, Any]:
    requirements = analysis.requirements
    return {
        "intent": analysis.intent.value,
        "appointment_purpose": requirements.appointment_purpose.value if requirements.appointment_purpose else None,
        "budget_max": requirements.budget_max,
        "preferred_brands": requirements.preferred_brands,
        "selected_slot_number": analysis.selected_slot_number,
        "confirmation": analysis.confirmation,
    }


def today_from_inputs(inputs: dict[str, Any]) -> date:
    return date.fromisoformat(inputs["today"])


def run_analysis(analyzer: Analyzer, inputs: dict[str, Any]) -> dict[str, Any]:
    return outputs_from_analysis(analyzer.analyze(state_from_inputs(inputs)))
