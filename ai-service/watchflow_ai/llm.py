from langchain_core.language_models import BaseChatModel

from watchflow_ai.config import Settings


def create_chat_model(settings: Settings) -> BaseChatModel:
    if settings.llm_provider == "google":
        if not settings.google_api_key:
            raise RuntimeError("GOOGLE_API_KEY is missing. Copy .env.example to .env and fill it in.")

        from langchain_google_genai import ChatGoogleGenerativeAI

        return ChatGoogleGenerativeAI(
            model=settings.llm_model,
            google_api_key=settings.google_api_key,
            temperature=0,
            max_retries=3,
        )

    raise ValueError(f"Unsupported LLM provider '{settings.llm_provider}'.")
