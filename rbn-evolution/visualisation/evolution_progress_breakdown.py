"""Temporal breakdown of accepted move categories by normalised run progress."""

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

from rbn.types import Contexts, EvolutionHistory
from visualisation.accepted_move_taxonomy import (
    TOP_LEVEL_MOVE_CATEGORY_LABELS,
    TOP_LEVEL_MOVE_CATEGORY_ORDER,
    TopLevelMoveCategory,
    classify_top_level_accepted_move_category,
    iter_accepted_change_records,
)

ProgressMoveCategory = TopLevelMoveCategory

PROGRESS_BIN_COUNT = 10
PROGRESS_BIN_LABELS = tuple(
    f"{start}-{start + 10}%"
    for start in range(0, 100, 10)
)

CATEGORY_ORDER = TOP_LEVEL_MOVE_CATEGORY_ORDER

CATEGORY_LABELS = TOP_LEVEL_MOVE_CATEGORY_LABELS

CATEGORY_COLORS: dict[ProgressMoveCategory, str] = {
    "factor_neutral": "#0284c7",
    "factor_changing_neutral": "#d97706",
    "direct_improvement": "#facc15",
    "compensatory_improvement": "#4ade80",
}


@dataclass(frozen=True)
class ProgressMoveEvent:
    run_label: str
    accepted_step: int
    progress_percent: float
    bin_index: int
    category: ProgressMoveCategory


def _require_matplotlib() -> None:
    if plt is None or np is None:
        raise ImportError("matplotlib and numpy are required for progress breakdown plots")


def normalized_run_progress(accepted_step: int, total_accepted: int) -> float:
    if total_accepted <= 1:
        return 100.0
    return 100.0 * (accepted_step - 1) / (total_accepted - 1)


def progress_bin_index(progress_percent: float) -> int:
    if progress_percent >= 100.0:
        return PROGRESS_BIN_COUNT - 1
    return min(PROGRESS_BIN_COUNT - 1, int(progress_percent // 10))


def classify_progress_move_category(
    history_previous,
    history_current,
    contexts: Contexts,
    *,
    n_genes: int,
) -> ProgressMoveCategory:
    del n_genes
    return classify_top_level_accepted_move_category(
        history_previous, history_current, contexts
    )


def collect_progress_move_events(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> list[ProgressMoveEvent]:
    events: list[ProgressMoveEvent] = []
    for run_index, history in enumerate(histories):
        run_label = labels[run_index] if labels is not None else f"run_{run_index + 1:03d}"
        accepted_records = list(iter_accepted_change_records(history))
        total_accepted = len(accepted_records)
        for accepted_step, previous, current in accepted_records:
            category = classify_progress_move_category(
                previous, current, contexts, n_genes=n_genes
            )
            progress_percent = normalized_run_progress(accepted_step, total_accepted)
            events.append(
                ProgressMoveEvent(
                    run_label=run_label,
                    accepted_step=accepted_step,
                    progress_percent=progress_percent,
                    bin_index=progress_bin_index(progress_percent),
                    category=category,
                )
            )
    return events


def build_progress_bin_proportions(
    events: list[ProgressMoveEvent],
) -> tuple[np.ndarray, np.ndarray]:
    _require_matplotlib()
    counts = np.zeros((PROGRESS_BIN_COUNT, len(CATEGORY_ORDER)), dtype=float)
    category_index = {category: index for index, category in enumerate(CATEGORY_ORDER)}

    for event in events:
        counts[event.bin_index, category_index[event.category]] += 1.0

    totals = counts.sum(axis=1)
    proportions = np.zeros_like(counts)
    for bin_index in range(PROGRESS_BIN_COUNT):
        if totals[bin_index] > 0:
            proportions[bin_index] = counts[bin_index] / totals[bin_index]
    return counts, proportions


def _write_progress_breakdown_csv(
    counts: np.ndarray,
    proportions: np.ndarray,
    csv_path: Path,
    *,
    run_count: int,
    total_events: int,
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
                "total_events",
            ]
        )
        for bin_index, bin_label in enumerate(PROGRESS_BIN_LABELS):
            for category_index, category in enumerate(CATEGORY_ORDER):
                writer.writerow(
                    [
                        bin_label,
                        CATEGORY_LABELS[category],
                        int(counts[bin_index, category_index]),
                        f"{proportions[bin_index, category_index]:.6f}",
                        run_count,
                        total_events,
                    ]
                )


def plot_evolution_progress_breakdown(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    """
    Stacked bar chart of accepted move categories by normalised run progress.

    X = progress bins (0-10%, ..., 90-100%)
    Y = proportion of accepted moves within each bin
    Colour = move category
    """
    _require_matplotlib()
    if not histories:
        raise ValueError("histories must contain at least one run")

    events = collect_progress_move_events(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    if not events:
        raise ValueError("no classified accepted move events found")

    counts, proportions = build_progress_bin_proportions(events)
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_progress_breakdown_csv(
            counts,
            proportions,
            Path(csv_path),
            run_count=len(histories),
            total_events=len(events),
        )

    x_positions = np.arange(PROGRESS_BIN_COUNT)
    figure, axis = plt.subplots(figsize=(12, 7))
    bottom = np.zeros(PROGRESS_BIN_COUNT, dtype=float)

    for category_index, category in enumerate(CATEGORY_ORDER):
        values = proportions[:, category_index] * 100.0
        axis.bar(
            x_positions,
            values,
            bottom=bottom,
            label=CATEGORY_LABELS[category],
            color=CATEGORY_COLORS[category],
            edgecolor="white",
            linewidth=0.8,
            width=0.78,
        )
        bottom += values

    axis.set_xticks(x_positions)
    axis.set_xticklabels(PROGRESS_BIN_LABELS, fontsize=10)
    axis.set_xlabel("Normalised run progress")
    axis.set_ylabel("Proportion of accepted moves (%)")
    axis.set_ylim(0.0, 100.0)
    axis.set_title("Evolution progress breakdown")
    axis.grid(axis="y", alpha=0.25)

    footer_text = (
        f"{len(histories)} runs  |  {len(events)} accepted moves  |  "
        "proportions computed within each progress bin"
    )
    handles, legend_labels = axis.get_legend_handles_labels()
    figure.legend(
        handles,
        legend_labels,
        loc="upper center",
        bbox_to_anchor=(0.5, 0.10),
        ncol=4,
        fontsize=9,
        frameon=False,
        columnspacing=1.1,
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
