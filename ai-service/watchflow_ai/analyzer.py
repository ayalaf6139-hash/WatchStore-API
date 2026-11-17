import json
from datetime import date
from typing import Any, Callable, Optional, Protocol

from langchain_core.language_models import BaseChatModel
from langchain_core.messages import AIMessage, AnyMessage, HumanMessage, SystemMessage

from watchflow_ai.prompts import get_analyze_prompt
from watchflow_ai.schemas import Requirements, TurnAnalysis
from watchflow_ai.state import ConversationState

TRANSCRIPT_LIMIT = 12


class Analyzer(Protocol):
    def analyze(self, state: ConversationState) -> TurnAnalysis:
        ...


def _describe_slots(slots: list[dict[str, Any]]) -> str:
    if not slots:
        return "none"
    return "; ".join(f"{index}. {slot['start_local']}" for index, slot in enumerate(slots, start=1))


def build_analysis_messages(
    prompt_version: str,
    today: date,
    transcript: list[AnyMessage],
    requirements: Optional[Requirements],
    offered_slots: list[dict[str, Any]],
    pending_slot: Optional[dict[str, Any]],
) -> list[AnyMessage]:
    known = requirements.model_dump(mode="json", exclude_defaults=True) if requirements else {}
    system = get_analyze_prompt(prompt_version).format(
        today=f"{today.isoformat()} ({today.strftime('%A')})",
        known_requirements=json.dumps(known, ensure_ascii=False) if known else "nothing yet",
        offered_slots=_describe_slots(offered_slots),
        pending_slot=pending_slot["start_local"] if pending_slot else "none",
    )

    history: list[AnyMessage] = []
    for message in transcript[-TRANSCRIPT_LIMIT:]:
        if isinstance(message, HumanMessage):
            history.append(HumanMessage(content=message.content))
        elif isinstance(message, AIMessage):
            speaker = "Manager" if message.name == "manager" else "Assistant"
            history.append(AIMessage(content=f"[{speaker}] {message.content}"))

    return [SystemMessage(content=system), *history]


class LlmAnalyzer:
    def __init__(
        self,
        model: BaseChatModel,
        prompt_version: str = "v2",
        today: Callable[[], date] = date.today,
    ) -> None:
        get_analyze_prompt(prompt_version)
        self._structured = model.with_structured_output(TurnAnalysis)
        self._prompt_version = prompt_version
        self._today = today

    def analyze(self, state: ConversationState) -> TurnAnalysis:
        messages = build_analysis_messages(
            self._prompt_version,
            self._today(),
            state.get("messages", []),
            state.get("requirements"),
            state.get("offered_slots", []),
            state.get("pending_slot"),
        )
        result = self._structured.invoke(messages)
        if isinstance(result, dict):
            return TurnAnalysis.model_validate(result)
        return result
