"""Export evolution history and network data to JSON for analysis or Unity."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

from rbn.fitness import build_factor_scores
from rbn.types import Contexts, GenerationRecord, State, TransitionTable
from visualisation.factorised.metrics import (
    extract_accepted_trajectory_metrics,
    factor_row_labels,
)

SCHEMA_VERSION = 1


def state_to_index(state: State, n_genes: int) -> int:
    return int(state, 2)


def index_to_state(index: int, n_genes: int) -> State:
    return format(index, f"0{n_genes}b")


def transition_table_to_edge_to(
    transition_table: TransitionTable,
    n_genes: int,
) -> list[int]:
    """edgeTo[i] = destination index of state i (lexicographic binary order)."""
    state_count = 1 << n_genes
    edge_to: list[int] = []
    for index in range(state_count):
        state = index_to_state(index, n_genes)
        edge_to.append(state_to_index(transition_table[state], n_genes))
    return edge_to


def _serialize_context_details(
    details: dict[State, dict[str, object]],
    contexts: Contexts,
) -> list[dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    for initial, target in contexts.items():
        context_details = details[initial]
        path = context_details["path"]
        cycle = context_details["cycle"]
        entries.append(
            {
                "initial": initial,
                "target": target,
                "path": list(path),
                "cycle": list(cycle),
                "fitness": float(context_details["fitness"]),
                "geneScores": list(context_details.get("gene_scores", [])),
                "cycleLength": int(
                    context_details.get("cycle_length", len(cycle))
                ),
            }
        )
    return entries


def _deserialize_context_details(
    context_records: list[dict[str, Any]],
) -> dict[State, dict[str, object]]:
    details: dict[State, dict[str, object]] = {}
    for context_record in context_records:
        initial = context_record["initial"]
        cycle = list(context_record["cycle"])
        target = context_record["target"]
        gene_scores = context_record.get("geneScores")
        details[initial] = {
            "target": target,
            "path": list(context_record["path"]),
            "cycle": cycle,
            "fitness": float(context_record["fitness"]),
            "gene_scores": gene_scores,
            "cycle_length": int(context_record.get("cycleLength", len(cycle))),
        }
    return details


def _serialize_generation(
    record: GenerationRecord,
    contexts: Contexts,
    n_genes: int,
    include_edge_to: bool,
) -> dict[str, Any]:
    payload: dict[str, Any] = {
        "generation": record["generation"],
        "genome": record["genome"],
        "fitness": record["fitness"],
        "accepted": record["accepted"],
        "changedBits": record["changed_bits"],
        "factorScores": list(record.get("factor_scores", [])),
        "contexts": _serialize_context_details(record["details"], contexts),
    }
    if include_edge_to:
        payload["edgeTo"] = transition_table_to_edge_to(
            record["transition_table"], n_genes
        )
    return payload


def build_evolution_export(
    history: list[GenerationRecord],
    contexts: Contexts,
    n_genes: int,
    mutation_probability: float,
    generations_requested: int,
    include_edge_to: bool = True,
) -> dict[str, Any]:
    """Build a JSON-serialisable dict from an evolution run."""
    if not history:
        raise ValueError("history must contain at least one generation")

    accepted_count = sum(1 for record in history if record["accepted"])
    rejected_count = len(history) - accepted_count
    accepted_metrics = extract_accepted_trajectory_metrics(history, contexts)
    factor_labels = factor_row_labels(contexts, n_genes)

    return {
        "schemaVersion": SCHEMA_VERSION,
        "nGenes": n_genes,
        "stateCount": 1 << n_genes,
        "genomeLength": len(history[0]["genome"]),
        "mutationProbability": mutation_probability,
        "generationsRequested": generations_requested,
        "generationsStored": len(history),
        "contexts": [
            {"initial": initial, "target": target}
            for initial, target in contexts.items()
        ],
        "factorLabels": factor_labels,
        "acceptedGenomes": [
            {
                "acceptedStep": step.step_index,
                "generation": step.generation,
                "genome": step.genome,
                "totalFitness": step.total_fitness,
                "contextFitness": list(step.context_fitness),
                "factorScores": list(step.factor_scores),
            }
            for step in accepted_metrics
        ],
        "summary": {
            "initialFitness": history[0]["fitness"],
            "finalFitness": history[-1]["fitness"],
            "acceptedCount": accepted_count,
            "rejectedCount": rejected_count,
            "acceptedGenomeCount": len(accepted_metrics),
        },
        "generations": [
            _serialize_generation(record, contexts, n_genes, include_edge_to)
            for record in history
        ],
    }


def export_evolution_json(
    history: list[GenerationRecord],
    contexts: Contexts,
    n_genes: int,
    output_path: str | Path,
    mutation_probability: float,
    generations_requested: int,
    include_edge_to: bool = True,
) -> Path:
    """Write evolution history to a JSON file."""
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)

    payload = build_evolution_export(
        history=history,
        contexts=contexts,
        n_genes=n_genes,
        mutation_probability=mutation_probability,
        generations_requested=generations_requested,
        include_edge_to=include_edge_to,
    )

    with output_path.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)

    return output_path
