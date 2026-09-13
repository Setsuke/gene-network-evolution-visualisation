"""Drift episode length helpers and histogram plotting."""

from __future__ import annotations

from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
except ImportError:
    plt = None
    np = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.genome_mds_path import extract_accepted_genome_paths
from visualisation.plateau_drift_classification import find_plateau_episodes

def _accepted_step_generations(history: EvolutionHistory) -> list[int]:
    """Map each accepted-path step index to its evolutionary generation number."""
    step_generations: list[int] = []
    last_genome = None

    for record in history:
        genome = record["genome"]
        if last_genome is None or genome != last_genome:
            step_generations.append(int(record["generation"]))
            last_genome = genome
            if record["fitness"] == 1.0:
                break

    if history[-1]["genome"] != last_genome and (
        not step_generations
        or step_generations[-1] != int(history[-1]["generation"])
    ):
        step_generations.append(int(history[-1]["generation"]))

    return step_generations


def _generation_span(history: EvolutionHistory, start_generation: int, exit_generation: int) -> int:
    """Count all evolutionary generations in [start, exit], including rejected candidates."""
    return sum(
        1
        for record in history
        if start_generation <= int(record["generation"]) <= exit_generation
    )


def _require_plot_dependencies() -> None:
    if plt is None or np is None:
        raise ImportError(
            "matplotlib and numpy are required for drift episode length histograms"
        )


def collect_drift_episode_lengths(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
) -> list[int]:
    """Collect accepted neutral move counts per plateau drift episode."""
    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    lengths: list[int] = []

    for path in paths:
        fitness_values = list(path.fitness_values)
        for start_step, end_step, _exit_step in find_plateau_episodes(fitness_values):
            drift_length = end_step - start_step
            if drift_length > 0:
                lengths.append(drift_length)

    return lengths


def plot_drift_episode_length_histogram(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    save_path: str | Path,
    labels: list[str] | None = None,
) -> list[int]:
    """Plot the distribution of accepted neutral move lengths across drift episodes."""
    _require_plot_dependencies()
    lengths = collect_drift_episode_lengths(histories, contexts, labels=labels)
    if not lengths:
        raise ValueError("No drift episodes found for length histogram")

    values = np.asarray(lengths, dtype=int)
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    run_count = len(histories)

    figure, axis = plt.subplots(figsize=(10, 6))
    minimum = int(values.min())
    maximum = int(values.max())
    bins = np.arange(minimum, maximum + 2) - 0.5
    axis.hist(values, bins=bins, color="#2563eb", edgecolor="white", linewidth=0.8)
    axis.set_xlabel("Drifting-episode length (accepted neutral movements)")
    axis.set_ylabel("Frequency")
    axis.set_title("Distribution of Drifting-Episode Lengths")
    axis.grid(axis="y", alpha=0.25)
    figure.text(
        0.5,
        0.02,
        f"{values.size:,} episodes from {run_count:,} evolutionary trajectories",
        ha="center",
        fontsize=9.5,
        color="#475569",
    )
    figure.subplots_adjust(bottom=0.12)
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)

    median = float(np.median(values))
    mean = float(np.mean(values))
    print(
        f"\nDrift episode length histogram: {values.size:,} episodes from "
        f"{run_count:,} evolutionary trajectories, "
        f"median={median:.1f}, mean={mean:.2f}, max={maximum}"
    )
    return lengths
