from datetime import date
from typing import Any, Callable, Optional

from langchain_core.messages import AIMessage, HumanMessage
from langchain_core.runnables import RunnableConfig
from langgraph.checkpoint.base import BaseCheckpointSaver
from langgraph.checkpoint.memory import InMemorySaver
from langgraph.graph import END, START, StateGraph
from langgraph.types import interrupt

from watchflow_ai import replies
from watchflow_ai.analyzer import Analyzer
from watchflow_ai.backend import BackendClient, BackendError, SlotUnavailable
from watchflow_ai.config import Settings
from watchflow_ai.schemas import AppointmentPurpose, Intent, Requirements, merge_requirements
from watchflow_ai.state import ConversationState

TRANSFER = "transfer_requested"
COMPLETED = "completed"
AI = "ai"

PURPOSES_NEEDING_BUDGET = {AppointmentPurpose.CONSULTATION, AppointmentPurpose.LUXURY_PURCHASE}


def requirements_notes(requirements: Requirements) -> Optional[str]:
    parts = []
    if requirements.budget_max is not None:
        parts.append(f"תקציב עד {requirements.budget_max:,.0f} ש\"ח")
    if requirements.preferred_brands:
        parts.append("מותגים: " + ", ".join(requirements.preferred_brands))
    if requirements.features:
        parts.append("מאפיינים: " + ", ".join(requirements.features))
    if requirements.special_preparation:
        parts.append("נדרשת הכנה מיוחדת")
    return "; ".join(parts) or None


def build_summary(state: ConversationState) -> str:
    requirements = state.get("requirements") or Requirements()
    customer = state.get("customer") or {}
    name = customer.get("full_name") or requirements.full_name or "לקוח חדש"
    lines = [f"לקוח: {name} ({state.get('caller_phone', '')})"]
    if requirements.appointment_purpose:
        lines.append(f"מטרה: {requirements.appointment_purpose.value}")
    notes = requirements_notes(requirements)
    if notes:
        lines.append(notes)
    if requirements.inferred_fields:
        lines.append("הוסק ולא נאמר במפורש: " + ", ".join(requirements.inferred_fields))
    booking = state.get("booking")
    lines.append(f"תור: {replies.format_slot(booking)}" if booking else "תור: לא נקבע")
    return "\n".join(lines)


def build_graph(
    analyzer: Analyzer,
    backend: BackendClient,
    settings: Optional[Settings] = None,
    checkpointer: Optional[BaseCheckpointSaver] = None,
    today: Callable[[], date] = date.today,
):
    settings = settings or Settings()

    def say(text: str) -> AIMessage:
        return AIMessage(content=text)

    def last_ai_text(state: ConversationState) -> str:
        for message in reversed(state.get("messages", [])):
            if isinstance(message, AIMessage):
                return str(message.content)
        return ""

    def identify_customer(state: ConversationState) -> dict[str, Any]:
        return {
            "customer": backend.find_customer(state["caller_phone"]),
            "requirements": Requirements(),
            "mode": AI,
            "turn_count": 0,
            "followup_count": 0,
            "missing_fields": [],
            "offered_slots": [],
            "pending_slot": None,
            "booking": None,
            "summary": None,
        }

    def greet(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.greeting(state.get("customer")))]}

    def wait_for_customer(state: ConversationState) -> dict[str, Any]:
        event = interrupt({"type": "customer_turn", "reply": last_ai_text(state)})
        if event.get("type") == "manager_takeover":
            return {"mode": TRANSFER}
        return {
            "messages": [HumanMessage(content=event["text"])],
            "turn_count": state.get("turn_count", 0) + 1,
        }

    def route_after_customer(state: ConversationState) -> str:
        if state.get("mode") == TRANSFER:
            return "manager_takeover"
        if state.get("turn_count", 0) > settings.max_turns:
            return "too_long"
        return "analyze"

    def analyze(state: ConversationState) -> dict[str, Any]:
        analysis = analyzer.analyze(state)
        current = state.get("requirements") or Requirements()
        return {"last_analysis": analysis, "requirements": merge_requirements(current, analysis.requirements)}

    def route_intent(state: ConversationState) -> str:
        intent = state["last_analysis"].intent
        in_booking_flow = bool(state.get("offered_slots")) or state.get("pending_slot") is not None

        if intent == Intent.TALK_TO_HUMAN:
            return "request_transfer"
        if intent == Intent.END_CONVERSATION:
            return "finish"
        if intent == Intent.PRODUCT_QUESTION and not in_booking_flow:
            return "answer_product"
        if intent == Intent.BOOK_APPOINTMENT or in_booking_flow:
            return "check_requirements"
        return "clarify"

    def check_requirements(state: ConversationState) -> dict[str, Any]:
        requirements = state["requirements"]
        missing = []
        if not state.get("customer") and not requirements.full_name:
            missing.append("full_name")
        if requirements.appointment_purpose is None:
            missing.append("appointment_purpose")
        elif requirements.appointment_purpose in PURPOSES_NEEDING_BUDGET and requirements.budget_max is None:
            missing.append("budget_max")
        return {"missing_fields": missing}

    def selected_slot(state: ConversationState) -> Optional[dict[str, Any]]:
        number = state["last_analysis"].selected_slot_number
        offered = state.get("offered_slots") or []
        if number is not None and 1 <= number <= len(offered):
            return offered[number - 1]
        return None

    def route_booking(state: ConversationState) -> str:
        analysis = state["last_analysis"]

        if state.get("missing_fields"):
            if state.get("followup_count", 0) >= settings.max_followups:
                return "request_transfer"
            return "ask_followup"

        if state.get("pending_slot") is not None:
            if analysis.confirmation == "yes":
                return "book_appointment"
            if analysis.confirmation == "no":
                return "offer_slots"
            return "confirm_slot"

        if state.get("offered_slots"):
            return "confirm_slot" if selected_slot(state) else "choose_again"

        return "offer_slots"

    def ask_followup(state: ConversationState) -> dict[str, Any]:
        field = state["missing_fields"][0]
        return {
            "messages": [say(replies.ask_missing(field))],
            "followup_count": state.get("followup_count", 0) + 1,
        }

    def customer_type_for(state: ConversationState) -> str:
        customer = state.get("customer")
        if customer and customer.get("customer_type"):
            return customer["customer_type"]
        requirements = state["requirements"]
        return requirements.customer_type.value if requirements.customer_type else "Private"

    def appointment_type_for(state: ConversationState) -> int:
        return settings.purpose_to_appointment_type[state["requirements"].appointment_purpose]

    def offer_slots(state: ConversationState) -> dict[str, Any]:
        requirements = state["requirements"]
        preferred = requirements.preferred_date
        from_date = preferred if preferred and preferred >= today() else today()

        try:
            slots = backend.get_availability(
                appointment_type_for(state),
                from_date,
                customer_type_for(state),
                bool(requirements.special_preparation),
                settings.slots_to_offer,
            )
        except BackendError:
            return {"messages": [say(replies.booking_failed())], "pending_slot": None, "mode": TRANSFER}

        if not slots:
            return {"messages": [say(replies.no_slots())], "offered_slots": [], "pending_slot": None, "mode": TRANSFER}

        return {"messages": [say(replies.offer_slots(slots))], "offered_slots": slots, "pending_slot": None}

    def confirm_slot(state: ConversationState) -> dict[str, Any]:
        slot = selected_slot(state) or state["pending_slot"]
        return {"pending_slot": slot, "messages": [say(replies.confirm_slot(slot))]}

    def choose_again(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.choose_slot_again(state["offered_slots"]))]}

    def book_appointment(state: ConversationState, config: RunnableConfig) -> dict[str, Any]:
        slot = state["pending_slot"]
        requirements = state["requirements"]
        customer = state.get("customer")
        thread_id = config["configurable"]["thread_id"]

        try:
            if customer is None:
                customer = backend.create_customer(state["caller_phone"], requirements.full_name, customer_type_for(state))

            appointment = backend.book_appointment(
                customer["id"],
                appointment_type_for(state),
                slot["start_utc"],
                bool(requirements.special_preparation),
                f"{thread_id}:{slot['start_utc']}",
                requirements_notes(requirements),
            )
        except SlotUnavailable as error:
            return {
                "customer": customer,
                "pending_slot": None,
                "offered_slots": error.alternatives,
                "messages": [say(replies.slot_taken(error.alternatives))],
                "mode": AI if error.alternatives else TRANSFER,
            }
        except BackendError:
            return {"customer": customer, "pending_slot": None, "messages": [say(replies.booking_failed())], "mode": TRANSFER}

        return {
            "customer": customer,
            "booking": appointment,
            "pending_slot": None,
            "offered_slots": [],
            "messages": [say(replies.booked(appointment))],
        }

    def answer_product(state: ConversationState) -> dict[str, Any]:
        analysis = state["last_analysis"]
        query = analysis.product_query or " ".join(state["requirements"].preferred_brands)

        try:
            products = backend.search_products(query) if query else []
        except BackendError:
            products = []

        if not products:
            return {"messages": [say(replies.product_unknown())], "mode": TRANSFER}

        return {"messages": [say(replies.product_results(products[:3]))]}

    def route_next(state: ConversationState) -> str:
        return "manager_takeover" if state.get("mode") == TRANSFER else "wait_for_customer"

    def request_transfer(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.transfer_to_manager())], "mode": TRANSFER}

    def too_long(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.too_long())], "mode": TRANSFER}

    def clarify(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.clarify())]}

    def manager_takeover(state: ConversationState) -> dict[str, Any]:
        requirements = state.get("requirements") or Requirements()
        decision = interrupt({
            "type": "manager_takeover",
            "caller_phone": state.get("caller_phone"),
            "customer": state.get("customer"),
            "requirements": requirements.model_dump(mode="json"),
            "summary": build_summary(state),
            "transcript": [
                {"role": "customer" if isinstance(m, HumanMessage) else (m.name or "assistant"), "text": str(m.content)}
                for m in state.get("messages", [])
            ],
        })

        manager_messages = [AIMessage(content=text, name="manager") for text in decision.get("messages", [])]
        completed = decision.get("action") == "complete"

        return {
            "messages": manager_messages,
            "mode": COMPLETED if completed else AI,
            "followup_count": 0,
        }

    def route_after_manager(state: ConversationState) -> str:
        return "finish" if state.get("mode") == COMPLETED else "resume_ai"

    def resume_ai(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.back_from_manager())]}

    def finish(state: ConversationState) -> dict[str, Any]:
        return {"messages": [say(replies.goodbye())], "summary": build_summary(state), "mode": COMPLETED}

    graph = StateGraph(ConversationState)

    graph.add_node("identify_customer", identify_customer)
    graph.add_node("greet", greet)
    graph.add_node("wait_for_customer", wait_for_customer)
    graph.add_node("analyze", analyze)
    graph.add_node("check_requirements", check_requirements)
    graph.add_node("ask_followup", ask_followup)
    graph.add_node("offer_slots", offer_slots)
    graph.add_node("confirm_slot", confirm_slot)
    graph.add_node("choose_again", choose_again)
    graph.add_node("book_appointment", book_appointment)
    graph.add_node("answer_product", answer_product)
    graph.add_node("request_transfer", request_transfer)
    graph.add_node("too_long", too_long)
    graph.add_node("clarify", clarify)
    graph.add_node("manager_takeover", manager_takeover)
    graph.add_node("resume_ai", resume_ai)
    graph.add_node("finish", finish)

    graph.add_edge(START, "identify_customer")
    graph.add_edge("identify_customer", "greet")
    graph.add_edge("greet", "wait_for_customer")

    graph.add_conditional_edges("wait_for_customer", route_after_customer, ["manager_takeover", "too_long", "analyze"])
    graph.add_conditional_edges(
        "analyze",
        route_intent,
        ["request_transfer", "finish", "answer_product", "check_requirements", "clarify"],
    )
    graph.add_conditional_edges(
        "check_requirements",
        route_booking,
        ["ask_followup", "request_transfer", "offer_slots", "confirm_slot", "choose_again", "book_appointment"],
    )

    for node in ("ask_followup", "confirm_slot", "choose_again", "clarify", "resume_ai"):
        graph.add_edge(node, "wait_for_customer")

    for node in ("offer_slots", "book_appointment", "answer_product"):
        graph.add_conditional_edges(node, route_next, ["manager_takeover", "wait_for_customer"])

    graph.add_edge("request_transfer", "manager_takeover")
    graph.add_edge("too_long", "manager_takeover")
    graph.add_conditional_edges("manager_takeover", route_after_manager, ["finish", "resume_ai"])
    graph.add_edge("finish", END)

    return graph.compile(checkpointer=checkpointer or InMemorySaver())
