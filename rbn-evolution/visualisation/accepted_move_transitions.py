"""Transition matrix between top-level accepted move categories."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from itertools import groupby
from pathlib import Path

try:
    import numpy as np
except ImportError:
    np = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.accepted_move_taxonomy import (
    TOP_LEVEL_MOVE_CATEGORY_ORDER,
    TopLevelMoveCategory,
    classify_top_level_accepted_move_category,
    iter_accepted_change_records,
)


@dataclass(frozen=True)
class AcceptedMoveTransitionRow:
    run_id: str
    accepted_step: int
    category: TopLevelMoveCategory
    next_category: TopLevelMoveCategory | None = None


def _require_numpy() -> None:
    if np is None:
        raise ImportError("numpy is required for accepted move transition tables")


def build_accepted_move_category_rows(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
) -> list[AcceptedMoveTransitionRow]:
    rows: list[AcceptedMoveTransitionRow] = []
    for run_index, history in enumerate(histories):
        run_id = labels[run_index] if labels is not None else f"run_{run_index + 1:03d}"
        for accepted_step, previous, current in iter_accepted_change_records(history):
            category = classify_top_level_accepted_move_category(
                previous, current, contexts
            )
            rows.append(
                AcceptedMoveTransitionRow(
                    run_id=run_id,
                    accepted_step=accepted_step,
                    category=category,
                )
            )

    rows.sort(key=lambda row: (row.run_id, row.accepted_step))
    return rows


def add_next_categories(
    rows: list[AcceptedMoveTransitionRow],
) -> list[AcceptedMoveTransitionRow]:
    enriched: list[AcceptedMoveTransitionRow] = []
    for _run_id, group in groupby(rows, key=lambda row: row.run_id):
        run_rows = list(group)
        for index, row in enumerate(run_rows):
            next_category = (
                run_rows[index + 1].category if index + 1 < len(run_rows) else None
            )
            enriched.append(
                AcceptedMoveTransitionRow(
                    run_id=row.run_id,
                    accepted_step=row.accepted_step,
                    category=row.category,
                    next_category=next_category,
                )
            )
    return enriched


def build_transition_rows(
    rows: list[AcceptedMoveTransitionRow],
) -> list[AcceptedMoveTransitionRow]:
    return [row for row in rows if row.next_category is not None]


def build_transition_matrices(
    transition_rows: list[AcceptedMoveTransitionRow],
) -> tuple[np.ndarray, np.ndarray]:
    _require_numpy()
    category_index = {
        category: index for index, category in enumerate(TOP_LEVEL_MOVE_CATEGORY_ORDER)
    }
    counts = np.zeros(
        (len(TOP_LEVEL_MOVE_CATEGORY_ORDER), len(TOP_LEVEL_MOVE_CATEGORY_ORDER)),
        dtype=float,
    )
    for row in transition_rows:
        counts[
            category_index[row.category],
            category_index[row.next_category],
        ] += 1.0

    row_totals = counts.sum(axis=1)
    percentages = np.zeros_like(counts)
    for row_index in range(len(TOP_LEVEL_MOVE_CATEGORY_ORDER)):
        if row_totals[row_index] > 0:
            percentages[row_index] = counts[row_index] / row_totals[row_index] * 100.0
    return counts, percentages


def _write_move_table_csv(
    rows: list[AcceptedMoveTransitionRow],
    csv_path: Path,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["run_id", "accepted_step", "category", "next_category"])
        for row in rows:
            writer.writerow(
                [
                    row.run_id,
                    row.accepted_step,
                    row.category,
                    row.next_category if row.next_category is not None else "",
                ]
            )


def _write_matrix_csv(
    matrix: np.ndarray,
    csv_path: Path,
    *,
    value_format: str,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["current_category", *TOP_LEVEL_MOVE_CATEGORY_ORDER])
        for row_index, current_category in enumerate(TOP_LEVEL_MOVE_CATEGORY_ORDER):
            writer.writerow(
                [
                    current_category,
                    *[
                        value_format.format(matrix[row_index, column_index])
                        for column_index in range(len(TOP_LEVEL_MOVE_CATEGORY_ORDER))
                    ],
                ]
            )


def export_accepted_move_transition_tables(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
    moves_csv_path: str | Path | None = None,
    counts_csv_path: str | Path | None = None,
    percentages_csv_path: str | Path | None = None,
) -> None:
    """Export accepted move category transition tables as CSV files."""
    if not histories:
        raise ValueError("histories must contain at least one run")

    all_rows = add_next_categories(build_accepted_move_category_rows(histories, contexts, labels=labels))
    transition_rows = build_transition_rows(all_rows)
    if not transition_rows:
        raise ValueError("no accepted move transitions found")

    counts, percentages = build_transition_matrices(transition_rows)

    if moves_csv_path is not None:
        _write_move_table_csv(all_rows, Path(moves_csv_path))
    if counts_csv_path is not None:
        _write_matrix_csv(counts, Path(counts_csv_path), value_format="{:.0f}")
    if percentages_csv_path is not None:
        _write_matrix_csv(
            percentages,
            Path(percentages_csv_path),
            value_format="{:.2f}",
        )
