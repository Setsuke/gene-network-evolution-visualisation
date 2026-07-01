"""Export accepted fitness trajectories for the Unity visualisation."""

from __future__ import annotations

import json
import math
from pathlib import Path
from typing import Any

from rbn.types import EvolutionHistory, Genome

SCHEMA_VERSION = 1


def hamming_distance(left: Genome, right: Genome) -> int:
    return sum(bit_left != bit_right for bit_left, bit_right in zip(left, right))


def extract_accepted_fitness_trajectory(history: EvolutionHistory) -> list[float]:
    """Return accepted fitness values in order, stopping at perfect fitness."""
    fitness_values: list[float] = []
    last_genome: Genome | None = None

    for record in history:
        genome = record["genome"]
        if last_genome is None or genome != last_genome:
            fitness_values.append(float(record["fitness"]))
            last_genome = genome
            if record["fitness"] == 1.0:
                break

    if history[-1]["genome"] != last_genome and (
        not fitness_values or fitness_values[-1] != float(history[-1]["fitness"])
    ):
        fitness_values.append(float(history[-1]["fitness"]))

    return fitness_values


def _extract_genomes(history: EvolutionHistory) -> list[Genome]:
    genomes: list[Genome] = []
    last_genome: Genome | None = None
    for record in history:
        genome = record["genome"]
        if last_genome is None or genome != last_genome:
            genomes.append(genome)
            last_genome = genome
            if record["fitness"] == 1.0:
                break
    if history[-1]["genome"] != last_genome:
        genomes.append(history[-1]["genome"])
    return genomes


def build_unity_trajectory_payload(
    histories: list[EvolutionHistory],
    *,
    labels: list[str] | None = None,
    disk_radius: float = 10.0,
) -> dict[str, Any]:
    if not histories:
        raise ValueError("histories must contain at least one run")

    run_entries: list[dict[str, Any]] = []
    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        genomes = _extract_genomes(history)
        fitness_values = extract_accepted_fitness_trajectory(history)
        if not genomes or not fitness_values:
            continue

        final_genome = genomes[-1]
        initial_genome = genomes[0]
        run_entries.append(
            {
                "sourceLabel": label,
                "initialHamming": hamming_distance(initial_genome, final_genome),
                "acceptedCount": len(fitness_values),
                "finalFitness": fitness_values[-1],
                "points": [{"fitness": value} for value in fitness_values],
            }
        )

    run_entries.sort(key=lambda item: item["initialHamming"])

    max_accepted = max(item["acceptedCount"] for item in run_entries)
    run_count = len(run_entries)
    for run_index, entry in enumerate(run_entries, start=1):
        angle = 0.0 if run_count == 1 else ((run_index - 1) / run_count) * (2.0 * math.pi)
        if max_accepted <= 1:
            start_radius = disk_radius
        else:
            normalised = (entry["acceptedCount"] - 1) / (max_accepted - 1)
            start_radius = disk_radius * max(0.08, normalised)
        entry["runIndex"] = run_index
        entry["label"] = str(run_index)
        entry["angleRadians"] = angle
        entry["startRadius"] = start_radius

    return {
        "schemaVersion": SCHEMA_VERSION,
        "runCount": run_count,
        "diskRadius": disk_radius,
        "maxAcceptedCount": max_accepted,
        "maxInitialHamming": max(item["initialHamming"] for item in run_entries),
        "runs": run_entries,
    }


def export_unity_trajectories_json(
    histories: list[EvolutionHistory],
    output_path: str | Path,
    *,
    labels: list[str] | None = None,
    disk_radius: float = 10.0,
) -> Path:
    output_path = Path(output_path)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    payload = build_unity_trajectory_payload(
        histories,
        labels=labels,
        disk_radius=disk_radius,
    )
    with output_path.open("w", encoding="utf-8") as handle:
        json.dump(payload, handle, indent=2)
    return output_path


def load_evolution_json(path: Path) -> EvolutionHistory:
    with path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)

    history: EvolutionHistory = []
    for record in payload["generations"]:
        history.append(
            {
                "generation": record["generation"],
                "genome": record["genome"],
                "fitness": record["fitness"],
                "candidate_fitness": record["candidateFitness"],
                "accepted": record["accepted"],
                "changed_bits": record["changedBits"],
                "transition_table": {},
                "details": {},
            }
        )
    return history


def export_unity_trajectories_from_outputs(
    outputs_dir: str | Path,
    destination_path: str | Path,
    *,
    disk_radius: float = 10.0,
) -> Path:
    outputs_dir = Path(outputs_dir)
    runs_dir = outputs_dir / "runs"
    evolution_files = sorted(runs_dir.glob("run_*/evolution.json"))
    if not evolution_files:
        evolution_files = sorted(outputs_dir.glob("run_*/evolution.json"))
    if not evolution_files and (outputs_dir / "evolution.json").exists():
        evolution_files = [outputs_dir / "evolution.json"]

    histories = [load_evolution_json(path) for path in evolution_files]
    labels = [
        path.parent.name if path.parent.name.startswith("run_") else path.stem
        for path in evolution_files
    ]
    return export_unity_trajectories_json(
        histories,
        destination_path,
        labels=labels,
        disk_radius=disk_radius,
    )
