import argparse
from collections import defaultdict
from typing import Any, Iterable

from watchflow_ai.analyzer import LlmAnalyzer
from watchflow_ai.config import Settings
from watchflow_ai.eval.dataset import DATASET_NAME, EXAMPLES
from watchflow_ai.eval.evaluators import EVALUATORS
from watchflow_ai.eval.target import run_analysis, today_from_inputs
from watchflow_ai.llm import create_chat_model


def make_target(settings: Settings, prompt_version: str):
    model = create_chat_model(settings)

    def target(inputs: dict[str, Any]) -> dict[str, Any]:
        analyzer = LlmAnalyzer(model, prompt_version=prompt_version, today=lambda: today_from_inputs(inputs))
        return run_analysis(analyzer, inputs)

    return target


def print_scores(title: str, scores: dict[str, list[float]]) -> None:
    print(f"\n{title}")
    print("-" * len(title))
    for key in [evaluator.__name__ for evaluator in EVALUATORS]:
        values = scores.get(key, [])
        average = sum(values) / len(values) if values else 0.0
        print(f"{key:<22} {average:6.0%}  ({int(sum(values))}/{len(values)})")


def ensure_dataset(client) -> None:
    if client.has_dataset(dataset_name=DATASET_NAME):
        print(f"Using existing LangSmith dataset '{DATASET_NAME}'.")
        return

    dataset = client.create_dataset(
        DATASET_NAME,
        description="WatchFlow AI: intent and requirement extraction from one customer turn (Hebrew).",
    )
    client.create_examples(dataset_id=dataset.id, examples=EXAMPLES)
    print(f"Created LangSmith dataset '{DATASET_NAME}' with {len(EXAMPLES)} examples.")


def collect_langsmith_scores(results: Iterable[Any]) -> dict[str, list[float]]:
    scores: dict[str, list[float]] = defaultdict(list)
    for row in results:
        for result in row["evaluation_results"]["results"]:
            if result.score is not None:
                scores[result.key].append(float(result.score))
    return scores


def run_local(target) -> None:
    scores: dict[str, list[float]] = defaultdict(list)
    for index, example in enumerate(EXAMPLES, start=1):
        outputs = target(example["inputs"])
        failed = []
        for evaluator in EVALUATORS:
            result = evaluator(outputs, example["outputs"])
            scores[result["key"]].append(result["score"])
            if not result["score"]:
                failed.append(result["key"])
        status = "OK  " if not failed else "FAIL"
        print(f"{status} {index:>2}. {example['inputs']['message']}" + (f"  -> {', '.join(failed)}" if failed else ""))
    print_scores("Local results", scores)


def main() -> None:
    parser = argparse.ArgumentParser(description="Evaluate the turn analyzer prompt against the WatchFlow dataset.")
    parser.add_argument("--prompt", choices=["v1", "v2"], default="v2")
    parser.add_argument("--local", action="store_true", help="Run without LangSmith and print results only.")
    args = parser.parse_args()

    settings = Settings.from_env()
    target = make_target(settings, args.prompt)

    if args.local:
        run_local(target)
        return

    from langsmith import Client

    client = Client()
    ensure_dataset(client)

    results = client.evaluate(
        target,
        data=DATASET_NAME,
        evaluators=EVALUATORS,
        experiment_prefix=f"turn-analysis-{args.prompt}",
        description=f"Turn analyzer with prompt {args.prompt}",
        metadata={"prompt_version": args.prompt, "model": settings.llm_model},
        max_concurrency=1,
    )

    print_scores(f"Experiment {results.experiment_name}", collect_langsmith_scores(results))
    print(f"\nOpen in LangSmith: {results.url}")


if __name__ == "__main__":
    main()
