"""Metrics for accepted genomes along a factorised fitness trajectory."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path

from rbn.fitness import compute_gene_scores
from rbn.types import Contexts, EvolutionHistory, GenerationRecord, Genome, State


@dataclass(frozen=True)
class AcceptedStepMetrics:
    step_index: int
    generation: int
    total_fitness: float
    context_fitness: tuple[float, ...]
    gene_scores: tuple[tuple[float, ...], ...]
    bottleneck_score: float
    number_of_perfect_factors: int
    cycle_lengths: tuple[int, ...]


def context_order(contexts: Contexts) -> list[State]:
    return list(contexts.keys())


def factor_row_labels(contexts: Contexts, n_genes: int) -> list[str]:
    labels: list[str] = []
    for context_index, _initial in enumerate(context_order(contexts), start=1):
        for gene_index in range(n_genes):
            labels.append(f"Context {context_index} - Gene {gene_index}")
    return labels


def _gene_scores_for_context(
    record: GenerationRecord,
    initial: State,
    target: State,
) -> list[float]:
    details = record["details"][initial]
    stored = details.get("gene_scores")
    if stored is not None:
        return [float(score) for score in stored]
    return compute_gene_scores(list(details["cycle"]), target)


def metrics_from_record(
    record: GenerationRecord,
    contexts: Contexts,
    *,
    step_index: int,
) -> AcceptedStepMetrics:
    inits = context_order(contexts)
    context_fitness_values: list[float] = []
    gene_score_rows: list[tuple[float, ...]] = []
    cycle_lengths: list[int] = []
    flat_scores: list[float] = []

    for initial in inits:
        details = record["details"][initial]
        target = contexts[initial]
        scores = _gene_scores_for_context(record, initial, target)
        gene_score_rows.append(tuple(scores))
        flat_scores.extend(scores)
        context_fitness_values.append(float(details["fitness"]))
        stored_length = details.get("cycle_length")
        cycle_lengths.append(
            int(stored_length) if stored_length is not None else len(details["cycle"])
        )

    perfect_count = sum(1 for score in flat_scores if score == 1.0)
    return AcceptedStepMetrics(
        step_index=step_index,
        generation=int(record["generation"]),
        total_fitness=float(record["fitness"]),
        context_fitness=tuple(context_fitness_values),
        gene_scores=tuple(gene_score_rows),
        bottleneck_score=min(flat_scores) if flat_scores else 0.0,
        number_of_perfect_factors=perfect_count,
        cycle_lengths=tuple(cycle_lengths),
    )


def extract_accepted_trajectory_metrics(
    history: EvolutionHistory,
    contexts: Contexts,
) -> list[AcceptedStepMetrics]:
    """One metrics row per accepted genome change along the trajectory."""
    if not history:
        return []

    metrics: list[AcceptedStepMetrics] = []
    last_genome: Genome | None = None

    for record in history:
        genome = record["genome"]
        if last_genome is None or genome != last_genome:
            metrics.append(
                metrics_from_record(
                    record,
                    contexts,
                    step_index=len(metrics),
                )
            )
            last_genome = genome
            if record["fitness"] == 1.0:
                break

    if history[-1]["genome"] != last_genome and (
        not metrics
        or metrics[-1].total_fitness != float(history[-1]["fitness"])
    ):
        metrics.append(
            metrics_from_record(
                history[-1],
                contexts,
                step_index=len(metrics),
            )
        )

    return metrics


def build_factor_score_matrix(
    metrics: list[AcceptedStepMetrics],
) -> list[list[float]]:
    if not metrics:
        return []

    n_contexts = len(metrics[0].gene_scores)
    n_genes = len(metrics[0].gene_scores[0])
    matrix: list[list[float]] = []

    for context_index in range(n_contexts):
        for gene_index in range(n_genes):
            matrix.append(
                [
                    step.gene_scores[context_index][gene_index]
                    for step in metrics
                ]
            )

    return matrix


def export_factorised_trajectory_csv(
    metrics: list[AcceptedStepMetrics],
    contexts: Contexts,
    n_genes: int,
    csv_path: str | Path,
) -> Path:
    csv_path = Path(csv_path)
    csv_path.parent.mkdir(parents=True, exist_ok=True)

    context_inits = context_order(contexts)
    headers = [
        "accepted_step",
        "generation",
        "total_fitness",
        "bottleneck_score",
        "number_of_perfect_factors",
    ]
    for index in range(len(context_inits)):
        headers.append(f"context_fitness_{index + 1}")
    for index in range(len(context_inits)):
        headers.append(f"cycle_length_{index + 1}")
    for context_index in range(len(context_inits)):
        for gene_index in range(n_genes):
            headers.append(f"context_{context_index + 1}_gene_{gene_index}")

    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(headers)
        for step in metrics:
            row: list[int | float] = [
                step.step_index,
                step.generation,
                step.total_fitness,
                step.bottleneck_score,
                step.number_of_perfect_factors,
                *step.context_fitness,
                *step.cycle_lengths,
            ]
            for context_scores in step.gene_scores:
                row.extend(context_scores)
            writer.writerow(row)

    return csv_path


PROGRESS_POINTS = tuple(range(0, 101, 5))


def perfect_factors_at_progress(
    metrics: list[AcceptedStepMetrics],
    progress_percent: int,
) -> int:
    if not metrics:
        return 0
    if len(metrics) == 1:
        return metrics[0].number_of_perfect_factors
    index = round((progress_percent / 100.0) * (len(metrics) - 1))
    return metrics[index].number_of_perfect_factors


def sample_perfect_factors_trajectory(
    metrics: list[AcceptedStepMetrics],
) -> tuple[int, ...]:
    return tuple(
        perfect_factors_at_progress(metrics, progress)
        for progress in PROGRESS_POINTS
    )


def generations_to_solution(metrics: list[AcceptedStepMetrics]) -> int:
    for step in metrics:
        if step.total_fitness == 1.0:
            return step.generation
    return metrics[-1].generation if metrics else 0


def plateau_duration(metrics: list[AcceptedStepMetrics]) -> int:
    """Accepted steps spent at unchanged total fitness beyond the first step."""
    if len(metrics) <= 1:
        return 0

    total = 0
    plateau_start = 0
    for index in range(1, len(metrics)):
        if metrics[index].total_fitness != metrics[index - 1].total_fitness:
            plateau_length = index - plateau_start
            if plateau_length > 1:
                total += plateau_length - 1
            plateau_start = index

    final_plateau = len(metrics) - plateau_start
    if final_plateau > 1:
        total += final_plateau - 1
    return total
