"""Stacked bar charts of drift categories by normalised run progress (Unity buckets)."""

from __future__ import annotations

import csv
import json
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

try:
    import matplotlib.pyplot as plt
    import numpy as np
except ImportError:
    plt = None
    np = None

from config import DATA_OUTPUT_DIR, OUTPUT_DIR, RUNS_OUTPUT_DIR, get_contexts
from rbn.transition_table import build_transition_table_from_genome
from rbn.types import Contexts, EvolutionHistory, GenerationRecord
from visualisation.evolution_progress_breakdown import (
    PROGRESS_BIN_COUNT,
    PROGRESS_BIN_LABELS,
    progress_bin_index,
)
from visualisation.genome_mds_path import extract_accepted_genome_paths
from visualisation.plateau_drift_classification import (
    PLATEAU_DRIFT_EXIT_CATEGORIES,
    find_plateau_episodes,
)
from visualisation.progress_attractor_counts import normalized_trajectory_progress

DriftCategory = Literal["neutral", "path_changed", "attractor_changed"]
ProgressCategory = DriftCategory | Literal["improvement"]

DRIFT_ONLY_CATEGORY_ORDER: tuple[DriftCategory, ...] = (
    "neutral",
    "path_changed",
    "attractor_changed",
)

WITH_IMPROVEMENT_CATEGORY_ORDER: tuple[ProgressCategory, ...] = (
    "neutral",
    "path_changed",
    "attractor_changed",
    "improvement",
)

DRIFT_CATEGORY_LABELS: dict[ProgressCategory, str] = {
    "neutral": "Neutral",
    "path_changed": "Path changed",
    "attractor_changed": "Context attractor changed",
    "improvement": "Improvement (direct + compensatory)",
}

DRIFT_ONLY_COLORS: dict[DriftCategory, str] = {
    "neutral": "#eb3838",
    "path_changed": "#21c45e",
    "attractor_changed": "#1473ff",
}

WITH_IMPROVEMENT_COLORS: dict[ProgressCategory, str] = {
    **DRIFT_ONLY_COLORS,
    "improvement": "#9ca3af",
}


@dataclass(frozen=True)
class CategoryProgressEvent:
    run_label: str
    step_index: int
    progress_percent: float
    bin_index: int
    category: ProgressCategory


def _require_matplotlib() -> None:
    if plt is None or np is None:
        raise ImportError("matplotlib and numpy are required for drift category plots")


def _normalize_change_type(change_type: str) -> str:
    if change_type in {
        "partial_improvement",
        "pure_improvement",
        "partial_global_improvement",
    }:
        return "direct_improvement"
    return change_type


def is_improvement_move(change_type: str) -> bool:
    normalized = _normalize_change_type(change_type)
    return normalized in {"direct_improvement", "compensatory_improvement"}


def classify_drift_category(change_type: str) -> DriftCategory:
    """Match Unity GenomePathChangeTypeStyles.GetDriftCategoryBucket."""
    if change_type == "path_transitions_changed_attractor_cycle_unchanged":
        return "path_changed"
    if change_type in {
        "attractor_cycle_changed_factor_scores_unchanged",
        "factor_changing_neutral",
    }:
        return "attractor_changed"
    return "neutral"


def classify_progress_category(change_type: str, *, include_improvement_moves: bool) -> ProgressCategory:
    if include_improvement_moves and is_improvement_move(change_type):
        return "improvement"
    return classify_drift_category(change_type)


def _append_event(
    events: list[CategoryProgressEvent],
    *,
    run_label: str,
    step_index: int,
    total_steps: int,
    change_type: str,
    include_improvement_moves: bool,
) -> None:
    progress_percent = normalized_trajectory_progress(step_index, total_steps)
    events.append(
        CategoryProgressEvent(
            run_label=run_label,
            step_index=step_index,
            progress_percent=progress_percent,
            bin_index=progress_bin_index(progress_percent),
            category=classify_progress_category(
                change_type,
                include_improvement_moves=include_improvement_moves,
            ),
        )
    )


def collect_category_progress_events(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
    include_improvement_moves: bool = False,
) -> list[CategoryProgressEvent]:
    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    events: list[CategoryProgressEvent] = []

    for path in paths:
        fitness_values = list(path.fitness_values)
        change_types = list(path.change_types)
        total_steps = len(fitness_values)

        if include_improvement_moves:
            for step_index in range(1, total_steps):
                change_type = change_types[step_index]
                _append_event(
                    events,
                    run_label=path.run_label,
                    step_index=step_index,
                    total_steps=total_steps,
                    change_type=change_type,
                    include_improvement_moves=True,
                )
            continue

        for start_step, end_step, _exit_step in find_plateau_episodes(fitness_values):
            for step_index in range(start_step + 1, end_step + 1):
                change_type = change_types[step_index]
                if change_type in PLATEAU_DRIFT_EXIT_CATEGORIES:
                    continue
                _append_event(
                    events,
                    run_label=path.run_label,
                    step_index=step_index,
                    total_steps=total_steps,
                    change_type=change_type,
                    include_improvement_moves=False,
                )

    return events


def build_category_bin_proportions(
    events: list[CategoryProgressEvent],
    *,
    category_order: tuple[ProgressCategory, ...],
) -> tuple[np.ndarray, np.ndarray]:
    _require_matplotlib()
    counts = np.zeros((PROGRESS_BIN_COUNT, len(category_order)), dtype=float)
    category_index = {category: index for index, category in enumerate(category_order)}

    for event in events:
        counts[event.bin_index, category_index[event.category]] += 1.0

    totals = counts.sum(axis=1)
    proportions = np.zeros_like(counts)
    for bin_index in range(PROGRESS_BIN_COUNT):
        if totals[bin_index] > 0:
            proportions[bin_index] = counts[bin_index] / totals[bin_index]
    return counts, proportions


def _write_csv(
    counts: np.ndarray,
    proportions: np.ndarray,
    csv_path: Path,
    *,
    category_order: tuple[ProgressCategory, ...],
    run_count: int,
    total_events: int,
    move_label: str,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            [
                "progress_bin",
                "category",
                "count",
                "proportion_within_bin",
                "runs",
                f"total_{move_label}",
            ]
        )
        for bin_index, bin_label in enumerate(PROGRESS_BIN_LABELS):
            for category_index, category in enumerate(category_order):
                writer.writerow(
                    [
                        bin_label,
                        DRIFT_CATEGORY_LABELS[category],
                        int(counts[bin_index, category_index]),
                        f"{proportions[bin_index, category_index]:.6f}",
                        run_count,
                        total_events,
                    ]
                )


def plot_categories_normalised_progress(
    events: list[CategoryProgressEvent],
    save_path: str | Path,
    *,
    run_count: int,
    category_order: tuple[ProgressCategory, ...],
    category_colors: dict[ProgressCategory, str],
    title: str,
    y_label: str,
    x_label: str = "Normalised evolutionary progression",
    footer_text: str,
    csv_path: str | Path | None = None,
    csv_move_label: str = "moves",
) -> Path:
    _require_matplotlib()
    if not events:
        raise ValueError("no classified moves found")

    counts, proportions = build_category_bin_proportions(
        events,
        category_order=category_order,
    )
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_csv(
            counts,
            proportions,
            Path(csv_path),
            category_order=category_order,
            run_count=run_count,
            total_events=len(events),
            move_label=csv_move_label,
        )

    x_positions = np.arange(PROGRESS_BIN_COUNT)
    figure, axis = plt.subplots(figsize=(12, 7))
    bottom = np.zeros(PROGRESS_BIN_COUNT, dtype=float)

    for category_index, category in enumerate(category_order):
        values = proportions[:, category_index] * 100.0
        axis.bar(
            x_positions,
            values,
            bottom=bottom,
            label=DRIFT_CATEGORY_LABELS[category],
            color=category_colors[category],
            edgecolor="white",
            linewidth=0.8,
            width=0.78,
        )
        bottom += values

    axis.set_xticks(x_positions)
    axis.set_xticklabels(PROGRESS_BIN_LABELS, fontsize=10)
    axis.set_xlabel(x_label)
    axis.set_ylabel(y_label)
    axis.set_ylim(0.0, 100.0)
    axis.set_title(title)
    axis.grid(axis="y", alpha=0.25)

    handles, legend_labels = axis.get_legend_handles_labels()
    figure.legend(
        handles,
        legend_labels,
        loc="upper center",
        bbox_to_anchor=(0.5, 0.10),
        ncol=min(4, len(category_order)),
        fontsize=9,
        frameon=False,
        columnspacing=1.2,
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

    figure.subplots_adjust(bottom=0.24, top=0.92)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.45)
    plt.close(figure)
    return save_path


def plot_drifting_categories_normalised_progress(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    events = collect_category_progress_events(
        histories,
        contexts,
        labels=labels,
        include_improvement_moves=False,
    )
    return plot_categories_normalised_progress(
        events,
        save_path,
        run_count=len(histories),
        category_order=DRIFT_ONLY_CATEGORY_ORDER,
        category_colors=DRIFT_ONLY_COLORS,
        title="Drifting Categories - Normalised progress",
        y_label="Proportion of plateau drift moves (%)",
        footer_text=(
            f"{len(histories)} runs  |  {len(events)} plateau drift moves"
        ),
        csv_path=csv_path,
        csv_move_label="drift_moves",
    )


def plot_drifting_categories_with_improvement_normalised_progress(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    events = collect_category_progress_events(
        histories,
        contexts,
        labels=labels,
        include_improvement_moves=True,
    )
    return plot_categories_normalised_progress(
        events,
        save_path,
        run_count=len(histories),
        category_order=WITH_IMPROVEMENT_CATEGORY_ORDER,
        category_colors=WITH_IMPROVEMENT_COLORS,
        title="Movement Categories Across Normalised Evolutionary Progression",
        y_label="Proportion of accepted moves (%)",
        footer_text=(
            f"{len(histories):,} evolutionary trajectories  |  "
            f"{len(events):,} accepted genomic changes"
        ),
        csv_path=csv_path,
        csv_move_label="accepted_moves",
    )


def _generation_record_from_json(entry: dict, n_genes: int) -> GenerationRecord:
    details: dict = {}
    for context_entry in entry["contexts"]:
        initial = context_entry["initial"]
        cycle = list(context_entry["cycle"])
        details[initial] = {
            "target": context_entry["target"],
            "path": list(context_entry["path"]),
            "cycle": cycle,
            "fitness": float(context_entry["fitness"]),
            "gene_scores": context_entry.get("geneScores"),
            "cycle_length": int(context_entry.get("cycleLength", len(cycle))),
        }

    genome = entry["genome"]
    transition_table, _, _ = build_transition_table_from_genome(genome, n_genes)
    return {
        "generation": int(entry["generation"]),
        "genome": genome,
        "fitness": float(entry["fitness"]),
        "accepted": bool(entry["accepted"]),
        "changed_bits": int(entry["changedBits"]),
        "transition_table": transition_table,
        "details": details,
        "factor_scores": entry.get("factorScores"),
    }


def load_evolution_history_from_json(path: Path, contexts: Contexts) -> EvolutionHistory:
    del contexts
    payload = json.loads(path.read_text(encoding="utf-8"))
    n_genes = int(payload["nGenes"])
    return [_generation_record_from_json(entry, n_genes) for entry in payload["generations"]]


def load_histories_from_runs_dir(runs_dir: Path, contexts: Contexts) -> tuple[list[EvolutionHistory], list[str]]:
    run_paths = sorted(runs_dir.glob("run_*/evolution.json"))
    if not run_paths:
        raise FileNotFoundError(f"No evolution.json files found under {runs_dir}")

    histories: list[EvolutionHistory] = []
    labels: list[str] = []
    total = len(run_paths)

    for index, evolution_path in enumerate(run_paths, start=1):
        histories.append(load_evolution_history_from_json(evolution_path, contexts))
        labels.append(f"Run {index}/{total}")
        if index % 100 == 0 or index == total:
            print(f"  loaded {index}/{total} runs...")

    return histories, labels


if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Plot drifting categories by normalised run progress.",
    )
    parser.add_argument(
        "--runs-dir",
        type=Path,
        default=RUNS_OUTPUT_DIR,
        help="Directory with run_NNN/evolution.json files.",
    )
    parser.add_argument(
        "--png",
        type=Path,
        default=OUTPUT_DIR / "drifting_categories_normalised_progress.png",
        help="Output PNG path for drift-only chart.",
    )
    parser.add_argument(
        "--csv",
        type=Path,
        default=DATA_OUTPUT_DIR / "drifting_categories_normalised_progress.csv",
        help="Output CSV path for drift-only chart.",
    )
    parser.add_argument(
        "--with-improvement-png",
        type=Path,
        default=OUTPUT_DIR / "drifting_categories_with_improvement_normalised_progress.png",
        help="Output PNG path for chart including improvement moves.",
    )
    parser.add_argument(
        "--with-improvement-csv",
        type=Path,
        default=DATA_OUTPUT_DIR / "drifting_categories_with_improvement_normalised_progress.csv",
        help="Output CSV path for chart including improvement moves.",
    )
    parser.add_argument("--n-genes", type=int, default=5)
    args = parser.parse_args()

    contexts = get_contexts(args.n_genes)
    histories, labels = load_histories_from_runs_dir(args.runs_dir, contexts)

    print("Collecting plateau drift category events...")
    drift_png = plot_drifting_categories_normalised_progress(
        histories,
        contexts,
        args.png,
        labels=labels,
        csv_path=args.csv,
    )
    print(f"Wrote {drift_png}")

    print("Collecting accepted moves including improvement...")
    improvement_png = plot_drifting_categories_with_improvement_normalised_progress(
        histories,
        contexts,
        args.with_improvement_png,
        labels=labels,
        csv_path=args.with_improvement_csv,
    )
    print(f"Wrote {improvement_png}")
