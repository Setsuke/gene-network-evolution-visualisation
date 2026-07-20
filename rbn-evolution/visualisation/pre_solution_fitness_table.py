"""Distribution of total fitness values on accepted steps before fitness reaches 1.0."""

from __future__ import annotations

import csv
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.factorised.metrics import extract_accepted_trajectory_metrics
from visualisation.non_neutral_moves import _style_summary_table


@dataclass(frozen=True)
class PreSolutionFitnessRow:
    rank: int
    total_fitness: float
    count: int
    percentage: float


@dataclass(frozen=True)
class PreSolutionFitnessSummary:
    runs_total: int
    runs_reaching_perfect_fitness: int
    pre_solution_steps: int
    unique_fitness_values: int
    rows: tuple[PreSolutionFitnessRow, ...]


def collect_pre_solution_fitness_values(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    fitness_round_digits: int = 6,
) -> tuple[list[float], int, int]:
    """
    Collect total fitness from accepted trajectory steps with fitness < 1.0.

    Only runs whose final accepted fitness is 1.0 contribute steps.
    """
    values: list[float] = []
    runs_reaching_perfect = 0

    for history in histories:
        if not history:
            continue

        final_fitness = float(history[-1]["fitness"])
        if final_fitness != 1.0:
            continue

        runs_reaching_perfect += 1
        metrics = extract_accepted_trajectory_metrics(history, contexts)
        for step in metrics:
            fitness = round(float(step.total_fitness), fitness_round_digits)
            if fitness >= 1.0:
                continue
            values.append(fitness)

    return values, len(histories), runs_reaching_perfect


def build_pre_solution_fitness_summary(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    fitness_round_digits: int = 6,
) -> PreSolutionFitnessSummary:
    values, runs_total, runs_reaching_perfect = collect_pre_solution_fitness_values(
        histories,
        contexts,
        fitness_round_digits=fitness_round_digits,
    )
    counts = Counter(values)
    total_steps = len(values)

    sorted_items = sorted(
        counts.items(),
        key=lambda item: (-item[1], -item[0]),
    )

    rows = tuple(
        PreSolutionFitnessRow(
            rank=index + 1,
            total_fitness=fitness,
            count=count,
            percentage=(100.0 * count / total_steps) if total_steps else 0.0,
        )
        for index, (fitness, count) in enumerate(sorted_items)
    )

    return PreSolutionFitnessSummary(
        runs_total=runs_total,
        runs_reaching_perfect_fitness=runs_reaching_perfect,
        pre_solution_steps=total_steps,
        unique_fitness_values=len(rows),
        rows=rows,
    )


def _write_summary_csv(summary: PreSolutionFitnessSummary, csv_path: Path) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["rank", "total_fitness", "count", "percentage"])
        for row in summary.rows:
            writer.writerow(
                [
                    row.rank,
                    f"{row.total_fitness:.6f}",
                    row.count,
                    f"{row.percentage:.4f}",
                ]
            )


def plot_pre_solution_fitness_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    csv_path: str | Path | None = None,
    max_rows: int = 5,
) -> Path:
    if plt is None:
        raise ImportError("matplotlib is required for pre-solution fitness tables")

    summary = build_pre_solution_fitness_summary(histories, contexts)
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_summary_csv(summary, Path(csv_path))

    display_rows = summary.rows[:max_rows]
    headers = ["Rank", "Total fitness", "Count", "Percentage"]
    table_rows = [
        [
            row.rank,
            f"{row.total_fitness:.6f}",
            row.count,
            f"{row.percentage:.2f}%",
        ]
        for row in display_rows
    ]

    figure_height = max(3.6, 0.42 * len(table_rows) + 1.8)
    figure, axis = plt.subplots(figsize=(10.5, figure_height))
    axis.axis("off")
    table = axis.table(
        cellText=table_rows,
        colLabels=headers,
        loc="center",
        cellLoc="center",
        colWidths=[0.12, 0.28, 0.18, 0.22],
    )
    _style_summary_table(
        table,
        left_align_first_column=False,
        header_fontsize=9,
        body_fontsize=9,
    )

    if summary.unique_fitness_values > max_rows:
        row_note = (
            f"Showing top {max_rows} of {summary.unique_fitness_values} "
            "unique fitness values"
        )
    else:
        row_note = f"{summary.unique_fitness_values} unique fitness values"

    figure.suptitle(
        "Most common total fitness values before reaching 1.0\n"
        f"{summary.runs_reaching_perfect_fitness}/{summary.runs_total} runs reached "
        f"fitness 1.0  |  {summary.pre_solution_steps} pre-solution accepted steps  |  "
        f"{row_note}",
        fontsize=11,
        y=0.98,
    )
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
