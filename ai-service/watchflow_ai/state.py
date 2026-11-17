from typing import Annotated, Any, Optional, TypedDict

from langchain_core.messages import AnyMessage
from langgraph.graph.message import add_messages

from watchflow_ai.schemas import Requirements, TurnAnalysis


class ConversationState(TypedDict, total=False):
    messages: Annotated[list[AnyMessage], add_messages]
    caller_phone: str
    customer: Optional[dict[str, Any]]
    requirements: Requirements
    last_analysis: Optional[TurnAnalysis]
    missing_fields: list[str]
    offered_slots: list[dict[str, Any]]
    pending_slot: Optional[dict[str, Any]]
    booking: Optional[dict[str, Any]]
    mode: str
    followup_count: int
    turn_count: int
    summary: Optional[str]
