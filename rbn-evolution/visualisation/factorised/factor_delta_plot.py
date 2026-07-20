"""Heatmap of factor score changes between consecutive accepted steps."""

from __future__ import annotations

from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
    from matplotlib.colors import BoundaryNorm, ListedColormap
except ImportError:
    plt = None
    np = None
    BoundaryNorm = None
    ListedColormap = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.factorised.metrics import (
    build_factor_delta_matrix,
    extract_accepted_trajectory_metrics,
    factor_row_labels,
)

WORSE_COLOR = "#dc2626"
UNCHANGED_COLOR = "#9ca3af"
BETTER_COLOR = "#22c55e"


def _require_matplotlib():
    if plt is None or np is None or BoundaryNorm is None or ListedColormap is None:
        raise ImportError("matplotlib and numpy are required for factor delta plots")


def _delta_sign_matrix(delta_matrix: list[list[float]]) -> np.ndarray:
    signs = np.zeros((len(delta_matrix), len(delta_matrix[0])), dtype=int)
    for row_index, row in enumerate(delta_matrix):
        for column_index, delta in enumerate(row):
            if delta > 0.0:
                signs[row_index, column_index] = 1
            elif delta < 0.0:
                signs[row_index, column_index] = -1
    return signs


def plot_factor_delta_heatmap(
    history: EvolutionHistory,
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path | None = None,
    *,
    title: str | None = None,
) -> Path | None:
    """
    Factor delta heatmap.

    X = accepted step, Y = context-gene factor,
    colour = change in factor score (green = improved, red = worsened, grey = unchanged).
    """
    _require_matplotlib()

    metrics = extract_accepted_trajectory_metrics(history, contexts)
    if not metrics:
        raise ValueError("no accepted trajectory steps found")

    delta_matrix = build_factor_delta_matrix(metrics)
    sign_matrix = _delta_sign_matrix(delta_matrix)
    row_labels = factor_row_labels(contexts, n_genes)
    steps = [step.step_index for step in metrics]

    step_count = len(metrics)
    factor_count = len(row_labels)
    figure, axis = plt.subplots(figsize=(max(10.0, 0.35 * step_count + 4.0), max(5.0, 0.45 * factor_count + 1.5)))

    cmap = ListedColormap([WORSE_COLOR, UNCHANGED_COLOR, BETTER_COLOR])
    norm = BoundaryNorm([-1.5, -0.5, 0.5, 1.5], cmap.N)

    image = axis.imshow(
        sign_matrix,
        aspect="auto",
        origin="upper",
        cmap=cmap,
        norm=norm,
        interpolation="nearest",
    )
    axis.set_xticks(np.arange(step_count))
    axis.set_xticklabels(steps, fontsize=8)
    axis.set_xlabel("Accepted step")
    axis.set_yticks(np.arange(factor_count))
    axis.set_yticklabels(row_labels, fontsize=9)
    axis.set_ylabel("Context-gene factor")
    axis.set_title("Factor score change")

    colorbar = figure.colorbar(image, ax=axis, fraction=0.025, pad=0.02, ticks=[-1, 0, 1])
    colorbar.ax.set_yticklabels(["Worsened", "Unchanged", "Improved"])

    figure.suptitle(
        title
        or (
            "Factor Delta Heatmap\n"
            f"{step_count} accepted steps  |  "
            f"{len(contexts)} contexts × {n_genes} genes = {factor_count} factors"
        ),
        fontsize=12,
    )

    figure.tight_layout()

    if save_path is None:
        plt.show()
        return None

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
