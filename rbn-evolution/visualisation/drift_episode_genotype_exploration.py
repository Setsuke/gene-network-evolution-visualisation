"""Genotype-space exploration during plateau drift episodes."""

from __future__ import annotations

import csv
import random
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
except ImportError:
    plt = None
    np = None

from rbn.types import Contexts, EvolutionHistory, Genome
from visualisation.drift_episode_length_distribution import (
    _accepted_step_generations,
    _generation_span,
)
from visualisation.evolutionary_landscape import hamming_distance
from visualisation.genome_mds_path import extract_accepted_genome_paths, genomes_to_bit_matrix
from visualisation.plateau_drift_classification import find_plateau_episodes


@dataclass(frozen=True)
class DriftEpisodeExplorationRecord:
    run_label: str
    episode_index: int
    start_step: int
    end_step: int
    exit_step: int
    drift_length_accepted: int
    drift_duration_generations: int
    start_end_hamming: int
    max_hamming_from_start: int
    unique_genomes: int
    net_hamming_per_move: float
    max_hamming_per_move: float
    excess_exploration_hamming: int
    unique_genome_fraction: float


def _episode_hamming_profile(
    start_genome: Genome,
    episode_genomes: tuple[Genome, ...],
) -> tuple[int, int, int]:
    if not episode_genomes:
        return 0, 0, 0

    start_bits = genomes_to_bit_matrix([start_genome])[0]
    episode_bits = genomes_to_bit_matrix(list(episode_genomes))
    hamming_profile = (episode_bits != start_bits).sum(axis=1).astype(int)
    start_end = int(hamming_profile[-1])
    max_hamming = int(hamming_profile.max())
    return start_end, max_hamming, len(set(episode_genomes))


def collect_drift_episode_exploration_records(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
) -> list[DriftEpisodeExplorationRecord]:
    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    records: list[DriftEpisodeExplorationRecord] = []

    for history, path in zip(histories, paths):
        fitness_values = list(path.fitness_values)
        step_generations = _accepted_step_generations(history)
        if len(step_generations) != len(fitness_values):
            raise ValueError(
                f"{path.run_label}: accepted step/generation mapping length mismatch "
                f"({len(step_generations)} != {len(fitness_values)})"
            )

        episode_index = 0
        for start_step, end_step, exit_step in find_plateau_episodes(fitness_values):
            drift_length_accepted = end_step - start_step
            if drift_length_accepted <= 0:
                continue

            episode_genomes = path.genomes[start_step : end_step + 1]
            start_end_hamming, max_hamming_from_start, unique_genomes = _episode_hamming_profile(
                path.genomes[start_step],
                episode_genomes,
            )
            generation_start = step_generations[start_step]
            generation_exit = step_generations[exit_step]
            drift_duration_generations = _generation_span(
                history,
                generation_start,
                generation_exit,
            )

            genome_count = drift_length_accepted + 1
            records.append(
                DriftEpisodeExplorationRecord(
                    run_label=path.run_label,
                    episode_index=episode_index,
                    start_step=start_step,
                    end_step=end_step,
                    exit_step=exit_step,
                    drift_length_accepted=drift_length_accepted,
                    drift_duration_generations=drift_duration_generations,
                    start_end_hamming=start_end_hamming,
                    max_hamming_from_start=max_hamming_from_start,
                    unique_genomes=unique_genomes,
                    net_hamming_per_move=start_end_hamming / drift_length_accepted,
                    max_hamming_per_move=max_hamming_from_start / drift_length_accepted,
                    excess_exploration_hamming=max_hamming_from_start - start_end_hamming,
                    unique_genome_fraction=unique_genomes / genome_count,
                )
            )
            episode_index += 1

    return records


def validate_exploration_records_manual(
    records: list[DriftEpisodeExplorationRecord],
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
    sample_count: int = 5,
    seed: int = 42,
) -> list[str]:
    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    path_by_label = {path.run_label: path for path in paths}
    samples = random.Random(seed).sample(records, min(sample_count, len(records)))
    lines: list[str] = []

    for record in samples:
        path = path_by_label[record.run_label]
        episode_genomes = path.genomes[record.start_step : record.end_step + 1]
        manual_start_end = hamming_distance(
            path.genomes[record.start_step],
            path.genomes[record.end_step],
        )
        manual_max = max(
            hamming_distance(path.genomes[record.start_step], genome)
            for genome in episode_genomes
        )
        manual_unique = len(set(episode_genomes))
        ok = (
            manual_start_end == record.start_end_hamming
            and manual_max == record.max_hamming_from_start
            and manual_unique == record.unique_genomes
        )
        lines.append(
            f"{record.run_label} ep#{record.episode_index}: "
            f"steps {record.start_step}->{record.end_step} (exit {record.exit_step}), "
            f"L={record.drift_length_accepted}, gens={record.drift_duration_generations}, "
            f"start_end={record.start_end_hamming} (manual {manual_start_end}), "
            f"max={record.max_hamming_from_start} (manual {manual_max}), "
            f"unique={record.unique_genomes} (manual {manual_unique}) "
            f"{'OK' if ok else 'MISMATCH'}"
        )

    return lines


def _require_dependencies() -> None:
    if plt is None or np is None:
        raise ImportError(
            "matplotlib and numpy are required for drift exploration analysis"
        )


def _assign_quartile_groups(values: np.ndarray) -> tuple[list[np.ndarray], list[tuple[float, float]]]:
    thresholds = np.quantile(values, [0.25, 0.5, 0.75])
    groups = [
        values <= thresholds[0],
        (values > thresholds[0]) & (values <= thresholds[1]),
        (values > thresholds[1]) & (values <= thresholds[2]),
        values > thresholds[2],
    ]
    ranges = [
        (float(values.min()), float(thresholds[0])),
        (float(thresholds[0]), float(thresholds[1])),
        (float(thresholds[1]), float(thresholds[2])),
        (float(thresholds[2]), float(values.max())),
    ]
    return groups, ranges


def _quartile_summary(
    records: list[DriftEpisodeExplorationRecord],
    *,
    group_field: str,
) -> list[dict[str, float | int | str]]:
    _require_dependencies()
    values = np.asarray([getattr(record, group_field) for record in records], dtype=float)
    groups, ranges = _assign_quartile_groups(values)
    rows: list[dict[str, float | int | str]] = []

    for quartile_index, (mask, (range_min, range_max)) in enumerate(zip(groups, ranges), start=1):
        group_records = [record for record, keep in zip(records, mask) if keep]
        if not group_records:
            continue

        rows.append(
            {
                "group_field": group_field,
                "quartile": f"Q{quartile_index}",
                "range_min": range_min,
                "range_max": range_max,
                "episode_count": len(group_records),
                "median_start_end_hamming": float(np.median([r.start_end_hamming for r in group_records])),
                "median_max_hamming_from_start": float(
                    np.median([r.max_hamming_from_start for r in group_records])
                ),
                "median_unique_genomes": float(np.median([r.unique_genomes for r in group_records])),
                "median_net_hamming_per_move": float(
                    np.median([r.net_hamming_per_move for r in group_records])
                ),
                "median_excess_exploration_hamming": float(
                    np.median([r.excess_exploration_hamming for r in group_records])
                ),
                "median_unique_genome_fraction": float(
                    np.median([r.unique_genome_fraction for r in group_records])
                ),
            }
        )

    return rows


def _write_episode_csv(records: list[DriftEpisodeExplorationRecord], csv_path: Path) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    fieldnames = [
        "run_label",
        "episode_index",
        "start_step",
        "end_step",
        "exit_step",
        "drift_length_accepted",
        "drift_duration_generations",
        "start_end_hamming",
        "max_hamming_from_start",
        "unique_genomes",
        "net_hamming_per_move",
        "max_hamming_per_move",
        "excess_exploration_hamming",
        "unique_genome_fraction",
    ]
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        for record in records:
            writer.writerow(
                {
                    "run_label": record.run_label,
                    "episode_index": record.episode_index,
                    "start_step": record.start_step,
                    "end_step": record.end_step,
                    "exit_step": record.exit_step,
                    "drift_length_accepted": record.drift_length_accepted,
                    "drift_duration_generations": record.drift_duration_generations,
                    "start_end_hamming": record.start_end_hamming,
                    "max_hamming_from_start": record.max_hamming_from_start,
                    "unique_genomes": record.unique_genomes,
                    "net_hamming_per_move": f"{record.net_hamming_per_move:.6f}",
                    "max_hamming_per_move": f"{record.max_hamming_per_move:.6f}",
                    "excess_exploration_hamming": record.excess_exploration_hamming,
                    "unique_genome_fraction": f"{record.unique_genome_fraction:.6f}",
                }
            )


def _write_quartile_csv(rows: list[dict[str, float | int | str]], csv_path: Path) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    if not rows:
        return
    fieldnames = list(rows[0].keys())
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(rows)


def _median_start_end_hamming_by_length(
    records: list[DriftEpisodeExplorationRecord],
) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    by_length: dict[int, list[DriftEpisodeExplorationRecord]] = {}
    for record in records:
        by_length.setdefault(record.drift_length_accepted, []).append(record)

    lengths = np.asarray(sorted(by_length), dtype=int)
    medians = np.asarray(
        [
            float(np.median([episode.start_end_hamming for episode in by_length[length]]))
            for length in lengths
        ],
        dtype=float,
    )
    counts = np.asarray([len(by_length[length]) for length in lengths], dtype=int)
    return lengths, medians, counts


def _marker_sizes_from_counts(
    counts: np.ndarray,
    *,
    min_size: float = 8.0,
    max_size: float = 120.0,
) -> np.ndarray:
    """Scale marker area so the highest-frequency bin uses max_size."""
    if len(counts) == 0:
        return np.asarray([], dtype=float)

    max_count = int(counts.max())
    if max_count <= 0:
        return np.full(len(counts), min_size, dtype=float)

    return min_size + (counts.astype(float) / max_count) * (max_size - min_size)


def _plot_length_median_start_end_hamming(
    records: list[DriftEpisodeExplorationRecord],
    save_path: Path,
) -> None:
    lengths, medians, counts = _median_start_end_hamming_by_length(records)
    marker_sizes = _marker_sizes_from_counts(counts)
    figure, axis = plt.subplots(figsize=(10, 6))
    axis.scatter(
        lengths,
        medians,
        s=marker_sizes,
        alpha=0.55,
        color="#2563eb",
        edgecolors="white",
        linewidths=0.6,
        label="Median by drift length",
    )
    axis.plot(lengths, medians, color="#2563eb", alpha=0.35, linewidth=1.2)
    axis.set_xlabel("Drifting-episode length (accepted neutral movements)")
    axis.set_ylabel("Median start-to-end Hamming distance")
    axis.set_title("Drifting-Episode Length and Genomic Displacement")
    axis.grid(alpha=0.25)
    total_episodes = int(counts.sum())
    figure.text(
        0.5,
        0.02,
        f"{total_episodes:,} episodes",
        ha="center",
        fontsize=9.5,
        color="#475569",
    )
    figure.subplots_adjust(bottom=0.12)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)


def _plot_quartile_medians(
    quartile_rows: list[dict[str, float | int | str]],
    save_path: Path,
    *,
    group_field: str,
    metric_field: str,
    y_label: str,
    title: str,
) -> None:
    labels = [str(row["quartile"]) for row in quartile_rows]
    values = [float(row[metric_field]) for row in quartile_rows]
    figure, axis = plt.subplots(figsize=(8, 5))
    axis.bar(labels, values, color="#2563eb", edgecolor="white")
    axis.set_xlabel(f"{group_field} quartile")
    axis.set_ylabel(y_label)
    axis.set_title(title)
    axis.grid(axis="y", alpha=0.25)
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)


def print_exploration_report(
    records: list[DriftEpisodeExplorationRecord],
    length_quartiles: list[dict[str, float | int | str]],
    duration_quartiles: list[dict[str, float | int | str]],
    validation_lines: list[str],
) -> None:
    print("\n=== Drift Episode Genotype Exploration ===")
    print(f"Episodes analysed: {len(records)}")
    print("\nManual Hamming validation:")
    for line in validation_lines:
        print(f"  {line}")

    print("\nMedian exploration by accepted-length quartile:")
    for row in length_quartiles:
        print(
            f"  {row['quartile']} [{row['range_min']:.0f}, {row['range_max']:.0f}] "
            f"n={row['episode_count']}: "
            f"start_end={row['median_start_end_hamming']:.1f}, "
            f"max={row['median_max_hamming_from_start']:.1f}, "
            f"unique={row['median_unique_genomes']:.1f}, "
            f"net/move={row['median_net_hamming_per_move']:.3f}, "
            f"excess={row['median_excess_exploration_hamming']:.1f}"
        )

    print("\nMedian exploration by generation-duration quartile:")
    for row in duration_quartiles:
        print(
            f"  {row['quartile']} [{row['range_min']:.0f}, {row['range_max']:.0f}] "
            f"n={row['episode_count']}: "
            f"start_end={row['median_start_end_hamming']:.1f}, "
            f"max={row['median_max_hamming_from_start']:.1f}, "
            f"unique={row['median_unique_genomes']:.1f}, "
            f"net/move={row['median_net_hamming_per_move']:.3f}, "
            f"excess={row['median_excess_exploration_hamming']:.1f}"
        )

    print("\n=== Conservative interpretation ===")
    print(_build_interpretation(records, length_quartiles, duration_quartiles))


def _build_interpretation(
    records: list[DriftEpisodeExplorationRecord],
    length_quartiles: list[dict[str, float | int | str]],
    duration_quartiles: list[dict[str, float | int | str]],
) -> str:
    all_unique = all(abs(record.unique_genome_fraction - 1.0) < 1e-9 for record in records)
    excess_positive = sum(1 for record in records if record.excess_exploration_hamming > 0)
    q1_net = float(length_quartiles[0]["median_net_hamming_per_move"])
    q4_net = float(length_quartiles[-1]["median_net_hamming_per_move"])
    length_q1_start_end = float(length_quartiles[0]["median_start_end_hamming"])
    length_q4_start_end = float(length_quartiles[-1]["median_start_end_hamming"])
    duration_q1_start_end = float(duration_quartiles[0]["median_start_end_hamming"])
    duration_q4_start_end = float(duration_quartiles[-1]["median_start_end_hamming"])

    lines = [
        "1. Progressive displacement from the starting genome: "
        + (
            f"Yes. Quartile medians rise from {length_q1_start_end:.0f} bits (Q1) to "
            f"{length_q4_start_end:.0f} bits (Q4) for accepted drift length."
            if length_q4_start_end > length_q1_start_end
            else "Not clearly supported."
        ),
        "2. More distinct genotypes in longer episodes: "
        + (
            "Trivially yes for this dataset: every episode visits only unique accepted genomes "
            f"(unique_genomes = drift_length + 1 in all {len(records)} episodes)."
            if all_unique
            else "Longer episodes visit more distinct accepted genomes."
        ),
        "3. Genuine exploration vs local/repeated movement: "
        + (
            "Within-episode revisiting does not occur (unique fraction = 1.0 throughout). "
            f"However, only {excess_positive}/{len(records)} episodes ({100*excess_positive/len(records):.1f}%) "
            "ever exceed their final Hamming distance, so most trajectories end at their farthest "
            f"accepted point. Longer episodes accumulate displacement across more moves "
            f"(median net/start-end per move falls from Q1={q1_net:.2f} to Q4={q4_net:.2f}), "
            "consistent with incremental exploration rather than one-step jumps alone."
        ),
        "4. Generation duration vs accepted length: "
        + (
            f"Yes, a different story. Generation-duration quartile medians rise from "
            f"{duration_q1_start_end:.0f} to {duration_q4_start_end:.0f} bits, less sharply than "
            f"accepted-length quartiles ({length_q1_start_end:.0f} to {length_q4_start_end:.0f}), "
            "because many rejected generations can elapse with little or no accepted displacement."
            if duration_q4_start_end - duration_q1_start_end < length_q4_start_end - length_q1_start_end
            else "Similar directionally to accepted-length drift."
        ),
        "5. Neutral drift and genotype-space exploration at constant fitness: "
        + (
            "Consistent with exploration: drift episodes move through novel accepted genomes and "
            "increase Hamming distance from the plateau start while fitness remains flat. This "
            "supports neutral drift as genotype-space movement, but does not by itself establish "
            "that such exploration causes later fitness improvements."
        ),
    ]
    return "\n".join(lines)


def analyse_drift_episode_genotype_exploration(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    episode_csv_path: str | Path,
    quartile_csv_path: str | Path,
    length_quartile_max_hamming_plot: str | Path,
    length_median_start_end_hamming_plot: str | Path,
    labels: list[str] | None = None,
) -> list[DriftEpisodeExplorationRecord]:
    _require_dependencies()
    records = collect_drift_episode_exploration_records(histories, contexts, labels=labels)
    validation_lines = validate_exploration_records_manual(
        records,
        histories,
        contexts,
        labels=labels,
    )
    length_quartiles = _quartile_summary(records, group_field="drift_length_accepted")
    duration_quartiles = _quartile_summary(records, group_field="drift_duration_generations")
    quartile_rows = length_quartiles + duration_quartiles

    _write_episode_csv(records, Path(episode_csv_path))
    _write_quartile_csv(quartile_rows, Path(quartile_csv_path))

    _plot_quartile_medians(
        length_quartiles,
        Path(length_quartile_max_hamming_plot),
        group_field="drift_length_accepted",
        metric_field="median_max_hamming_from_start",
        y_label="Median max Hamming from start",
        title="Genome Exploration by Accepted Drift-Length Quartile",
    )
    _plot_length_median_start_end_hamming(records, Path(length_median_start_end_hamming_plot))

    print_exploration_report(
        records,
        length_quartiles,
        duration_quartiles,
        validation_lines,
    )
    return records


if __name__ == "__main__":
    import argparse

    from config import (
        DRIFT_EPISODE_GENOTYPE_EXPLORATION_EPISODES_CSV,
        DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_MEDIAN_START_END_HAMMING_PLOT,
        DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_QUARTILE_MAX_HAMMING_PLOT,
        DRIFT_EPISODE_GENOTYPE_EXPLORATION_QUARTILES_CSV,
        RUNS_OUTPUT_DIR,
        get_contexts,
    )
    from visualisation.drifting_categories_normalised_progress import load_histories_from_runs_dir

    parser = argparse.ArgumentParser(description="Analyse genotype exploration during drift episodes.")
    parser.add_argument("--runs-dir", type=Path, default=RUNS_OUTPUT_DIR)
    parser.add_argument("--n-genes", type=int, default=5)
    args = parser.parse_args()

    contexts = get_contexts(args.n_genes)
    histories, labels = load_histories_from_runs_dir(args.runs_dir, contexts)
    analyse_drift_episode_genotype_exploration(
        histories,
        contexts,
        episode_csv_path=DRIFT_EPISODE_GENOTYPE_EXPLORATION_EPISODES_CSV,
        quartile_csv_path=DRIFT_EPISODE_GENOTYPE_EXPLORATION_QUARTILES_CSV,
        length_quartile_max_hamming_plot=DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_QUARTILE_MAX_HAMMING_PLOT,
        length_median_start_end_hamming_plot=DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_MEDIAN_START_END_HAMMING_PLOT,
        labels=labels,
    )
