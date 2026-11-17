import argparse
import uuid
from datetime import datetime, timedelta, timezone

from langchain_core.messages import AIMessage
from langgraph.types import Command

from watchflow_ai.analyzer import LlmAnalyzer
from watchflow_ai.backend import HttpBackendClient, InMemoryBackend
from watchflow_ai.config import Settings
from watchflow_ai.graph import build_graph
from watchflow_ai.llm import create_chat_model

HELP = "פקודות: /takeover = המנהל משתלט, /quit = יציאה"
MANAGER_HELP = "מצב מנהל. כתבו הודעות ללקוח. /ai = החזרה לבוט, /end = סיום השיחה"


def create_offline_backend() -> InMemoryBackend:
    tomorrow = datetime.now(timezone(timedelta(hours=2))).replace(hour=9, minute=0, second=0, microsecond=0) + timedelta(days=1)
    backend = InMemoryBackend(first_slot_local=tomorrow)
    backend.products.append({"name": "G-Shock GA-2100", "brand": "Casio", "model_number": "CASIO-001", "price": 550, "in_stock": True})
    backend.products.append({"name": "Submariner Date", "brand": "Rolex", "model_number": "ROLEX-001", "price": 45000, "in_stock": True})
    return backend


def print_new_messages(state: dict, printed: int) -> int:
    messages = state.get("messages", [])
    for message in messages[printed:]:
        if isinstance(message, AIMessage):
            speaker = "מנהל" if message.name == "manager" else "בוט"
            print(f"\n{speaker}> {message.content}")
    return len(messages)


def run_manager(payload: dict) -> dict:
    print("\n" + "=" * 50)
    print("המנהל קיבל את השיחה. סיכום עד עכשיו:")
    print(payload["summary"])
    print(MANAGER_HELP)
    messages = []
    while True:
        line = input("מנהל> ").strip()
        if line == "/ai":
            return {"action": "return_to_ai", "messages": messages}
        if line == "/end":
            return {"action": "complete", "messages": messages}
        if line:
            messages.append(line)


def main() -> None:
    parser = argparse.ArgumentParser(description="Simulated WatchFlow phone conversation.")
    parser.add_argument("--phone", default="050-1234567", help="Caller phone number")
    parser.add_argument("--offline", action="store_true", help="Use an in-memory backend instead of the WatchFlow API")
    args = parser.parse_args()

    settings = Settings.from_env()
    backend = create_offline_backend() if args.offline else HttpBackendClient(settings.api_url, settings.api_username, settings.api_password)
    analyzer = LlmAnalyzer(create_chat_model(settings), prompt_version=settings.prompt_version)
    graph = build_graph(analyzer, backend, settings)

    config = {"configurable": {"thread_id": f"call-{uuid.uuid4().hex[:8]}"}, "metadata": {"caller_phone": args.phone}}
    print(HELP)

    state = graph.invoke({"caller_phone": args.phone}, config)
    printed = print_new_messages(state, 0)

    while state.get("__interrupt__"):
        payload = state["__interrupt__"][0].value

        if payload["type"] == "manager_takeover":
            resume = run_manager(payload)
        else:
            text = input("\nלקוח> ").strip()
            if text == "/quit":
                return
            resume = {"type": "manager_takeover"} if text == "/takeover" else {"type": "customer_message", "text": text}

        state = graph.invoke(Command(resume=resume), config)
        printed = print_new_messages(state, printed)

    print("\n" + "=" * 50)
    print("סיכום השיחה:")
    print(state.get("summary"))


if __name__ == "__main__":
    main()
