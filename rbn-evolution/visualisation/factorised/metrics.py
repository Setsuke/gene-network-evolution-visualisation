"""Metrics for accepted genomes along a factorised fitness trajectory."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path

from rbn.fitness import build_factor_scores, compute_gene_scores
from rbn.types import Contexts, EvolutionHistory, GenerationRecord, Genome, State


@dataclass(frozen=True)
class AcceptedStepMetrics:
    step_index: int
    generation: int
    genome: str
    total_fitness: float
    context_fitness: tuple[float, ...]
    factor_scores: tuple[float, ...]
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

    for initial in inits:
        details = record["details"][initial]
        target = contexts[initial]
        scores = _gene_scores_for_context(record, initial, target)
        gene_score_rows.append(tuple(scores))
        context_fitness_values.append(float(details["fitness"]))
        stored_length = details.get("cycle_length")
        cycle_lengths.append(
            int(stored_length) if stored_length is not None else len(details["cycle"])
        )

    stored_factor_scores = record.get("factor_scores")
    if stored_factor_scores is not None:
        factor_scores = tuple(float(score) for score in stored_factor_scores)
    else:
        factor_scores = tuple(build_factor_scores(record["details"], contexts))

    perfect_count = sum(1 for score in factor_scores if score == 1.0)
    return AcceptedStepMetrics(
        step_index=step_index,
        generation=int(record["generation"]),
        genome=record["genome"],
        total_fitness=float(record["fitness"]),
        context_fitness=tuple(context_fitness_values),
        factor_scores=factor_scores,
        gene_scores=tuple(gene_score_rows),
        bottleneck_score=min(factor_scores) if factor_scores else 0.0,
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

    factor_count = len(metrics[0].factor_scores)
    return [
        [step.factor_scores[factor_index] for step in metrics]
        for factor_index in range(factor_count)
    ]


def build_factor_delta_matrix(
    metrics: list[AcceptedStepMetrics],
) -> list[list[float]]:
    """
    Per-factor score change between consecutive accepted steps.

    Column 0 is always 0.0 (no prior accepted genome to compare against).
    Column i > 0 is factor_scores[i] - factor_scores[i - 1].
    """
    score_matrix = build_factor_score_matrix(metrics)
    if not score_matrix:
        return []

    step_count = len(score_matrix[0])
    return [
        [
            0.0,
            *(
                scores[step_index] - scores[step_index - 1]
                for step_index in range(1, step_count)
            ),
        ]
        for scores in score_matrix
    ]


def export_factorised_trajectory_csv(
    metrics: list[AcceptedStepMetrics],
    contexts: Contexts,
    n_genes: int,
    csv_path: str | Path,
) -> Path:
    csv_path = Path(csv_path)
    csv_path.parent.mkdir(parents=True, exist_ok=True)

    context_inits = context_order(contexts)
    factor_labels = factor_row_labels(contexts, n_genes)
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
    headers.extend(factor_labels)

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
                *step.factor_scores,
            ]
            writer.writerow(row)

    return csv_path
