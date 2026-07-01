"""Factorised fitness trajectory heatmap with summary line plots."""

from __future__ import annotations

from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
    from matplotlib.colors import LinearSegmentedColormap
    from matplotlib.gridspec import GridSpec
except ImportError:
    plt = None
    np = None
    LinearSegmentedColormap = None
    GridSpec = None

from visualisation.factorised.metrics import (
    build_factor_score_matrix,
    export_factorised_trajectory_csv,
    extract_accepted_trajectory_metrics,
    factor_row_labels,
)
from rbn.types import Contexts, EvolutionHistory


def _require_matplotlib():
    if plt is None or np is None or LinearSegmentedColormap is None or GridSpec is None:
        raise ImportError("matplotlib and numpy are required for factorised trajectory plots")


def _score_colormap():
    return LinearSegmentedColormap.from_list(
        "gene_score",
        ["#1e293b", "#475569", "#94a3b8", "#86efac", "#22c55e"],
        N=256,
    )


def plot_factorised_fitness_trajectory(
    history: EvolutionHistory,
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path | None = None,
    *,
    csv_path: str | Path | None = None,
    title: str | None = None,
) -> Path | None:
    """
    Factorised Fitness Trajectory View.

    Main panel: heatmap with X = accepted step, Y = context-gene factor, colour = gene_score.
    Side panels: total fitness and bottleneck score over accepted steps.
    """
    _require_matplotlib()

    metrics = extract_accepted_trajectory_metrics(history, contexts)
    if not metrics:
        raise ValueError("no accepted trajectory steps found")

    if csv_path is not None:
        export_factorised_trajectory_csv(metrics, contexts, n_genes, csv_path)

    matrix = np.array(build_factor_score_matrix(metrics), dtype=float)
    row_labels = factor_row_labels(contexts, n_genes)
    steps = [step.step_index for step in metrics]
    total_fitness = [step.total_fitness for step in metrics]
    bottleneck = [step.bottleneck_score for step in metrics]

    step_count = len(metrics)
    factor_count = len(row_labels)
    figure = plt.figure(figsize=(16, max(7.0, 0.45 * factor_count + 2.5)))
    grid = GridSpec(
        2,
        2,
        figure=figure,
        width_ratios=[3.2, 1.0],
        height_ratios=[1.0, 1.0],
        wspace=0.22,
        hspace=0.28,
    )

    heatmap_axis = figure.add_subplot(grid[:, 0])
    fitness_axis = figure.add_subplot(grid[0, 1])
    bottleneck_axis = figure.add_subplot(grid[1, 1])

    cmap = _score_colormap()
    image = heatmap_axis.imshow(
        matrix,
        aspect="auto",
        origin="upper",
        cmap=cmap,
        vmin=0.0,
        vmax=1.0,
        interpolation="nearest",
    )
    heatmap_axis.set_xticks(np.arange(step_count))
    heatmap_axis.set_xticklabels(steps, fontsize=8)
    heatmap_axis.set_xlabel("Accepted evolutionary step")
    heatmap_axis.set_yticks(np.arange(factor_count))
    heatmap_axis.set_yticklabels(row_labels, fontsize=9)
    heatmap_axis.set_ylabel("Context-gene factor")
    heatmap_axis.set_title("Factorised gene scores")

    colorbar = figure.colorbar(image, ax=heatmap_axis, fraction=0.025, pad=0.02)
    colorbar.set_label("Gene score (0 = unresolved, 1.0 = perfect)")

    fitness_axis.plot(steps, total_fitness, color="#2563eb", marker="o", markersize=3)
    fitness_axis.set_ylim(0.0, 1.02)
    fitness_axis.set_xlim(steps[0], steps[-1])
    fitness_axis.set_ylabel("Total fitness")
    fitness_axis.set_title("Total fitness over time")
    fitness_axis.grid(alpha=0.25)

    bottleneck_axis.plot(steps, bottleneck, color="#dc2626", marker="o", markersize=3)
    bottleneck_axis.set_ylim(0.0, 1.02)
    bottleneck_axis.set_xlim(steps[0], steps[-1])
    bottleneck_axis.set_xlabel("Accepted step")
    bottleneck_axis.set_ylabel("Bottleneck score")
    bottleneck_axis.set_title("Bottleneck score over time")
    bottleneck_axis.grid(alpha=0.25)

    figure.suptitle(
        title
        or (
            "Factorised Fitness Trajectory View\n"
            f"{step_count} accepted steps  |  "
            f"{len(contexts)} contexts × {n_genes} genes = {factor_count} factors"
        ),
        fontsize=12,
    )

    if save_path is None:
        plt.show()
        return None

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
