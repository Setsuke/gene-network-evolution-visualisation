"""Summary table of exit move types after plateau drift episodes (Unity Option 10 / MDS 2D)."""

from __future__ import annotations

import csv
import json
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.genome_mds_path import extract_accepted_genome_paths
from visualisation.plateau_drift_classification import find_plateau_episodes

FITNESS_EPSILON = 1e-5

EXIT_TYPE_ORDER: tuple[str, ...] = (
    "compensatory improvement",
    "direct improvement",
    "solution",
)

EXIT_TYPE_LABELS: dict[str, str] = {
    "compensatory improvement": "Compensatory improvement",
    "direct improvement": "Direct improvement",
    "solution": "Solution (final accepted step)",
    "unknown": "Unknown",
}

TABLE_HEADERS = [
    "Exit type",
    "Episodes",
    "% of drift exits",
    "Raw changeType",
]


@dataclass(frozen=True)
class PlateauDriftExitSummary:
    run_count: int
    runs_with_drift: int
    episode_count: int
    exit_type_counts: dict[str, int]
    raw_change_type_counts: dict[str, int]


def resolve_exit_type(change_types: list[str], exit_step: int) -> str:
    """Match Unity PlateauEpisodeAnalyzer.ResolveExitType."""
    if exit_step >= len(change_types) - 1:
        return "solution"

    change_type = change_types[exit_step] or ""
    if change_type in {
        "partial_improvement",
        "pure_improvement",
        "partial_global_improvement",
        "direct_improvement",
    }:
        return "direct improvement"
    if change_type == "compensatory_improvement":
        return "compensatory improvement"
    if not change_type:
        return "unknown"
    return change_type.replace("_", " ")


def _raw_change_type_for_exit(change_types: list[str], exit_step: int) -> str:
    if exit_step >= len(change_types):
        return ""
    return change_types[exit_step] or "unknown"


def collect_plateau_drift_exit_summary(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
) -> PlateauDriftExitSummary:
    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    exit_type_counts: Counter[str] = Counter()
    raw_change_type_counts: Counter[str] = Counter()
    runs_with_drift = 0
    episode_count = 0

    for path in paths:
        fitness_values = list(path.fitness_values)
        change_types = list(path.change_types)
        episodes = find_plateau_episodes(fitness_values)
        if episodes:
            runs_with_drift += 1
        for _start_step, _end_step, exit_step in episodes:
            episode_count += 1
            exit_type = resolve_exit_type(change_types, exit_step)
            exit_type_counts[exit_type] += 1
            raw_change_type_counts[_raw_change_type_for_exit(change_types, exit_step)] += 1

    return PlateauDriftExitSummary(
        run_count=len(paths),
        runs_with_drift=runs_with_drift,
        episode_count=episode_count,
        exit_type_counts=dict(exit_type_counts),
        raw_change_type_counts=dict(raw_change_type_counts),
    )


def collect_plateau_drift_exit_summary_from_unity_json(
    json_path: str | Path,
) -> PlateauDriftExitSummary:
    payload = json.loads(Path(json_path).read_text(encoding="utf-8"))
    runs = payload.get("runs") or []
    exit_type_counts: Counter[str] = Counter()
    raw_change_type_counts: Counter[str] = Counter()
    runs_with_drift = 0
    episode_count = 0

    for run in runs:
        points = run.get("points") or []
        fitness_values = [float(point["fitness"]) for point in points]
        change_types = [point.get("changeType") or "" for point in points]
        episodes = find_plateau_episodes(fitness_values)
        if episodes:
            runs_with_drift += 1
        for _start_step, _end_step, exit_step in episodes:
            episode_count += 1
            exit_type = resolve_exit_type(change_types, exit_step)
            exit_type_counts[exit_type] += 1
            raw_change_type_counts[_raw_change_type_for_exit(change_types, exit_step)] += 1

    return PlateauDriftExitSummary(
        run_count=len(runs),
        runs_with_drift=runs_with_drift,
        episode_count=episode_count,
        exit_type_counts=dict(exit_type_counts),
        raw_change_type_counts=dict(raw_change_type_counts),
    )


def _raw_change_type_label(exit_type: str, raw_counts: dict[str, int]) -> str:
    if exit_type == "compensatory improvement":
        return "compensatory_improvement"
    if exit_type == "direct improvement":
        direct = raw_counts.get("direct_improvement", 0)
        partial = sum(
            raw_counts.get(key, 0)
            for key in (
                "partial_improvement",
                "pure_improvement",
                "partial_global_improvement",
            )
        )
        if partial:
            return f"direct_improvement ({direct}); partial_* ({partial})"
        return "direct_improvement"
    if exit_type == "solution":
        return "direct_improvement (final step)"
    unknown_keys = [key for key in raw_counts if key not in {
        "compensatory_improvement",
        "direct_improvement",
        "partial_improvement",
        "pure_improvement",
        "partial_global_improvement",
        "initial",
    }]
    if unknown_keys:
        return ", ".join(sorted(unknown_keys))
    return "—"


def build_plateau_drift_exit_rows(
    summary: PlateauDriftExitSummary,
) -> tuple[list[str], list[list[str | int | float]]]:
    rows: list[list[str | int | float]] = []
    total_episodes = summary.episode_count

    ordered_exit_types = list(EXIT_TYPE_ORDER)
    extra_exit_types = [
        exit_type
        for exit_type in summary.exit_type_counts
        if exit_type not in ordered_exit_types
    ]
    ordered_exit_types.extend(sorted(extra_exit_types))

    for exit_type in ordered_exit_types:
        count = summary.exit_type_counts.get(exit_type, 0)
        if count == 0:
            continue
        rows.append(
            [
                EXIT_TYPE_LABELS.get(exit_type, exit_type.capitalize()),
                count,
                100.0 * count / total_episodes if total_episodes else 0.0,
                _raw_change_type_label(exit_type, summary.raw_change_type_counts),
            ]
        )

    rows.append(
        [
            "Total drift exits",
            total_episodes,
            100.0 if total_episodes else 0.0,
            "—",
        ]
    )
    return TABLE_HEADERS, rows


def _write_summary_csv(
    headers: list[str],
    rows: list[list[str | int | float]],
    csv_path: Path,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(headers)
        writer.writerows(rows)


def plot_plateau_drift_exit_types_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    summary = collect_plateau_drift_exit_summary(histories, contexts, labels=labels)
    return _render_plateau_drift_exit_table(summary, save_path, csv_path=csv_path)


def plot_plateau_drift_exit_types_table_from_json(
    json_path: str | Path,
    save_path: str | Path,
    *,
    csv_path: str | Path | None = None,
) -> Path:
    summary = collect_plateau_drift_exit_summary_from_unity_json(json_path)
    return _render_plateau_drift_exit_table(summary, save_path, csv_path=csv_path)


def _render_plateau_drift_exit_table(
    summary: PlateauDriftExitSummary,
    save_path: str | Path,
    *,
    csv_path: str | Path | None = None,
) -> Path:
    if plt is None:
        raise ImportError("matplotlib is required for plateau drift exit type tables")

    from visualisation.non_neutral_moves import _style_summary_table

    headers, rows = build_plateau_drift_exit_rows(summary)
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    if csv_path is not None:
        _write_summary_csv(headers, rows, Path(csv_path))

    display_rows = [
        [
            str(row[0]),
            str(row[1]),
            f"{float(row[2]):.1f}%",
            str(row[3]),
        ]
        for row in rows
    ]

    figure_height = max(3.8, 0.55 * len(display_rows) + 2.2)
    figure, axis = plt.subplots(figsize=(12.5, figure_height))
    axis.axis("off")
    table = axis.table(
        cellText=display_rows,
        colLabels=headers,
        loc="center",
        cellLoc="center",
        colWidths=[0.34, 0.12, 0.18, 0.36],
    )
    _style_summary_table(table, header_fontsize=9, body_fontsize=9)
    table.auto_set_font_size(False)
    table.scale(1.0, 1.65)

    figure.suptitle(
        "Exit types after plateau drift\n"
        f"{summary.run_count} runs  |  {summary.runs_with_drift} runs with drift  |  "
        f"{summary.episode_count} drift episodes",
        fontsize=12,
        y=0.98,
    )
    figure.text(
        0.5,
        0.03,
        "Each row counts the first fitness-improving step after a neutral plateau drift segment "
        "(same detection as Unity Show drift moves).",
        ha="center",
        fontsize=9,
        color="#334155",
    )
    figure.subplots_adjust(top=0.84, bottom=0.12)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.35)
    plt.close(figure)
    return save_path


if __name__ == "__main__":
    import argparse

    from config import (
        GENOME_BATCH_MDS_UNITY_JSON,
        PLATEAU_DRIFT_EXIT_TYPES_CSV,
        PLATEAU_DRIFT_EXIT_TYPES_TABLE,
    )

    parser = argparse.ArgumentParser(
        description="Generate PNG/CSV table of exit types after plateau drift episodes.",
    )
    parser.add_argument(
        "--json",
        type=Path,
        default=GENOME_BATCH_MDS_UNITY_JSON,
        help="Unity batch genome MDS JSON to analyse",
    )
    parser.add_argument(
        "--png",
        type=Path,
        default=PLATEAU_DRIFT_EXIT_TYPES_TABLE,
        help="Output PNG path",
    )
    parser.add_argument(
        "--csv",
        type=Path,
        default=PLATEAU_DRIFT_EXIT_TYPES_CSV,
        help="Output CSV path",
    )
    args = parser.parse_args()
    args.png.parent.mkdir(parents=True, exist_ok=True)
    args.csv.parent.mkdir(parents=True, exist_ok=True)
    output_png = plot_plateau_drift_exit_types_table_from_json(
        args.json,
        args.png,
        csv_path=args.csv,
    )
    print(f"Wrote {output_png}")
