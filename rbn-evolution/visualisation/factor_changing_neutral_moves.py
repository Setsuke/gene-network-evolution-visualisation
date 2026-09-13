"""Factor-changing neutral accepted moves: unchanged fitness with at least one factor score change."""

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
    count_accepted_genome_changes,
    factor_score_deltas,
    factor_scores_unchanged,
    is_fitness_neutral,
    iter_accepted_change_records,
)
from visualisation.evolutionary_landscape import count_changed_transitions, hamming_distance
from visualisation.factorised.metrics import context_order

FactorChangingNeutralCategory = Literal[
    "compensatory_neutral",
    "zero_masked_improvement",
    "zero_masked_deterioration",
    "numerical_edge_case",
]

FACTOR_CHANGING_CATEGORY_ORDER: tuple[FactorChangingNeutralCategory, ...] = (
    "compensatory_neutral",
    "zero_masked_improvement",
    "zero_masked_deterioration",
    "numerical_edge_case",
)

COMPENSATORY_NEUTRAL_CHANGES = "Compensatory neutral changes"
ZERO_MASKED_IMPROVEMENT = "Zero masked improvement"
ZERO_MASKED_DETERIORATION = "Zero masked deterioration"
ZERO_MASKED_IMPROVEMENT_DETERIORATION = "Zero masked improvement/deterioration"
OTHER_FACTOR_CHANGING_NEUTRAL_MOVES = "Other factor-changing neutral moves"

ZERO_MASKED_CATEGORIES: frozenset[FactorChangingNeutralCategory] = frozenset(
    {"zero_masked_improvement", "zero_masked_deterioration"}
)

FACTOR_CHANGING_CHART_GROUPS: tuple[tuple[str, str, tuple[FactorChangingNeutralCategory, ...]], ...] = (
    ("compensatory_neutral", COMPENSATORY_NEUTRAL_CHANGES, ("compensatory_neutral",)),
    (
        "zero_masked",
        ZERO_MASKED_IMPROVEMENT_DETERIORATION,
        ("zero_masked_improvement", "zero_masked_deterioration"),
    ),
    ("numerical_edge_case", OTHER_FACTOR_CHANGING_NEUTRAL_MOVES, ("numerical_edge_case",)),
)

FACTOR_CHANGING_CATEGORY_LABELS: dict[FactorChangingNeutralCategory, str] = {
    "compensatory_neutral": COMPENSATORY_NEUTRAL_CHANGES,
    "zero_masked_improvement": ZERO_MASKED_IMPROVEMENT,
    "zero_masked_deterioration": ZERO_MASKED_DETERIORATION,
    "numerical_edge_case": OTHER_FACTOR_CHANGING_NEUTRAL_MOVES,
}

FACTOR_CHANGING_CATEGORY_MEANINGS: dict[FactorChangingNeutralCategory, str] = {
    "compensatory_neutral": (
        "Some factor scores improve and others worsen, while total fitness remains unchanged"
    ),
    "zero_masked_improvement": (
        "Some factor scores improve while total fitness remains unchanged because "
        "at least one factor score remains zero"
    ),
    "zero_masked_deterioration": (
        "Some factor scores worsen while total fitness remains unchanged because "
        "at least one factor score remains zero"
    ),
    "numerical_edge_case": (
        "Factor scores change without a clear mixed compensation or unidirectional pattern"
    ),
}

ZERO_MASKED_CHART_MEANING = (
    "Factor scores change in one direction while total fitness remains unchanged because "
    "at least one factor score remains zero"
)

FACTOR_CHANGING_TABLE_HEADERS = [
    "Category",
    "n",
    "Meaning",
]

FACTOR_CHANGING_TABLE_COL_WIDTHS = [0.30, 0.07, 0.63]


@dataclass(frozen=True)
class FactorChangingNeutralEvent:
    run_label: str
    accepted_step: int
    generation: int
    category: FactorChangingNeutralCategory
    genome_hamming_distance: int
    changed_transitions: int
    context_path_changes: int
    attractor_cycle_changes: int


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


def classify_factor_changing_neutral_move(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> FactorChangingNeutralCategory | None:
    if not is_fitness_neutral(previous, current):
        return None
    if factor_scores_unchanged(previous, current, contexts):
        return None

    deltas = factor_score_deltas(previous, current, contexts)
    improved = any(delta > 0.0 for delta in deltas)
    worsened = any(delta < 0.0 for delta in deltas)
    if improved and worsened:
        return "compensatory_neutral"
    if improved:
        return "zero_masked_improvement"
    if worsened:
        return "zero_masked_deterioration"
    return "numerical_edge_case"


def extract_factor_changing_neutral_events(
    history: EvolutionHistory,
    contexts: Contexts,
    *,
    n_genes: int,
    run_label: str,
) -> list[FactorChangingNeutralEvent]:
    events: list[FactorChangingNeutralEvent] = []
    for accepted_step, previous, current in iter_accepted_change_records(history):
        category = classify_factor_changing_neutral_move(previous, current, contexts)
        if category is None:
            continue
        previous_table = _transition_table_for_record(previous, n_genes)
        current_table = _transition_table_for_record(current, n_genes)
        events.append(
            FactorChangingNeutralEvent(
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
            )
        )
    return events


def collect_factor_changing_neutral_events(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> list[FactorChangingNeutralEvent]:
    events: list[FactorChangingNeutralEvent] = []
    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        events.extend(
            extract_factor_changing_neutral_events(
                history,
                contexts,
                n_genes=n_genes,
                run_label=label,
            )
        )
    return events


def _average(values: list[float | int]) -> float:
    if not values:
        return 0.0
    return float(sum(values)) / len(values)


def build_factor_changing_neutral_summary_rows(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> tuple[list[str], list[list[str | int | float]]]:
    events = collect_factor_changing_neutral_events(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    total = count_accepted_genome_changes(histories)
    fitness_neutral_total = sum(
        1
        for history in histories
        for _, previous, current in iter_accepted_change_records(history)
        if is_fitness_neutral(previous, current)
    )

    headers = [
        "category",
        "count",
        "meaning",
        "pct_of_factor_changing_neutral",
        "pct_of_fitness_neutral_accepted",
        "pct_all_accepted",
        "avg_hamming",
        "avg_transitions",
        "avg_path_changes",
        "avg_attractor_cycle_changes",
    ]
    rows: list[list[str | int | float]] = []
    for group_key, label, categories in FACTOR_CHANGING_CHART_GROUPS:
        group = [event for event in events if event.category in categories]
        count = len(group)
        if count == 0:
            continue
        meaning = (
            ZERO_MASKED_CHART_MEANING
            if group_key == "zero_masked"
            else FACTOR_CHANGING_CATEGORY_MEANINGS[categories[0]]
        )
        rows.append(
            [
                label,
                count,
                meaning,
                100.0 * count / len(events) if events else 0.0,
                100.0 * count / fitness_neutral_total if fitness_neutral_total else 0.0,
                100.0 * count / total if total else 0.0,
                _average([event.genome_hamming_distance for event in group]),
                _average([event.changed_transitions for event in group]),
                _average([event.context_path_changes for event in group]),
                _average([event.attractor_cycle_changes for event in group]),
            ]
        )
    rows.append(
        [
            "All factor-changing neutral accepted moves",
            len(events),
            "Accepted moves with unchanged total fitness and at least one changed factor score",
            100.0 if events else 0.0,
            100.0 * len(events) / fitness_neutral_total if fitness_neutral_total else 0.0,
            100.0 * len(events) / total if total else 0.0,
            _average([event.genome_hamming_distance for event in events]),
            _average([event.changed_transitions for event in events]),
            _average([event.context_path_changes for event in events]),
            _average([event.attractor_cycle_changes for event in events]),
        ]
    )
    return headers, rows


def plot_factor_changing_neutral_summary_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    if plt is None:
        raise ImportError("matplotlib is required for factor-changing neutral move tables")

    from visualisation.non_neutral_moves import _style_summary_table

    headers, rows = build_factor_changing_neutral_summary_rows(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    if csv_path is not None:
        csv_path = Path(csv_path)
        csv_path.parent.mkdir(parents=True, exist_ok=True)
        with csv_path.open("w", encoding="utf-8", newline="") as handle:
            writer = csv.writer(handle)
            writer.writerow(headers)
            writer.writerows(rows)

    display_rows = [
        [str(row[0]), str(row[1]), str(row[2])]
        for row in rows
    ]

    figure, axis = plt.subplots(figsize=(14, max(4.2, 0.75 * len(display_rows) + 2.0)))
    axis.axis("off")
    table = axis.table(
        cellText=display_rows,
        colLabels=FACTOR_CHANGING_TABLE_HEADERS,
        loc="upper center",
        cellLoc="center",
        colWidths=FACTOR_CHANGING_TABLE_COL_WIDTHS,
        bbox=[0.03, 0.22, 0.94, 0.62],
    )
    _style_summary_table(table, header_fontsize=8, body_fontsize=8)
    for (row, col), cell in table.get_celld().items():
        if col == 2 and row > 0:
            cell.set_text_props(ha="left", va="center", fontsize=8)
            cell.PAD = 0.04
        if col == 0 and row > 0:
            cell.set_text_props(ha="left", va="center", fontsize=8)

    group_pct = float(rows[-1][5]) if rows else 0.0
    figure.suptitle(
        "Factor-changing neutral accepted moves\n"
        f"{len(histories)} runs  |  fitness_delta = 0 and at least one factor score changed",
        fontsize=12,
        y=0.98,
    )
    figure.text(
        0.5,
        0.08,
        (
            f"Factor-changing neutral accepted moves represent {group_pct:.2f}% of all accepted "
            f"genome changes across {len(histories)} runs."
        ),
        ha="center",
        fontsize=10,
    )
    figure.subplots_adjust(left=0.03, right=0.97, top=0.86, bottom=0.12)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.35)
    plt.close(figure)
    return save_path
