"""Line plot of local (context) and total fitness over accepted generations."""

from __future__ import annotations

from pathlib import Path

try:
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.factorised.metrics import (
    context_order,
    extract_accepted_trajectory_metrics,
)

CONTEXT_COLORS = ("#059669", "#d97706", "#7c3aed", "#db2777", "#0891b2")
TOTAL_COLOR = "#2563eb"


def _require_matplotlib():
    if plt is None:
        raise ImportError("matplotlib is required for accepted fitness plots")


def plot_accepted_fitness_trajectory(
    history: EvolutionHistory,
    contexts: Contexts,
    save_path: str | Path | None = None,
    *,
    title: str | None = None,
) -> Path | None:
    """
    Plot local context fitness and total fitness on the same axes.

    X axis: accepted generation (one point per accepted genome change).
    Y axis: fitness in [0, 1].
    """
    _require_matplotlib()

    metrics = extract_accepted_trajectory_metrics(history, contexts)
    if not metrics:
        raise ValueError("no accepted trajectory steps found")

    accepted_steps = [step.step_index + 1 for step in metrics]
    total_fitness = [step.total_fitness for step in metrics]
    context_inits = context_order(contexts)

    figure, axis = plt.subplots(figsize=(10, 5))

    for index, initial in enumerate(context_inits):
        color = CONTEXT_COLORS[index % len(CONTEXT_COLORS)]
        values = [step.context_fitness[index] for step in metrics]
        axis.plot(
            accepted_steps,
            values,
            color=color,
            marker="o",
            markersize=3,
            linewidth=1.5,
            label=f"Context {index + 1} ({initial})",
        )

    axis.plot(
        accepted_steps,
        total_fitness,
        color=TOTAL_COLOR,
        marker="o",
        markersize=4,
        linewidth=2.5,
        label="Total fitness",
    )

    axis.set_xlabel("Accepted generation")
    axis.set_ylabel("Fitness")
    axis.set_ylim(0.0, 1.02)
    axis.set_xlim(accepted_steps[0], accepted_steps[-1])
    axis.set_title(
        title
        or (
            "Local and Total Fitness over Accepted Generations\n"
            f"{len(metrics)} accepted steps"
        )
    )
    axis.grid(alpha=0.25)
    axis.legend(loc="lower right", fontsize=9)

    figure.tight_layout()

    if save_path is None:
        plt.show()
        return None

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
