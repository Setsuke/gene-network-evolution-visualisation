"""Summary table for fitness-improving accepted genome changes."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

try:
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

from rbn.types import Contexts, EvolutionHistory, GenerationRecord
from visualisation.accepted_genome_change_helpers import (
    _count_context_attractor_cycles_changed,
    _transition_table_for_record,
)
from visualisation.accepted_move_taxonomy import (
    factor_score_deltas,
    is_fitness_improving,
    iter_accepted_change_records,
)
from visualisation.evolutionary_landscape import count_changed_transitions, hamming_distance
from visualisation.factorised.metrics import context_order

FitnessImprovingCategory = Literal[
    "compensatory_improvement",
    "direct_improvement",
]

FITNESS_IMPROVING_CATEGORY_ORDER: tuple[FitnessImprovingCategory, ...] = (
    "compensatory_improvement",
    "direct_improvement",
)

FITNESS_IMPROVING_CATEGORY_LABELS: dict[FitnessImprovingCategory, str] = {
    "compensatory_improvement": "Compensatory improvement",
    "direct_improvement": "Direct improvement",
}


@dataclass(frozen=True)
class FitnessImprovingEvent:
    run_label: str
    accepted_step: int
    generation: int
    category: FitnessImprovingCategory
    genome_hamming_distance: int
    changed_transitions: int
    context_path_changes: int
    attractor_cycle_changes: int
    fitness_delta: float


def _count_context_path_changes(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> int:
    return sum(
        1
        for initial in context_order(contexts)
        if tuple(previous["details"][initial]["path"])
        != tuple(current["details"][initial]["path"])
    )


def classify_fitness_improving_move(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> FitnessImprovingCategory | None:
    if not is_fitness_improving(previous, current):
        return None

    deltas = factor_score_deltas(previous, current, contexts)
    improved = any(delta > 0.0 for delta in deltas)
    worsened = any(delta < 0.0 for delta in deltas)

    if improved and worsened:
        return "compensatory_improvement"
    return "direct_improvement"


def extract_fitness_improving_events(
    history: EvolutionHistory,
    contexts: Contexts,
    *,
    n_genes: int,
    run_label: str,
) -> list[FitnessImprovingEvent]:
    events: list[FitnessImprovingEvent] = []
    for accepted_step, previous, current in iter_accepted_change_records(history):
        category = classify_fitness_improving_move(previous, current, contexts)
        if category is None:
            continue

        previous_table = _transition_table_for_record(previous, n_genes)
        current_table = _transition_table_for_record(current, n_genes)
        events.append(
            FitnessImprovingEvent(
                run_label=run_label,
                accepted_step=accepted_step,
                generation=int(current["generation"]),
                category=category,
                genome_hamming_distance=hamming_distance(previous["genome"], current["genome"]),
                changed_transitions=count_changed_transitions(previous_table, current_table),
                context_path_changes=_count_context_path_changes(previous, current, contexts),
                attractor_cycle_changes=_count_context_attractor_cycles_changed(
                    previous, current, contexts
                ),
                fitness_delta=float(current["fitness"]) - float(previous["fitness"]),
            )
        )
    return events


def collect_fitness_improving_events(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> list[FitnessImprovingEvent]:
    events: list[FitnessImprovingEvent] = []
    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        events.extend(
            extract_fitness_improving_events(
                history,
                contexts,
                n_genes=n_genes,
                run_label=label,
            )
        )
    if not events:
        raise ValueError("no fitness-improving accepted genome changes found")
    return events


collect_non_neutral_move_events = collect_fitness_improving_events


def _average(values: list[float | int]) -> float:
    if not values:
        return 0.0
    return float(sum(values)) / len(values)


def build_fitness_improving_summary_rows(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> tuple[list[str], list[list[str | int | float]]]:
    events = collect_fitness_improving_events(histories, contexts, n_genes=n_genes, labels=labels)

    headers = [
        "category",
        "count",
        "pct_of_fitness_improving",
        "avg_fitness_delta",
    ]
    rows: list[list[str | int | float]] = []

    for category in FITNESS_IMPROVING_CATEGORY_ORDER:
        group = [event for event in events if event.category == category]
        count = len(group)
        if count == 0:
            continue
        rows.append(
            [
                FITNESS_IMPROVING_CATEGORY_LABELS[category],
                count,
                100.0 * count / len(events),
                _average([event.fitness_delta for event in group]),
            ]
        )

    rows.append(
        [
            "Total",
            len(events),
            100.0,
            _average([event.fitness_delta for event in events]),
        ]
    )
    return headers, rows


build_non_neutral_move_summary_rows = build_fitness_improving_summary_rows

FITNESS_IMPROVING_TABLE_COL_WIDTHS = [0.42, 0.12, 0.23, 0.23]


def _format_avg_fitness_delta(value: float) -> str:
    if value == 0.0:
        return "0.0000"
    if abs(value) < 0.001:
        formatted = f"{value:.5f}".rstrip("0").rstrip(".")
        return formatted if "." in formatted else f"{formatted}.0"
    return f"{value:.4f}"


def _style_summary_table(
    table,
    *,
    left_align_first_column: bool = True,
    header_fontsize: int = 7,
    body_fontsize: int = 8,
) -> None:
    table.auto_set_font_size(False)
    table.set_fontsize(body_fontsize)
    table.scale(1.0, 1.85)
    for (row, col), cell in table.get_celld().items():
        if row == 0:
            cell.set_text_props(ha="center", va="center", fontsize=header_fontsize)
            continue
        if col == 0 and left_align_first_column:
            cell.set_text_props(ha="left", va="center", fontsize=body_fontsize)
            cell.PAD = 0.03
        else:
            cell.set_text_props(ha="center", va="center", fontsize=body_fontsize)


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


def plot_fitness_improving_summary_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    if plt is None:
        raise ImportError("matplotlib is required for fitness-improving summary tables")

    headers, rows = build_fitness_improving_summary_rows(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_summary_csv(headers, rows, Path(csv_path))

    display_rows = [
        [
            str(row[0]),
            str(row[1]),
            f"{float(row[2]):.1f}%",
            _format_avg_fitness_delta(float(row[3])),
        ]
        for row in rows
    ]

    figure, axis = plt.subplots(figsize=(14, max(3.4, 0.55 * len(display_rows) + 1.4)))
    axis.axis("off")
    table = axis.table(
        cellText=display_rows,
        colLabels=headers,
        loc="center",
        cellLoc="center",
        colWidths=FITNESS_IMPROVING_TABLE_COL_WIDTHS,
    )
    table.auto_set_font_size(False)
    table.set_fontsize(10)
    table.scale(1.0, 1.8)
    figure.suptitle(
        "Fitness-improving accepted moves by category\n"
        f"{len(histories)} runs  |  fitness_delta > 0",
        fontsize=12,
    )
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path


plot_non_neutral_move_summary_table = plot_fitness_improving_summary_table
