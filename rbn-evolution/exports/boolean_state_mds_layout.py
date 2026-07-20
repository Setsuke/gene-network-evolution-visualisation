"""Export the fixed Boolean state-space MDS layout for Unity Option 5."""

from __future__ import annotations

import csv
import json
from pathlib import Path
from typing import Any

from exports.evolution_json import index_to_state
from visualisation.genome_mds_path import classical_mds

SCHEMA_VERSION = 1


def _pairwise_hamming_matrix(states: list[str]) -> np.ndarray:
    import numpy as np

    count = len(states)
    matrix = np.zeros((count, count), dtype=np.float64)
    for left_index in range(count):
        for right_index in range(left_index + 1, count):
            distance = float(
                sum(
                    left_bit != right_bit
                    for left_bit, right_bit in zip(states[left_index], states[right_index])
                )
            )
            matrix[left_index, right_index] = distance
            matrix[right_index, left_index] = distance
    return matrix


def all_boolean_states(n_genes: int) -> list[str]:
    state_count = 1 << n_genes
    return [index_to_state(index, n_genes) for index in range(state_count)]


def layout_as_state_coordinates(
    n_genes: int,
    *,
    n_components: int = 3,
    precision: int = 6,
) -> list[tuple[str, float, float, float]]:
    """
    Compute deterministic classical MDS coordinates for every Boolean state.

    This is the 5-bit Boolean state space (32 nodes), not the 160-bit genome space.
    """
    states = all_boolean_states(n_genes)
    distance_matrix = _pairwise_hamming_matrix(states)
    coordinates = classical_mds(distance_matrix, n_components=n_components)
    rows: list[tuple[str, float, float, float]] = []
    for state, coordinate in zip(states, coordinates):
        x, y, z = (float(value) for value in coordinate[:n_components])
        if n_components < 3:
            z = 0.0
        rows.append((state, round(x, precision), round(y, precision), round(z, precision)))
    return rows


def build_boolean_state_mds_layout_payload(
    n_genes: int,
    *,
    n_components: int = 3,
) -> dict[str, Any]:
    rows = layout_as_state_coordinates(n_genes, n_components=n_components)
    nodes: list[dict[str, Any]] = []
    for index, (state, x, y, z) in enumerate(rows):
        nodes.append(
            {
                "index": index,
                "state": state,
                "label": state,
                "x": x,
                "y": y,
                "z": z,
            }
        )

    return {
        "schemaVersion": SCHEMA_VERSION,
        "space": "boolean_state",
        "description": (
            "Deterministic classical MDS embedding of the Boolean state space. "
            "This layout is fixed across all genomes and runs."
        ),
        "geneCount": n_genes,
        "stateCount": len(nodes),
        "metric": "hamming",
        "method": "classical_mds",
        "dimensions": n_components,
        "nodes": nodes,
    }


def export_boolean_state_mds_layout_csv(
    output_path: str | Path,
    n_genes: int,
    *,
    n_components: int = 3,
) -> Path:
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    rows = layout_as_state_coordinates(n_genes, n_components=n_components)
    with output_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(["state", "x", "y", "z"])
        for state, x, y, z in rows:
            writer.writerow([state, x, y, z])
    return output_path


def export_boolean_state_mds_layout_json(
    output_paths: str | Path | list[str | Path],
    n_genes: int,
    *,
    n_components: int = 3,
) -> list[Path]:
    payload = build_boolean_state_mds_layout_payload(
        n_genes,
        n_components=n_components,
    )
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
