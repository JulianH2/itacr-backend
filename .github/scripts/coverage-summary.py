#!/usr/bin/env python3
"""Summarizes the test coverage of the backend (ITACR-62).

Every test project writes its own Cobertura report, and the same line of the product is usually run by more than one of them.
This merges the reports (a line is covered when any project ran it) and prints the percentage of covered lines, in total and
per project, as a Markdown table. In GitHub Actions the table is also added to the summary of the job.

Usage: coverage-summary.py <folder with the coverage.cobertura.xml files>
"""

import os
import sys
import xml.etree.ElementTree as ET
from collections import defaultdict
from pathlib import Path


def merge_reports(reports):
    """Returns {project: {(file, line): hits}}, keeping the highest number of hits of each line."""
    projects = defaultdict(dict)
    for report in reports:
        for package in ET.parse(report).getroot().iter("package"):
            lines = projects[package.get("name", "unknown")]
            for cls in package.iter("class"):
                filename = cls.get("filename", "")
                for line in cls.iter("line"):
                    key = (filename, int(line.get("number", "0")))
                    lines[key] = max(lines.get(key, 0), int(line.get("hits", "0")))
    return projects


def percent(covered, total):
    return 100.0 * covered / total if total else 0.0


def render(projects):
    rows = []
    covered_all = total_all = 0
    for name in sorted(projects):
        lines = projects[name]
        covered = sum(1 for hits in lines.values() if hits > 0)
        covered_all += covered
        total_all += len(lines)
        rows.append(f"| {name} | {covered} | {len(lines)} | {percent(covered, len(lines)):.1f}% |")
    header = [
        f"### Test coverage: {percent(covered_all, total_all):.1f}% of the lines",
        "",
        "| Project | Covered lines | Lines | Coverage |",
        "| --- | ---: | ---: | ---: |",
    ]
    footer = [f"| **Total** | **{covered_all}** | **{total_all}** | **{percent(covered_all, total_all):.1f}%** |"]
    return "\n".join(header + rows + footer) + "\n"


def main():
    folder = Path(sys.argv[1]) if len(sys.argv) > 1 else Path("TestResults")
    reports = sorted(folder.rglob("coverage.cobertura.xml"))
    if not reports:
        print(f"::warning title=No coverage report::There is no coverage.cobertura.xml under {folder}.")
        return

    markdown = render(merge_reports(reports))
    print(markdown)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as file:
            file.write(markdown)


if __name__ == "__main__":
    main()
