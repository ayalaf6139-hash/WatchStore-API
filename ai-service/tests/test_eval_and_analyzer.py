from datetime import date

import pytest
from langchain_core.messages import AIMessage, HumanMessage, SystemMessage

from watchflow_ai.analyzer import build_analysis_messages
from watchflow_ai.eval.dataset import EXAMPLES
from watchflow_ai.eval.evaluators import EVALUATORS, FIELDS, all_fields_correct, brands_correct, budget_correct
from watchflow_ai.eval.target import outputs_from_analysis, run_analysis, state_from_inputs
from watchflow_ai.prompts import PROMPTS, get_analyze_prompt
from watchflow_ai.schemas import AppointmentPurpose, Intent, Requirements, TurnAnalysis, merge_requirements


def test_dataset_has_twenty_complete_examples():
    assert len(EXAMPLES) == 20
    intents = {e.value for e in Intent}
    for example in EXAMPLES:
        assert set(example["outputs"]) == set(FIELDS)
        assert example["outputs"]["intent"] in intents
        assert example["inputs"]["message"].strip()


def test_dataset_covers_every_intent():
    covered = {example["outputs"]["intent"] for example in EXAMPLES}
    assert covered == {e.value for e in Intent}


def test_evaluators_score_matches_and_mismatches():
    reference = {"intent": "book_appointment", "appointment_purpose": None, "budget_max": 5000,
                 "preferred_brands": ["Seiko", "Tissot"], "selected_slot_number": None, "confirmation": None}
    good = dict(reference, budget_max=5000.0, preferred_brands=["tissot", "SEIKO"])
    bad = dict(reference, budget_max=None, preferred_brands=["Seiko"])

    assert budget_correct(good, reference)["score"] == 1
    assert brands_correct(good, reference)["score"] == 1
    assert all_fields_correct(good, reference)["score"] == 1
    assert budget_correct(bad, reference)["score"] == 0
    assert brands_correct(bad, reference)["score"] == 0
    assert all_fields_correct(bad, reference)["score"] == 0
    assert len({evaluator.__name__ for evaluator in EVALUATORS}) == len(EVALUATORS)


class EchoAnalyzer:
    def __init__(self, analysis: TurnAnalysis) -> None:
        self.analysis = analysis
        self.state = None

    def analyze(self, state):
        self.state = state
        return self.analysis


def test_target_builds_state_and_flattens_outputs():
    example = next(e for e in EXAMPLES if e["outputs"]["confirmation"] == "yes")
    analyzer = EchoAnalyzer(TurnAnalysis(intent=Intent.BOOK_APPOINTMENT, confirmation="yes"))

    outputs = run_analysis(analyzer, example["inputs"])

    assert outputs["confirmation"] == "yes"
    assert outputs["intent"] == "book_appointment"
    assert isinstance(analyzer.state["messages"][-1], HumanMessage)
    assert analyzer.state["messages"][-1].content == example["inputs"]["message"]
    assert analyzer.state["pending_slot"] is not None
    assert analyzer.state["requirements"].budget_max == 5000


def test_outputs_from_analysis_handles_empty_requirements():
    outputs = outputs_from_analysis(TurnAnalysis(intent=Intent.OTHER))
    assert outputs == {"intent": "other", "appointment_purpose": None, "budget_max": None,
                       "preferred_brands": [], "selected_slot_number": None, "confirmation": None}


@pytest.mark.parametrize("version", sorted(PROMPTS))
def test_every_prompt_version_renders_with_context(version):
    inputs = next(e for e in EXAMPLES if e["inputs"]["pending_slot"])["inputs"]
    state = state_from_inputs(inputs)

    messages = build_analysis_messages(version, date(2027, 3, 1), state["messages"], state["requirements"],
                                       state["offered_slots"], state["pending_slot"])

    assert isinstance(messages[0], SystemMessage)
    assert "2027-03-01" in messages[0].content
    assert "2027-03-07T10:30:00+02:00" in messages[0].content
    assert any(isinstance(m, AIMessage) and m.content.startswith("[Assistant]") for m in messages)


def test_unknown_prompt_version_is_rejected():
    with pytest.raises(ValueError):
        get_analyze_prompt("v9")


def test_merge_keeps_known_values_and_tracks_inferred_fields():
    current = Requirements(appointment_purpose=AppointmentPurpose.LUXURY_PURCHASE, preferred_brands=["Rolex"],
                           inferred_fields=["appointment_purpose"])
    update = Requirements(budget_max=40000, preferred_brands=["rolex", "Omega"], appointment_purpose=AppointmentPurpose.LUXURY_PURCHASE)

    merged = merge_requirements(current, update)

    assert merged.budget_max == 40000
    assert merged.preferred_brands == ["Rolex", "Omega"]
    assert merged.inferred_fields == []
    assert current.budget_max is None
