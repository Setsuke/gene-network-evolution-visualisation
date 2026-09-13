"""Occupancy of total fitness on accepted pre-solution genomes across all runs."""

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
class MostDriftingFitnessRow:
    rank: int
    total_fitness: float
    count: int
    percentage: float


@dataclass(frozen=True)
class MostDriftingFitnessSummary:
    runs_total: int
    runs_reaching_perfect_fitness: int
    accepted_genomes_total: int
    unique_fitness_values: int
    rows: tuple[MostDriftingFitnessRow, ...]


def collect_pre_solution_accepted_fitness_values(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    fitness_round_digits: int = 6,
) -> tuple[list[float], int, int]:
    """
    Collect total fitness from every accepted genome with fitness < 1.0.

    Only runs whose final accepted fitness is 1.0 contribute genomes.
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


def build_most_drifting_fitness_summary(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    fitness_round_digits: int = 6,
    top_n: int = 5,
) -> MostDriftingFitnessSummary:
    values, runs_total, runs_reaching_perfect = collect_pre_solution_accepted_fitness_values(
        histories,
        contexts,
        fitness_round_digits=fitness_round_digits,
    )
    counts = Counter(values)
    accepted_genomes_total = len(values)

    sorted_items = sorted(
        counts.items(),
        key=lambda item: (-item[1], -item[0]),
    )[:top_n]

    rows = tuple(
        MostDriftingFitnessRow(
            rank=index + 1,
            total_fitness=fitness,
            count=count,
            percentage=(100.0 * count / accepted_genomes_total) if accepted_genomes_total else 0.0,
        )
        for index, (fitness, count) in enumerate(sorted_items)
    )

    return MostDriftingFitnessSummary(
        runs_total=runs_total,
        runs_reaching_perfect_fitness=runs_reaching_perfect,
        accepted_genomes_total=accepted_genomes_total,
        unique_fitness_values=len(counts),
        rows=rows,
    )


def _write_summary_csv(summary: MostDriftingFitnessSummary, csv_path: Path) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["rank", "total_fitness", "genome_count", "percentage_of_genomes"])
        for row in summary.rows:
            writer.writerow(
                [
                    row.rank,
                    f"{row.total_fitness:.6f}",
                    row.count,
                    f"{row.percentage:.4f}",
                ]
            )


def plot_most_drifting_fitness_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    csv_path: str | Path | None = None,
    top_n: int = 5,
) -> Path:
    if plt is None:
        raise ImportError("matplotlib is required for most drifting fitness tables")

    summary = build_most_drifting_fitness_summary(
        histories,
        contexts,
        top_n=top_n,
    )
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_summary_csv(summary, Path(csv_path))

    headers = ["Rank", "Total fitness", "Accepted genomes", "% of genomes"]
    table_rows = [
        [
            row.rank,
            f"{row.total_fitness:.6f}",
            row.count,
            f"{row.percentage:.2f}%",
        ]
        for row in summary.rows
    ]

    figure_height = max(3.6, 0.42 * len(table_rows) + 1.8)
    figure, axis = plt.subplots(figsize=(10.5, figure_height))
    axis.axis("off")
    table = axis.table(
        cellText=table_rows,
        colLabels=headers,
        loc="center",
        cellLoc="center",
        colWidths=[0.12, 0.28, 0.22, 0.22],
    )
    _style_summary_table(
        table,
        left_align_first_column=False,
        header_fontsize=9,
        body_fontsize=9,
    )

    row_note = (
        f"Top {top_n} of {summary.unique_fitness_values} unique fitness values"
        if summary.unique_fitness_values > top_n
        else f"{summary.unique_fitness_values} unique fitness values"
    )

    figure.suptitle(
        "Most drifting fitness (accepted genomes before reaching 1.0)\n"
        f"{summary.runs_reaching_perfect_fitness}/{summary.runs_total} runs reached "
        f"fitness 1.0  |  {summary.accepted_genomes_total} accepted pre-solution genomes  |  "
        f"{row_note}",
        fontsize=11,
        y=0.98,
    )
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
