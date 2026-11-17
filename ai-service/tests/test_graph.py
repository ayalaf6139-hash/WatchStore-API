from langchain_core.messages import AIMessage
from langgraph.checkpoint.memory import InMemorySaver
from langgraph.types import Command

from conftest import TODAY, ScriptedAnalyzer
from watchflow_ai.graph import build_graph
from watchflow_ai.schemas import AppointmentPurpose, Intent, Requirements, TurnAnalysis


def turn(intent=Intent.BOOK_APPOINTMENT, **kwargs) -> TurnAnalysis:
    requirements = kwargs.pop("requirements", Requirements())
    return TurnAnalysis(intent=intent, requirements=requirements, **kwargs)


class Conversation:
    def __init__(self, graph, phone="050-1234567", thread_id="call-1"):
        self.graph = graph
        self.config = {"configurable": {"thread_id": thread_id}}
        self.result = graph.invoke({"caller_phone": phone}, self.config)

    def customer(self, text):
        self.result = self.graph.invoke(Command(resume={"type": "customer_message", "text": text}), self.config)
        return self

    def takeover(self):
        self.result = self.graph.invoke(Command(resume={"type": "manager_takeover"}), self.config)
        return self

    def manager(self, action, *messages):
        self.result = self.graph.invoke(Command(resume={"action": action, "messages": list(messages)}), self.config)
        return self

    @property
    def interrupt(self):
        interrupts = self.result.get("__interrupt__")
        return interrupts[0].value if interrupts else None

    @property
    def ai_texts(self):
        return [m.content for m in self.result["messages"] if isinstance(m, AIMessage)]

    @property
    def last_reply(self):
        return self.ai_texts[-1]


def build(analyzer, backend, checkpointer=None):
    return build_graph(analyzer, backend, checkpointer=checkpointer, today=lambda: TODAY)


def test_scenario_a_new_customer_books_after_followup_loop(backend):
    analyzer = ScriptedAnalyzer(
        turn(requirements=Requirements(appointment_purpose=AppointmentPurpose.CONSULTATION)),
        turn(requirements=Requirements(full_name="ישראל ישראלי")),
        turn(requirements=Requirements(budget_max=5000, preferred_brands=["Seiko"])),
        turn(selected_slot_number=2),
        turn(confirmation="yes"),
    )
    conversation = Conversation(build(analyzer, backend))

    assert conversation.interrupt["type"] == "customer_turn"
    assert "Friedman Watches" in conversation.last_reply

    conversation.customer("שלום, אני רוצה לקבוע פגישת ייעוץ")
    assert "איך קוראים לך" in conversation.last_reply

    conversation.customer("ישראל ישראלי")
    assert "תקציב" in conversation.last_reply

    conversation.customer("עד 5 אלף, אוהב סייקו")
    assert conversation.result["offered_slots"]
    assert "1." in conversation.last_reply and "3." in conversation.last_reply

    conversation.customer("השני")
    second = conversation.result["offered_slots"][1]
    assert conversation.result["pending_slot"] == second
    assert conversation.last_reply.startswith("לאשר תור")
    assert backend.appointments == []

    conversation.customer("כן")
    assert len(backend.appointments) == 1
    assert backend.appointments[0]["start_utc"] == second["start_utc"]
    assert backend.find_customer("0501234567")["full_name"] == "ישראל ישראלי"
    assert "התור נקבע" in conversation.last_reply
    assert conversation.result["booking"]["id"] == 1


def test_scenario_b_existing_customer_is_greeted_and_not_asked_for_name(backend):
    backend.add_customer("050-1234567", "רחל כהן", "Collector")
    analyzer = ScriptedAnalyzer(
        turn(requirements=Requirements(appointment_purpose=AppointmentPurpose.REPAIR)),
    )
    conversation = Conversation(build(analyzer, backend))

    assert "רחל כהן" in conversation.last_reply

    conversation.customer("השעון שלי נעצר, אפשר להביא לתיקון?")
    assert conversation.result["offered_slots"]
    assert "איך קוראים לך" not in " ".join(conversation.ai_texts)


def test_no_booking_claim_when_slot_was_taken_meanwhile(backend):
    backend.add_customer("050-1234567", "דוד", "Private")
    analyzer = ScriptedAnalyzer(
        turn(requirements=Requirements(appointment_purpose=AppointmentPurpose.STRAP_FITTING)),
        turn(selected_slot_number=1),
        turn(confirmation="yes"),
    )
    conversation = Conversation(build(analyzer, backend))
    conversation.customer("אני צריך להתאים רצועה")
    conversation.customer("הראשון")

    taken = conversation.result["pending_slot"]["start_utc"]
    backend.taken.add(taken)

    conversation.customer("כן")

    assert backend.appointments == []
    assert conversation.result["booking"] is None
    assert not any("התור נקבע" in text for text in conversation.ai_texts)
    assert "נתפס" in conversation.last_reply
    assert all(slot["start_utc"] != taken for slot in conversation.result["offered_slots"])
    assert conversation.interrupt["type"] == "customer_turn"


def test_customer_must_confirm_explicitly_before_booking(backend):
    backend.add_customer("050-1234567", "דוד", "Private")
    analyzer = ScriptedAnalyzer(
        turn(requirements=Requirements(appointment_purpose=AppointmentPurpose.REPAIR)),
        turn(selected_slot_number=3),
        turn(),
        turn(confirmation="no"),
    )
    conversation = Conversation(build(analyzer, backend))
    conversation.customer("תיקון שעון")
    conversation.customer("השלישי")
    conversation.customer("אממ")

    assert conversation.last_reply.startswith("לאשר תור")
    assert backend.appointments == []

    conversation.customer("לא, רגע")
    assert conversation.result["pending_slot"] is None
    assert "המועדים הפנויים" in conversation.last_reply
    assert backend.appointments == []


def test_scenario_d_manager_takeover_keeps_context_and_returns_to_ai(backend):
    analyzer = ScriptedAnalyzer(
        turn(requirements=Requirements(appointment_purpose=AppointmentPurpose.LUXURY_PURCHASE, budget_max=40000, preferred_brands=["Rolex"])),
        turn(intent=Intent.END_CONVERSATION),
    )
    conversation = Conversation(build(analyzer, backend))
    conversation.customer("מחפש רולקס עד 40 אלף")

    conversation.takeover()
    payload = conversation.interrupt
    assert payload["type"] == "manager_takeover"
    assert payload["requirements"]["budget_max"] == 40000
    assert payload["requirements"]["preferred_brands"] == ["Rolex"]
    assert any(line["role"] == "customer" for line in payload["transcript"])

    conversation.manager("return_to_ai", "שלום, כאן המנהל. יש לנו Submariner במלאי.")
    manager_messages = [m for m in conversation.result["messages"] if isinstance(m, AIMessage) and m.name == "manager"]
    assert manager_messages[0].content.startswith("שלום, כאן המנהל")
    assert conversation.result["requirements"].budget_max == 40000
    assert conversation.interrupt["type"] == "customer_turn"
    assert conversation.result["mode"] == "ai"

    conversation.customer("תודה, זה הכל")
    assert conversation.interrupt is None
    assert conversation.result["mode"] == "completed"
    assert "Rolex" in conversation.result["summary"]


def test_customer_asking_for_human_triggers_takeover(backend):
    analyzer = ScriptedAnalyzer(turn(intent=Intent.TALK_TO_HUMAN))
    conversation = Conversation(build(analyzer, backend))

    conversation.customer("אפשר לדבר עם בן אדם?")

    assert conversation.interrupt["type"] == "manager_takeover"
    assert "מעביר את השיחה למנהל" in conversation.last_reply

    conversation.manager("complete", "אחזור אליך בעוד שעה")
    assert conversation.interrupt is None
    assert conversation.result["mode"] == "completed"


def test_scenario_e_unknown_product_escalates_to_manager(backend):
    analyzer = ScriptedAnalyzer(turn(intent=Intent.PRODUCT_QUESTION, product_query="Patek Philippe"))
    conversation = Conversation(build(analyzer, backend))

    conversation.customer("יש לכם פטק פיליפ?")

    assert conversation.interrupt["type"] == "manager_takeover"
    assert "אין לי מידע מדויק" in conversation.last_reply


def test_known_product_answer_uses_catalog_data_only(backend):
    backend.products.append({"name": "G-Shock GA-2100", "brand": "Casio", "model_number": "CASIO-001", "price": 550, "in_stock": True})
    analyzer = ScriptedAnalyzer(turn(intent=Intent.PRODUCT_QUESTION, product_query="G-Shock"))
    conversation = Conversation(build(analyzer, backend))

    conversation.customer("יש לכם ג'י שוק?")

    assert "550" in conversation.last_reply
    assert "במלאי" in conversation.last_reply
    assert conversation.interrupt["type"] == "customer_turn"


def test_followup_loop_is_bounded(backend):
    analyzer = ScriptedAnalyzer(*[turn() for _ in range(5)])
    conversation = Conversation(build(analyzer, backend))

    for _ in range(4):
        conversation.customer("רוצה לקבוע תור")
        assert conversation.interrupt["type"] == "customer_turn"

    conversation.customer("רוצה לקבוע תור")
    assert conversation.interrupt["type"] == "manager_takeover"


def test_scenario_f_state_survives_new_graph_instance(backend):
    checkpointer = InMemorySaver()
    first = build(ScriptedAnalyzer(turn(requirements=Requirements(budget_max=3000))), backend, checkpointer)
    conversation = Conversation(first, thread_id="call-dropped")
    conversation.customer("עד 3000 שקל")

    restarted = build(ScriptedAnalyzer(), backend, checkpointer)
    state = restarted.get_state({"configurable": {"thread_id": "call-dropped"}})

    assert state.values["requirements"].budget_max == 3000
    assert state.next == ("wait_for_customer",)


def test_graph_contains_loop_and_conditional_routing(backend):
    graph = build(ScriptedAnalyzer(), backend).get_graph()
    edges = {(edge.source, edge.target) for edge in graph.edges}

    assert ("ask_followup", "wait_for_customer") in edges
    assert ("wait_for_customer", "analyze") in edges
    assert ("analyze", "check_requirements") in edges
    assert ("check_requirements", "ask_followup") in edges
