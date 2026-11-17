import sys
from datetime import date, datetime, timedelta, timezone
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from watchflow_ai.backend import InMemoryBackend
from watchflow_ai.schemas import TurnAnalysis

ISRAEL = timezone(timedelta(hours=2))
TODAY = date(2027, 3, 1)


class ScriptedAnalyzer:
    def __init__(self, *analyses: TurnAnalysis) -> None:
        self._analyses = list(analyses)
        self.calls = 0

    def analyze(self, state):
        self.calls += 1
        if not self._analyses:
            raise AssertionError("ScriptedAnalyzer ran out of scripted analyses")
        return self._analyses.pop(0)


@pytest.fixture
def backend() -> InMemoryBackend:
    return InMemoryBackend(first_slot_local=datetime(2027, 3, 7, 9, 0, tzinfo=ISRAEL))
