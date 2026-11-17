import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def main(results_dir: str) -> int:
    files = list(Path(results_dir).rglob("*.trx"))
    if not files:
        print("::error title=Tests::No test results were produced (build or test run failed before tests ran).")
        return 1

    total = passed = failed = 0
    failures = []

    for trx in files:
        root = ET.parse(trx).getroot()
        counters = root.find("t:ResultSummary/t:Counters", NS)
        if counters is not None:
            total += int(counters.get("total", 0))
            passed += int(counters.get("passed", 0))
            failed += int(counters.get("failed", 0))

        for result in root.iterfind("t:Results/t:UnitTestResult", NS):
            if result.get("outcome") != "Failed":
                continue
            message = result.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=NS)
            failures.append((result.get("testName", "?"), " ".join(message.split())[:400]))

    print(f"::notice title=Tests::Total {total}, Passed {passed}, Failed {failed}")
    for name, message in failures:
        print(f"::error title=Test failed: {name}::{message}")

    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary_path:
        with open(summary_path, "a", encoding="utf-8") as summary:
            summary.write("## Test results\n\n| Total | Passed | Failed |\n|---|---|---|\n")
            summary.write(f"| {total} | {passed} | {failed} |\n\n")
            for name, message in failures:
                summary.write(f"- **{name}**: {message}\n")

    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "TestResults"))
