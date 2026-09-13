"""Bar chart of accepted-genome counts pooled by evolution-progress quartiles."""

from __future__ import annotations

import argparse
import csv
import json
import math
from dataclasses import dataclass
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np

from config import (
    ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV,
    ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT,
    DEFAULT_N_GENES,
    OUTPUT_DIR,
    RUNS_OUTPUT_DIR,
    SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV,
    SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT,
    UNITY_SHARED_GENOME_MDS_JSON,
    get_contexts,
)
from rbn.types import EvolutionHistory, Genome
from visualisation.accepted_move_taxonomy import iter_accepted_change_records
from visualisation.drifting_categories_normalised_progress import load_histories_from_runs_dir
from visualisation.evolution_progress_breakdown import normalized_run_progress
from visualisation.genome_mds_path import _pairwise_hamming_matrix

QUARTILE_LABELS = ("Q1 (0-25%)", "Q2 (25-50%)", "Q3 (50-75%)", "Q4 (75-100%)")
QUARTILE_COLORS = ("#4C78A8", "#72B7B2", "#F58518", "#E45756")


@dataclass(frozen=True)
class QuartileSummary:
    label: str
    count: int
    median_pairwise_hamming: float


def _quartile_index(progress_percent: float) -> int:
    if progress_percent < 25.0:
        return 0
    if progress_percent < 50.0:
        return 1
    if progress_percent < 75.0:
        return 2
    return 3


def _median_pairwise_hamming_unique(genomes: list[Genome]) -> float:
    """Median Hamming distance over all unique unordered genome pairs in the set."""
    unique = list(dict.fromkeys(genomes))
    count = len(unique)
    if count < 2:
        return 0.0 if count == 1 else math.nan

    matrix = _pairwise_hamming_matrix(unique)
    upper = matrix[np.triu_indices(count, k=1)]
    return float(np.median(upper))


def _collect_quartile_summaries_from_histories(
    histories: list[EvolutionHistory],
) -> list[QuartileSummary]:
    genome_buckets: list[list[Genome]] = [[], [], [], []]

    for history in histories:
        accepted_records = list(iter_accepted_change_records(history))
        total_accepted = len(accepted_records)
        for accepted_step, _previous, current in accepted_records:
            progress = normalized_run_progress(accepted_step, total_accepted)
            genome_buckets[_quartile_index(progress)].append(current["genome"])

    return [
        QuartileSummary(
            label=label,
            count=len(genome_buckets[index]),
            median_pairwise_hamming=_median_pairwise_hamming_unique(genome_buckets[index]),
        )
        for index, label in enumerate(QUARTILE_LABELS)
    ]


def _load_unique_genomes_for_mds_json(json_path: Path) -> list[Genome] | None:
    sidecar_path = json_path.with_name(f"{json_path.stem}_genomes.json")
    if sidecar_path.is_file():
        payload = json.loads(sidecar_path.read_text(encoding="utf-8"))
        genomes = payload.get("genomes")
        if isinstance(genomes, list) and genomes:
            return [str(genome) for genome in genomes]

    payload = json.loads(json_path.read_text(encoding="utf-8"))
    genomes = payload.get("uniqueGenomes")
    if isinstance(genomes, list) and genomes:
        return [str(genome) for genome in genomes]

    return None


def _collect_quartile_summaries_from_shared_genome_mds_json(
    json_path: Path,
) -> list[QuartileSummary]:
    payload = json.loads(json_path.read_text(encoding="utf-8"))
    unique_genomes = _load_unique_genomes_for_mds_json(json_path)
    genome_buckets: list[list[Genome]] = [[], [], [], []]
    quartile_counts = [0, 0, 0, 0]

    for run in payload.get("runs", []):
        points = run.get("points", [])
        if len(points) <= 1:
            continue

        accepted_points = points[1:]
        total_accepted = len(accepted_points)
        for accepted_step, point in enumerate(accepted_points, start=1):
            progress = normalized_run_progress(accepted_step, total_accepted)
            quartile = _quartile_index(progress)
            quartile_counts[quartile] += 1

            if unique_genomes is None:
                continue

            genome_index = point.get("genomeIndex")
            if not isinstance(genome_index, int) or not (
                0 <= genome_index < len(unique_genomes)
            ):
                continue
            genome_buckets[quartile].append(unique_genomes[genome_index])

    return [
        QuartileSummary(
            label=label,
            count=quartile_counts[index],
            median_pairwise_hamming=(
                _median_pairwise_hamming_unique(genome_buckets[index])
                if unique_genomes is not None
                else math.nan
            ),
        )
        for index, label in enumerate(QUARTILE_LABELS)
    ]


def _load_quartile_counts_from_summary_csv(csv_path: Path) -> list[QuartileSummary] | None:
    if not csv_path.is_file():
        return None

    summaries: list[QuartileSummary] = []
    with csv_path.open(newline="", encoding="utf-8") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            quartile = row.get("quartile", "")
            if not quartile.startswith("Q"):
                continue
            try:
                count = int(row["accepted_genome_count"])
            except (KeyError, TypeError, ValueError):
                continue
            summaries.append(
                QuartileSummary(
                    label=quartile.replace("–", "-"),
                    count=count,
                    median_pairwise_hamming=math.nan,
                )
            )

    return summaries if len(summaries) == 4 else None


def _format_median_hamming(value: float) -> str:
    if math.isnan(value):
        return "n/a"
    return f"{value:.1f}"


def plot_accepted_genomes_by_progress_quartiles(
    summaries: list[QuartileSummary],
    output_path: Path,
    *,
    title: str,
    subtitle: str | None = None,
) -> None:
    labels = [summary.label for summary in summaries]
    counts = [summary.count for summary in summaries]
    total = sum(counts)

    fig, ax = plt.subplots(figsize=(8.5, 5.5))
    bars = ax.bar(
        labels,
        counts,
        color=QUARTILE_COLORS,
        edgecolor="black",
        linewidth=0.6,
    )

    ymax = max(counts) if counts else 1
    ax.set_ylim(0, ymax * 1.22)
    ax.set_ylabel("Accepted genomes (pooled across runs)")
    ax.set_xlabel("Evolution progress quartile")
    ax.set_title(title, pad=14)
    if subtitle:
        ax.text(
            0.5,
            1.02,
            subtitle,
            transform=ax.transAxes,
            ha="center",
            va="bottom",
            fontsize=10,
        )

    for bar, summary in zip(bars, summaries):
        height = bar.get_height()
        ax.text(
            bar.get_x() + bar.get_width() / 2,
            height + ymax * 0.02,
            (
                f"{summary.count:,}\n"
                f"(median pairwise H = {_format_median_hamming(summary.median_pairwise_hamming)})"
            ),
            ha="center",
            va="bottom",
            fontsize=9,
        )

    ax.text(
        0.99,
        0.98,
        f"Total accepted genomes: {total:,}",
        transform=ax.transAxes,
        ha="right",
        va="top",
        fontsize=10,
        bbox={"boxstyle": "round,pad=0.3", "facecolor": "white", "edgecolor": "#cccccc"},
    )

    fig.tight_layout()
    output_path.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(output_path, dpi=160, bbox_inches="tight")
    plt.close(fig)


def write_quartile_summary_csv(
    summaries: list[QuartileSummary],
    output_path: Path,
) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with output_path.open("w", newline="", encoding="utf-8") as handle:
        writer = csv.DictWriter(
            handle,
            fieldnames=[
                "quartile",
                "accepted_genome_count",
                "median_pairwise_hamming",
            ],
        )
        writer.writeheader()
        for summary in summaries:
            median_value = (
                ""
                if math.isnan(summary.median_pairwise_hamming)
                else f"{summary.median_pairwise_hamming:.6f}"
            )
            writer.writerow(
                {
                    "quartile": summary.label,
                    "accepted_genome_count": summary.count,
                    "median_pairwise_hamming": median_value,
                }
            )


def generate_accepted_genomes_by_progress_quartiles(
    histories: list[EvolutionHistory] | None = None,
    *,
    runs_dir: Path | None = None,
    shared_genome_mds_json_path: Path | None = None,
    output_plot_path: Path = ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT,
    output_csv_path: Path = ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV,
    title: str = "Accepted genomes by evolution progress quartile",
    subtitle: str | None = None,
) -> tuple[Path, Path, list[QuartileSummary]]:
    if shared_genome_mds_json_path is not None:
        summaries = _collect_quartile_summaries_from_shared_genome_mds_json(
            shared_genome_mds_json_path
        )
        if subtitle is None:
            subtitle = (
                "Shared initial genome; pooled across all runs; "
                "median pairwise Hamming among unique genomes per quartile"
            )
    elif histories is not None:
        summaries = _collect_quartile_summaries_from_histories(histories)
        if subtitle is None:
            subtitle = (
                "Pooled across all runs; "
                "median pairwise Hamming among unique genomes per quartile"
            )
    elif runs_dir is not None:
        contexts = get_contexts(DEFAULT_N_GENES)
        histories, _labels = load_histories_from_runs_dir(runs_dir, contexts)
        summaries = _collect_quartile_summaries_from_histories(histories)
        if subtitle is None:
            subtitle = (
                "Pooled across all runs; "
                "median pairwise Hamming among unique genomes per quartile"
            )
    else:
        raise ValueError(
            "Provide histories, runs_dir, or shared_genome_mds_json_path."
        )

    plot_accepted_genomes_by_progress_quartiles(
        summaries,
        output_plot_path,
        title=title,
        subtitle=subtitle,
    )
    write_quartile_summary_csv(summaries, output_csv_path)
    return output_plot_path, output_csv_path, summaries


def generate_shared_accepted_genomes_by_progress_quartiles(
    shared_genome_mds_json_path: Path = UNITY_SHARED_GENOME_MDS_JSON,
    output_plot_path: Path = SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT,
    output_csv_path: Path = SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV,
) -> tuple[Path, Path, list[QuartileSummary]]:
    sidecar_path = shared_genome_mds_json_path.with_name(
        f"{shared_genome_mds_json_path.stem}_genomes.json"
    )
    if not sidecar_path.is_file():
        print(
            "Warning: genome sidecar not found at "
            f"{sidecar_path}. Accepted counts will be plotted, but median "
            "pairwise Hamming requires re-exporting the shared MDS JSON "
            "(demo.py --shared-initial-genome)."
        )

    return generate_accepted_genomes_by_progress_quartiles(
        shared_genome_mds_json_path=shared_genome_mds_json_path,
        output_plot_path=output_plot_path,
        output_csv_path=output_csv_path,
        title="Accepted genomes by evolution progress quartile (shared initial genome)",
    )


def main() -> None:
    parser = argparse.ArgumentParser(
        description=(
            "Plot accepted-genome counts pooled by evolution-progress quartiles."
        )
    )
    parser.add_argument(
        "--shared-initial-genome",
        action="store_true",
        help="Use unity_shared_genome_mds_path.json instead of batch runs.",
    )
    parser.add_argument(
        "--runs-dir",
        type=Path,
        default=RUNS_OUTPUT_DIR,
        help="Directory containing run_*/evolution.json files.",
    )
    args = parser.parse_args()

    if args.shared_initial_genome:
        plot_path, csv_path, summaries = generate_shared_accepted_genomes_by_progress_quartiles()
    else:
        plot_path, csv_path, summaries = generate_accepted_genomes_by_progress_quartiles(
            runs_dir=args.runs_dir,
        )

    print(f"Wrote {plot_path}")
    print(f"Wrote {csv_path}")
    for summary in summaries:
        print(
            f"  {summary.label}: count={summary.count}, "
            f"median_pairwise_hamming={_format_median_hamming(summary.median_pairwise_hamming)}"
        )


if __name__ == "__main__":
    main()
