"""Export 3D genome path embeddings (MDS / UMAP) for Unity visualisation."""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import numpy as np

from rbn.types import Contexts, Genome

SCHEMA_VERSION = 1


def serialize_contexts_for_unity(contexts: Contexts) -> list[dict[str, str]]:
    from visualisation.factorised.metrics import context_order

    return [
        {
            "label": f"Context {index}",
            "target": contexts[initial],
        }
        for index, initial in enumerate(context_order(contexts), start=1)
    ]


def build_unity_genome_embedding_payload(
    *,
    method: str,
    coordinates: np.ndarray,
    paths: list[tuple[str, list[Genome], list[float], list[str], float, list[list[float]]]],
    dimensions: int = 3,
    gene_count: int | None = None,
    context_count: int | None = None,
    contexts: Contexts | None = None,
) -> dict[str, Any]:
    if coordinates.ndim != 2:
        raise ValueError("coordinates must be a 2D array")
    if coordinates.shape[0] == 0:
        raise ValueError("coordinates must contain at least one genome")
    if coordinates.shape[1] < dimensions:
        raise ValueError(
            f"coordinates must have at least {dimensions} columns, "
            f"got {coordinates.shape[1]}"
        )

    unique_genomes: list[Genome] = []
    genome_index: dict[Genome, int] = {}
    for _, genomes, _, _, _, _ in paths:
        for genome in genomes:
            if genome in genome_index:
                continue
            genome_index[genome] = len(unique_genomes)
            unique_genomes.append(genome)

    run_entries: list[dict[str, Any]] = []
    for run_label, genomes, fitness_values, change_types, final_fitness, factor_scores in paths:
        if not (len(genomes) == len(fitness_values) == len(change_types) == len(factor_scores)):
            raise ValueError(
                f"run {run_label}: genome, fitness, change_type, and factor score "
                "trajectories must have equal length"
            )

        points: list[dict[str, Any]] = []
        for step, (genome, fitness, change_type, step_factor_scores) in enumerate(
            zip(genomes, fitness_values, change_types, factor_scores)
        ):
            index = genome_index[genome]
            coord = coordinates[index]
            points.append(
                {
                    "step": step,
                    "genomeId": f"g{index}",
                    "genomeIndex": index,
                    "fitness": float(fitness),
                    "x": float(coord[0]),
                    "y": float(coord[1]),
                    "z": float(coord[2]) if dimensions >= 3 else 0.0,
                    "changeType": change_type,
                    "factorScores": [float(score) for score in step_factor_scores],
                }
            )

        run_entries.append(
            {
                "sourceLabel": run_label,
                "finalFitness": float(final_fitness),
                "acceptedCount": len(points),
                "points": points,
            }
        )

    payload: dict[str, Any] = {
        "schemaVersion": SCHEMA_VERSION,
        "method": method,
        "metric": "hamming",
        "dimensions": dimensions,
        "runCount": len(run_entries),
        "genomeCount": len(unique_genomes),
        "runs": run_entries,
    }
    if gene_count is not None:
        payload["geneCount"] = int(gene_count)
    if context_count is not None:
        payload["contextCount"] = int(context_count)
    if contexts is not None:
        payload["contexts"] = serialize_contexts_for_unity(contexts)
    return payload


def export_unity_genome_embedding_json(
    payload: dict[str, Any],
    output_paths: str | Path | list[str | Path],
) -> list[Path]:
    if isinstance(output_paths, (str, Path)):
        paths = [Path(output_paths)]
    else:
        paths = [Path(path) for path in output_paths]

    written: list[Path] = []
    for path in paths:
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("w", encoding="utf-8") as handle:
            json.dump(payload, handle, indent=2)
        written.append(path)
    return written
