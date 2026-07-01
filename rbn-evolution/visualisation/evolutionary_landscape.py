"""Evolutionary landscape surface across ranked runs."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
    from mpl_toolkits.mplot3d import Axes3D
except ImportError:
    plt = None
    np = None

from rbn.types import EvolutionHistory, Genome, TransitionTable

PROGRESS_STEP = 5
PROGRESS_POINTS = tuple(range(0, 101, PROGRESS_STEP))


@dataclass(frozen=True)
class RankedRunTrajectory:
    label: str
    rank: int
    cumulative_changed_transitions: int
    genomes: tuple[Genome, ...]
    final_genome: Genome
    progress_values: tuple[int, ...]
    hamming_to_final: tuple[int, ...]


def _require_matplotlib():
    if plt is None or np is None:
        raise ImportError("matplotlib and numpy are required for evolutionary landscape plots")


def hamming_distance(left: Genome, right: Genome) -> int:
    return sum(bit_left != bit_right for bit_left, bit_right in zip(left, right))


def count_changed_transitions(
    previous: TransitionTable,
    current: TransitionTable,
) -> int:
    return sum(1 for state in previous if previous[state] != current[state])


def extract_accepted_trajectory(
    history: EvolutionHistory,
) -> tuple[list[Genome], list[TransitionTable], list[float]]:
    """Return accepted genomes, tables, and fitness in order, collapsing rejected steps."""
    genomes = [history[0]["genome"]]
    tables = [history[0]["transition_table"]]
    fitness_values = [history[0]["fitness"]]

    for record in history[1:]:
        if record["genome"] != genomes[-1]:
            genomes.append(record["genome"])
            tables.append(record["transition_table"])
            fitness_values.append(record["fitness"])

    if genomes[-1] != history[-1]["genome"]:
        genomes.append(history[-1]["genome"])
        tables.append(history[-1]["transition_table"])
        fitness_values.append(history[-1]["fitness"])

    return genomes, tables, fitness_values


def cumulative_changed_transitions_for_history(history: EvolutionHistory) -> int:
    _, tables, _ = extract_accepted_trajectory(history)
    total = 0
    for index in range(1, len(tables)):
        total += count_changed_transitions(tables[index - 1], tables[index])
    return total


def value_at_progress(values: list[float] | list[Genome] | list[TransitionTable], progress_percent: int):
    if len(values) == 1:
        return values[0]
    index = round((progress_percent / 100.0) * (len(values) - 1))
    return values[index]


def _build_run_trajectories(
    histories: list[EvolutionHistory],
    labels: list[str] | None = None,
) -> list[RankedRunTrajectory]:
    if labels is not None and len(labels) != len(histories):
        raise ValueError("labels must match the number of histories")

    trajectories: list[RankedRunTrajectory] = []
    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        genomes, _tables, _fitness_values = extract_accepted_trajectory(history)
        final_genome = genomes[-1]
        hamming_values = tuple(
            hamming_distance(value_at_progress(genomes, progress), final_genome)
            for progress in PROGRESS_POINTS
        )
        trajectories.append(
            RankedRunTrajectory(
                label=label,
                rank=0,
                cumulative_changed_transitions=cumulative_changed_transitions_for_history(
                    history
                ),
                genomes=tuple(genomes),
                final_genome=final_genome,
                progress_values=PROGRESS_POINTS,
                hamming_to_final=hamming_values,
            )
        )
    return trajectories


def build_ranked_run_trajectories(
    histories: list[EvolutionHistory],
    labels: list[str] | None = None,
) -> list[RankedRunTrajectory]:
    trajectories = _build_run_trajectories(histories, labels=labels)
    ordered = sorted(trajectories, key=lambda item: item.cumulative_changed_transitions)
    return [
        RankedRunTrajectory(
            label=trajectory.label,
            rank=rank,
            cumulative_changed_transitions=trajectory.cumulative_changed_transitions,
            genomes=trajectory.genomes,
            final_genome=trajectory.final_genome,
            progress_values=trajectory.progress_values,
            hamming_to_final=trajectory.hamming_to_final,
        )
        for rank, trajectory in enumerate(ordered)
    ]


def _plot_landscape_surface(
    ranked_runs: list[RankedRunTrajectory],
    save_path: Path,
    *,
    z_values: np.ndarray,
    z_label: str,
    title: str,
) -> Path:
    progress = np.array(PROGRESS_POINTS, dtype=float)
    ranks = np.arange(len(ranked_runs), dtype=float)
    progress_mesh, rank_mesh = np.meshgrid(progress, ranks)

    figure = plt.figure(figsize=(11, 8))
    axis = figure.add_subplot(111, projection="3d")
    surface = axis.plot_surface(
        progress_mesh,
        rank_mesh,
        z_values,
        cmap="viridis_r",
        linewidth=0.2,
        antialiased=True,
        alpha=0.95,
    )

    axis.set_xlabel("Evolutionary progress (accepted trajectory, %)")
    run_count = len(ranked_runs)
    axis.set_ylabel(
        f"{run_count} runs by cumulative\nchanged transitions (low to high)"
    )
    axis.set_zlabel(z_label)
    axis.set_title(title)
    axis.set_yticks([])

    colorbar = figure.colorbar(surface, ax=axis, shrink=0.65, pad=0.1)
    colorbar.set_label(z_label)

    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path


def plot_evolutionary_landscape(
    histories: list[EvolutionHistory],
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    """
    Plot a 3D evolutionary landscape across all runs.

    X = normalised accepted trajectory progress (0-100%)
    Y = runs ranked by cumulative changed transitions (low to high)
    Z = Hamming distance from current genome to that run's final genome
    """
    _require_matplotlib()

    if not histories:
        raise ValueError("histories must contain at least one run")

    ranked_runs = build_ranked_run_trajectories(histories, labels=labels)
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_hamming_landscape_csv(ranked_runs, Path(csv_path))

    z_values = np.array(
        [run.hamming_to_final for run in ranked_runs],
        dtype=float,
    )
    return _plot_landscape_surface(
        ranked_runs,
        save_path,
        z_values=z_values,
        z_label="Hamming distance to final genome",
        title="Evolutionary landscape",
    )


def _write_hamming_landscape_csv(ranked_runs: list[RankedRunTrajectory], csv_path: Path) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            [
                "label",
                "rank",
                "cumulative_changed_transitions",
                "progress_percent",
                "hamming_to_final",
                "accepted_genome_count",
            ]
        )
        for run in ranked_runs:
            for progress, hamming in zip(run.progress_values, run.hamming_to_final):
                writer.writerow(
                    [
                        run.label,
                        run.rank + 1,
                        run.cumulative_changed_transitions,
                        progress,
                        hamming,
                        len(run.genomes),
                    ]
                )
