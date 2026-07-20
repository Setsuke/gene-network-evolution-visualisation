"""Classification, summary table, and bar chart for accepted neutral moves."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

try:
    import matplotlib.pyplot as plt
    import numpy as np
except ImportError:
    plt = None
    np = None

from rbn.types import Contexts, EvolutionHistory, GenerationRecord, State
from visualisation.accepted_genome_change_helpers import (
    _count_context_attractor_cycles_changed,
    _transition_table_for_record,
)
from visualisation.accepted_move_taxonomy import (
    count_accepted_genome_changes,
    factor_scores_unchanged,
    is_fitness_neutral,
    iter_accepted_change_records,
)
from visualisation.factorised.metrics import context_order

NeutralMoveCategory = Literal[
    "no_transition_changed",
    "only_irrelevant_transitions_changed",
    "path_transitions_changed_attractor_cycle_unchanged",
    "attractor_cycle_changed_factor_scores_unchanged",
]

CATEGORY_ORDER: tuple[NeutralMoveCategory, ...] = (
    "no_transition_changed",
    "only_irrelevant_transitions_changed",
    "path_transitions_changed_attractor_cycle_unchanged",
    "attractor_cycle_changed_factor_scores_unchanged",
)

BAR_CATEGORY_ORDER: tuple[NeutralMoveCategory, ...] = CATEGORY_ORDER[1:]

CATEGORY_LABELS: dict[NeutralMoveCategory, str] = {
    "no_transition_changed": "No transition changed",
    "only_irrelevant_transitions_changed": "Only irrelevant transitions changed",
    "path_transitions_changed_attractor_cycle_unchanged": "Path transitions changed, attractor cycle unchanged",
    "attractor_cycle_changed_factor_scores_unchanged": "Attractor cycle changed, factor scores unchanged",
}

BAR_CHART_CATEGORY_LABELS: dict[NeutralMoveCategory, str] = {
    "only_irrelevant_transitions_changed": "Only irrelevant\ntransitions changed",
    "path_transitions_changed_attractor_cycle_unchanged": "Path changed,\ncycle unchanged",
    "attractor_cycle_changed_factor_scores_unchanged": "Cycle changed,\nfactor scores unchanged",
}

CATEGORY_COLORS: dict[NeutralMoveCategory, str] = {
    "no_transition_changed": "#cbd5e1",
    "only_irrelevant_transitions_changed": "#38bdf8",
    "path_transitions_changed_attractor_cycle_unchanged": "#f59e0b",
    "attractor_cycle_changed_factor_scores_unchanged": "#a855f7",
}


@dataclass(frozen=True)
class NeutralMoveEvent:
    run_label: str
    accepted_step: int
    generation: int
    category: NeutralMoveCategory


def _require_matplotlib():
    if plt is None or np is None:
        raise ImportError("matplotlib and numpy are required for neutral move plots")


def _changed_transition_states(
    previous: GenerationRecord,
    current: GenerationRecord,
    n_genes: int,
) -> set[State]:
    previous_table = _transition_table_for_record(previous, n_genes)
    current_table = _transition_table_for_record(current, n_genes)
    return {
        state
        for state in previous_table
        if previous_table[state] != current_table[state]
    }


def _relevant_states(record: GenerationRecord, contexts: Contexts) -> set[State]:
    relevant: set[State] = set()
    for initial in context_order(contexts):
        details = record["details"][initial]
        relevant.update(details["path"])
        relevant.update(details["cycle"])
    return relevant


def _factor_scores_unchanged(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> bool:
    return factor_scores_unchanged(previous, current, contexts)


def classify_factor_neutral_accepted_move(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
    *,
    n_genes: int,
) -> NeutralMoveCategory | None:
    """
    Classify factor-neutral accepted moves.

    Requires fitness_delta = 0 and all factor scores unchanged.
    """
    if not is_fitness_neutral(previous, current):
        return None
    if not factor_scores_unchanged(previous, current, contexts):
        return None

    changed_states = _changed_transition_states(previous, current, n_genes)
    if not changed_states:
        return "no_transition_changed"

    attractor_cycle_changed = _count_context_attractor_cycles_changed(previous, current, contexts) > 0
    if attractor_cycle_changed:
        return "attractor_cycle_changed_factor_scores_unchanged"

    relevant = _relevant_states(previous, contexts)
    if changed_states & relevant:
        return "path_transitions_changed_attractor_cycle_unchanged"
    return "only_irrelevant_transitions_changed"


def classify_neutral_move(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
    *,
    n_genes: int,
) -> NeutralMoveCategory | None:
    return classify_factor_neutral_accepted_move(
        previous, current, contexts, n_genes=n_genes
    )


def extract_factor_neutral_accepted_events(
    history: EvolutionHistory,
    contexts: Contexts,
    *,
    n_genes: int,
    run_label: str,
) -> list[NeutralMoveEvent]:
    events: list[NeutralMoveEvent] = []
    for accepted_step, previous, current in iter_accepted_change_records(history):
        category = classify_factor_neutral_accepted_move(
            previous, current, contexts, n_genes=n_genes
        )
        if category is None:
            continue
        events.append(
            NeutralMoveEvent(
                run_label=run_label,
                accepted_step=accepted_step,
                generation=int(current["generation"]),
                category=category,
            )
        )
    return events


def extract_neutral_move_events(
    history: EvolutionHistory,
    contexts: Contexts,
    *,
    n_genes: int,
    run_label: str,
) -> list[NeutralMoveEvent]:
    return extract_factor_neutral_accepted_events(
        history, contexts, n_genes=n_genes, run_label=run_label
    )


def collect_neutral_move_events(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> list[NeutralMoveEvent]:
    events: list[NeutralMoveEvent] = []
    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        events.extend(
            extract_neutral_move_events(
                history,
                contexts,
                n_genes=n_genes,
                run_label=label,
            )
        )
    return events


def collect_factor_neutral_accepted_events(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> list[NeutralMoveEvent]:
    return collect_neutral_move_events(histories, contexts, n_genes=n_genes, labels=labels)


def build_neutral_move_summary_rows(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> tuple[list[str], list[list[str | int | float]]]:
    events = collect_neutral_move_events(histories, contexts, n_genes=n_genes, labels=labels)
    total_accepted_changes = count_accepted_genome_changes(histories)
    neutral_count = len(events)
    counts = {category: 0 for category in CATEGORY_ORDER}
    for event in events:
        counts[event.category] += 1

    headers = [
        "category",
        "count",
        "pct_of_factor_neutral_accepted",
        "pct_of_all_accepted_changes",
    ]
    rows: list[list[str | int | float]] = []
    for category in CATEGORY_ORDER:
        count = counts[category]
        rows.append(
            [
                CATEGORY_LABELS[category],
                count,
                100.0 * count / neutral_count if neutral_count else 0.0,
                100.0 * count / total_accepted_changes if total_accepted_changes else 0.0,
            ]
        )
    rows.append(
        [
            "All factor-neutral accepted moves",
            neutral_count,
            100.0 if neutral_count else 0.0,
            100.0 * neutral_count / total_accepted_changes if total_accepted_changes else 0.0,
        ]
    )
    return headers, rows


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


def plot_neutral_move_summary_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    _require_matplotlib()

    headers, rows = build_neutral_move_summary_rows(
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
            f"{float(row[3]):.1f}%",
        ]
        for row in rows
    ]

    figure, axis = plt.subplots(figsize=(14, 3.4))
    axis.axis("off")
    table = axis.table(
        cellText=display_rows,
        colLabels=headers,
        loc="center",
        cellLoc="center",
        colWidths=[0.42, 0.12, 0.23, 0.23],
    )
    table.auto_set_font_size(False)
    table.set_fontsize(10)
    table.scale(1.0, 1.8)
    figure.suptitle(
        "Factor-neutral accepted moves by category\n"
        f"{len(histories)} runs  |  fitness_delta = 0 and all factor scores unchanged",
        fontsize=12,
    )
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path


def plot_neutral_move_bar_chart(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
) -> Path:
    """Bar chart of neutral move categories as % of all neutral accepted changes."""
    if plt is None:
        raise ImportError("matplotlib is required for neutral move bar charts")

    headers, rows = build_neutral_move_summary_rows(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    row_by_label = {str(row[0]): row for row in rows}
    total_row = row_by_label["All factor-neutral accepted moves"]
    neutral_pct_of_all = float(total_row[3])

    categories = BAR_CATEGORY_ORDER
    percentages = [
        float(row_by_label[CATEGORY_LABELS[category]][2]) for category in categories
    ]
    counts = [int(row_by_label[CATEGORY_LABELS[category]][1]) for category in categories]
    colors = [CATEGORY_COLORS[category] for category in categories]
    x_labels = [BAR_CHART_CATEGORY_LABELS[category] for category in categories]

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    figure, axis = plt.subplots(figsize=(10.5, 7.2))
    bars = axis.bar(
        np.arange(len(categories)),
        percentages,
        color=colors,
        edgecolor="white",
        linewidth=0.8,
        width=0.58,
    )
    axis.set_xticks(np.arange(len(categories)))
    axis.set_xticklabels(x_labels, fontsize=10, linespacing=1.25)
    axis.set_ylabel("Percentage of factor-neutral accepted moves")
    axis.set_xlabel("Category", labelpad=14)
    axis.set_ylim(0.0, max(percentages) * 1.18 if percentages else 1.0)
    axis.set_title("Breakdown of factor-neutral accepted moves")
    axis.grid(axis="y", alpha=0.25)
    axis.tick_params(axis="x", pad=6)

    for bar, percentage, count in zip(bars, percentages, counts):
        axis.text(
            bar.get_x() + bar.get_width() / 2.0,
            bar.get_height() + 1.0,
            f"{percentage:.1f}%\n(n={count})",
            ha="center",
            va="bottom",
            fontsize=10,
        )

    figure.subplots_adjust(bottom=0.28)
    figure.text(
        0.5,
        0.05,
        (
            f"Factor-neutral accepted moves represent {neutral_pct_of_all:.2f}% of all accepted "
            f"genome changes across {len(histories)} runs."
        ),
        ha="center",
        fontsize=10,
    )

    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.35)
    plt.close(figure)
    return save_path
