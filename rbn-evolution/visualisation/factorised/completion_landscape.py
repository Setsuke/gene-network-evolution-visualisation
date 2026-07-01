"""Factor completion landscape across ranked batch runs."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path
from typing import Literal

try:
    import matplotlib.pyplot as plt
    import numpy as np
    from matplotlib.colors import BoundaryNorm, ListedColormap
except ImportError:
    plt = None
    np = None
    BoundaryNorm = None
    ListedColormap = None

from visualisation.factorised.metrics import (
    PROGRESS_POINTS,
    extract_accepted_trajectory_metrics,
    generations_to_solution,
    plateau_duration,
    sample_perfect_factors_trajectory,
)
from rbn.types import Contexts, EvolutionHistory

RankBy = Literal["time_to_solution", "plateau_duration"]


@dataclass(frozen=True)
class RankedFactorCompletionRun:
    label: str
    rank: int
    generations_to_solution: int
    plateau_steps: int
    accepted_steps: int
    final_perfect_factors: int
    perfect_factors_by_progress: tuple[int, ...]


def _require_matplotlib():
    if plt is None or np is None or BoundaryNorm is None or ListedColormap is None:
        raise ImportError("matplotlib and numpy are required for factor completion plots")


def _completion_colormap(max_factors: int) -> ListedColormap:
    """Discrete colours: 7=red, 8=orange, 9=yellow, 10=green; lower values stay neutral."""
    color_by_value = [
        "#0f172a",
        "#1e293b",
        "#334155",
        "#475569",
        "#64748b",
        "#94a3b8",
        "#cbd5e1",
        "#dc2626",
        "#f97316",
        "#eab308",
        "#22c55e",
    ]
    if max_factors > len(color_by_value) - 1:
        raise ValueError(
            f"factor completion palette supports up to {len(color_by_value) - 1} factors"
        )
    return ListedColormap(color_by_value[: max_factors + 1], name="factor_completion")


def build_factor_completion_runs(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
    rank_by: RankBy = "time_to_solution",
) -> list[RankedFactorCompletionRun]:
    trajectories: list[RankedFactorCompletionRun] = []

    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        metrics = extract_accepted_trajectory_metrics(history, contexts)
        if not metrics:
            continue

        trajectories.append(
            RankedFactorCompletionRun(
                label=label,
                rank=0,
                generations_to_solution=generations_to_solution(metrics),
                plateau_steps=plateau_duration(metrics),
                accepted_steps=len(metrics),
                final_perfect_factors=metrics[-1].number_of_perfect_factors,
                perfect_factors_by_progress=sample_perfect_factors_trajectory(metrics),
            )
        )

    if not trajectories:
        raise ValueError("no accepted trajectory metrics found")

    sort_key = (
        (lambda item: (item.generations_to_solution, item.plateau_steps, item.label))
        if rank_by == "time_to_solution"
        else (lambda item: (item.plateau_steps, item.generations_to_solution, item.label))
    )
    ordered = sorted(trajectories, key=sort_key)
    return [
        RankedFactorCompletionRun(
            label=run.label,
            rank=rank,
            generations_to_solution=run.generations_to_solution,
            plateau_steps=run.plateau_steps,
            accepted_steps=run.accepted_steps,
            final_perfect_factors=run.final_perfect_factors,
            perfect_factors_by_progress=run.perfect_factors_by_progress,
        )
        for rank, run in enumerate(ordered)
    ]


def _write_factor_completion_csv(
    ranked_runs: list[RankedFactorCompletionRun],
    csv_path: Path,
    *,
    rank_by: RankBy,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            [
                "rank",
                "run_label",
                "rank_metric",
                "generations_to_solution",
                "plateau_steps",
                "accepted_steps",
                "final_perfect_factors",
                "progress_percent",
                "number_of_perfect_factors",
            ]
        )
        rank_metric_name = (
            "generations_to_solution"
            if rank_by == "time_to_solution"
            else "plateau_steps"
        )
        for run in ranked_runs:
            rank_metric = (
                run.generations_to_solution
                if rank_by == "time_to_solution"
                else run.plateau_steps
            )
            for progress, perfect_count in zip(
                PROGRESS_POINTS,
                run.perfect_factors_by_progress,
            ):
                writer.writerow(
                    [
                        run.rank + 1,
                        run.label,
                        rank_metric_name,
                        run.generations_to_solution,
                        run.plateau_steps,
                        run.accepted_steps,
                        run.final_perfect_factors,
                        progress,
                        perfect_count,
                    ]
                )


def _rank_footer(ranked_runs: list[RankedFactorCompletionRun], rank_by: RankBy) -> str:
    if rank_by == "time_to_solution":
        entries = (
            f"{run.rank + 1}:{run.label} (gen {run.generations_to_solution})"
            for run in ranked_runs[:6]
        )
        label = "Rank order (generations to solution)"
    else:
        entries = (
            f"{run.rank + 1}:{run.label} (plateau {run.plateau_steps})"
            for run in ranked_runs[:6]
        )
        label = "Rank order (plateau duration)"
    rank_labels = ", ".join(entries)
    if len(ranked_runs) > 6:
        rank_labels += ", ..."
    return f"{label}: {rank_labels}"


def plot_factor_completion_landscape(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
    rank_by: RankBy = "time_to_solution",
) -> Path:
    """
    Batch summary of factor completion across runs.

    X = normalised accepted trajectory progress (0-100%)
    Y = runs ranked by time to solution
    Colour = number_of_perfect_factors at that progress point
    """
    _require_matplotlib()

    if not histories:
        raise ValueError("histories must contain at least one run")

    ranked_runs = build_factor_completion_runs(
        histories,
        contexts,
        labels=labels,
        rank_by=rank_by,
    )
    max_factors = len(contexts) * n_genes
    matrix = np.array(
        [list(run.perfect_factors_by_progress) for run in ranked_runs],
        dtype=float,
    )

    if matrix.shape[0] == 1:
        matrix = np.vstack([matrix, matrix])

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_factor_completion_csv(ranked_runs, Path(csv_path), rank_by=rank_by)

    figure_height = max(6.0, 0.18 * len(ranked_runs) + 3.5)
    figure, axis = plt.subplots(figsize=(14, figure_height), constrained_layout=True)

    cmap = _completion_colormap(max_factors)
    boundaries = np.arange(-0.5, max_factors + 1.5, 1.0)
    norm = BoundaryNorm(boundaries, max_factors + 1, clip=True)
    image = axis.imshow(
        matrix,
        aspect="auto",
        origin="upper",
        cmap=cmap,
        norm=norm,
        interpolation="nearest",
    )

    axis.set_xticks(np.arange(len(PROGRESS_POINTS)))
    axis.set_xticklabels([str(progress) for progress in PROGRESS_POINTS], fontsize=8)
    axis.set_xlabel("Normalised evolutionary progress (%)")

    axis.set_yticks(np.arange(matrix.shape[0]))
    if len(ranked_runs) == 1:
        axis.set_yticklabels(["1"], fontsize=9)
    else:
        axis.set_yticklabels([str(run.rank + 1) for run in ranked_runs], fontsize=8)

    y_label = (
        "Run rank (time to solution)"
        if rank_by == "time_to_solution"
        else "Run rank (plateau duration)"
    )
    axis.set_ylabel(y_label)
    axis.set_title("Factor completion landscape")

    colorbar = figure.colorbar(image, ax=axis, fraction=0.025, pad=0.02, ticks=range(max_factors + 1))
    colorbar.set_label("Number of perfect factors")

    rank_title = (
        "Time to solution (generation of first fitness = 1.0)"
        if rank_by == "time_to_solution"
        else "Plateau duration (accepted steps at unchanged fitness)"
    )
    figure.suptitle(
        "Factor completion landscape across batch\n"
        f"{len(ranked_runs)} runs  |  {len(contexts)} contexts × {n_genes} genes  |  "
        f"Ranked by: {rank_title}",
        fontsize=12,
    )
    figure.text(0.02, 0.01, _rank_footer(ranked_runs, rank_by), fontsize=8)

    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
