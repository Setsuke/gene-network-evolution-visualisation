"""Mean attractor counts (point attractors, limit cycles) by normalised evolutionary progression."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
except ImportError:
    plt = None
    np = None

from rbn.attractors import find_attractors
from rbn.types import Contexts, EvolutionHistory, GenerationRecord, Genome
from visualisation.evolution_progress_breakdown import (
    PROGRESS_BIN_COUNT,
    PROGRESS_BIN_LABELS,
    progress_bin_index,
)


@dataclass(frozen=True)
class ProgressAttractorSample:
    run_label: str
    step_index: int
    progress_percent: float
    bin_index: int
    point_attractor_count: int
    limit_cycle_count: int


def _require_matplotlib() -> None:
    if plt is None or np is None:
        raise ImportError("matplotlib and numpy are required for progress attractor plots")


def normalized_trajectory_progress(step_index: int, total_steps: int) -> float:
    if total_steps <= 1:
        return 100.0
    return 100.0 * step_index / (total_steps - 1)


def _extract_accepted_records(history: EvolutionHistory) -> list[GenerationRecord]:
    if not history:
        return []

    records: list[GenerationRecord] = []
    last_genome: Genome | None = None

    for record in history:
        genome = record["genome"]
        if last_genome is None or genome != last_genome:
            records.append(record)
            last_genome = genome
            if record["fitness"] == 1.0:
                break

    if history[-1]["genome"] != last_genome and (
        not records or float(records[-1]["fitness"]) != float(history[-1]["fitness"])
    ):
        records.append(history[-1])

    return records


def collect_progress_attractor_samples(
    histories: list[EvolutionHistory],
    *,
    labels: list[str] | None = None,
) -> list[ProgressAttractorSample]:
    samples: list[ProgressAttractorSample] = []

    for run_index, history in enumerate(histories):
        run_label = labels[run_index] if labels is not None else f"run_{run_index + 1:03d}"
        accepted_records = _extract_accepted_records(history)
        total_steps = len(accepted_records)

        for step_index, record in enumerate(accepted_records):
            point_attractors, limit_cycles = find_attractors(record["transition_table"])
            progress_percent = normalized_trajectory_progress(step_index, total_steps)
            samples.append(
                ProgressAttractorSample(
                    run_label=run_label,
                    step_index=step_index,
                    progress_percent=progress_percent,
                    bin_index=progress_bin_index(progress_percent),
                    point_attractor_count=len(point_attractors),
                    limit_cycle_count=len(limit_cycles),
                )
            )

    return samples


def build_progress_attractor_bin_means(
    samples: list[ProgressAttractorSample],
) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    _require_matplotlib()
    point_sums = np.zeros(PROGRESS_BIN_COUNT, dtype=float)
    cycle_sums = np.zeros(PROGRESS_BIN_COUNT, dtype=float)
    counts = np.zeros(PROGRESS_BIN_COUNT, dtype=int)

    for sample in samples:
        point_sums[sample.bin_index] += sample.point_attractor_count
        cycle_sums[sample.bin_index] += sample.limit_cycle_count
        counts[sample.bin_index] += 1

    point_means = np.zeros(PROGRESS_BIN_COUNT, dtype=float)
    cycle_means = np.zeros(PROGRESS_BIN_COUNT, dtype=float)
    for bin_index in range(PROGRESS_BIN_COUNT):
        if counts[bin_index] > 0:
            point_means[bin_index] = point_sums[bin_index] / counts[bin_index]
            cycle_means[bin_index] = cycle_sums[bin_index] / counts[bin_index]

    return point_means, cycle_means, counts


def _write_progress_attractor_csv(
    point_means: np.ndarray,
    cycle_means: np.ndarray,
    counts: np.ndarray,
    csv_path: Path,
    *,
    run_count: int,
    total_samples: int,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            [
                "progress_bin",
                "mean_point_attractors",
                "mean_limit_cycles",
                "sample_count",
                "runs",
                "total_samples",
            ]
        )
        for bin_index, bin_label in enumerate(PROGRESS_BIN_LABELS):
            writer.writerow(
                [
                    bin_label,
                    f"{point_means[bin_index]:.6f}",
                    f"{cycle_means[bin_index]:.6f}",
                    int(counts[bin_index]),
                    run_count,
                    total_samples,
                ]
            )


def plot_progress_attractor_counts(
    histories: list[EvolutionHistory],
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    """
    Line chart of mean point-attractor and limit-cycle counts by normalised evolutionary progression.

    Each accepted genome along a trajectory contributes one sample. Progress is normalised
    from the initial accepted genome (0%) to the final accepted genome (100%).
    """
    _require_matplotlib()
    if not histories:
        raise ValueError("histories must contain at least one run")

    samples = collect_progress_attractor_samples(histories, labels=labels)
    if not samples:
        raise ValueError("no accepted trajectory samples found")

    point_means, cycle_means, bin_counts = build_progress_attractor_bin_means(samples)
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_progress_attractor_csv(
            point_means,
            cycle_means,
            bin_counts,
            Path(csv_path),
            run_count=len(histories),
            total_samples=len(samples),
        )

    x_positions = np.arange(PROGRESS_BIN_COUNT)
    figure, axis = plt.subplots(figsize=(12, 7))
    axis.plot(
        x_positions,
        point_means,
        marker="o",
        linewidth=2.0,
        markersize=7,
        color="#0284c7",
        label="Point attractors",
    )
    axis.plot(
        x_positions,
        cycle_means,
        marker="s",
        linewidth=2.0,
        markersize=7,
        color="#dc2626",
        label="Limit cycles",
    )

    axis.set_xticks(x_positions)
    axis.set_xticklabels(PROGRESS_BIN_LABELS, fontsize=10)
    axis.set_xlabel("Normalised evolutionary progression")
    axis.set_ylabel("Mean number of attractors")
    axis.set_title("Attractor Structure Across Normalised Evolutionary Progression")
    axis.grid(axis="y", alpha=0.25)
    axis.legend(loc="upper right", frameon=False)

    footer_text = (
        f"{len(histories):,} evolutionary trajectories  |  {len(samples):,} accepted genomes  |  "
        "attractors counted from each accepted network's transition table"
    )
    figure.text(
        0.5,
        0.03,
        footer_text,
        ha="center",
        va="bottom",
        fontsize=9.5,
        color="#475569",
    )

    figure.subplots_adjust(bottom=0.16, top=0.92)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.45)
    plt.close(figure)
    return save_path
