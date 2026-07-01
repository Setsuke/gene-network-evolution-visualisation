"""Batch Fitness Load Heatmap: accepted-genome load across fitness intervals (all runs)."""

from __future__ import annotations

import csv
from pathlib import Path

try:
    import matplotlib.pyplot as plt
    import numpy as np
    from matplotlib.colors import LinearSegmentedColormap
except ImportError:
    plt = None
    np = None
    LinearSegmentedColormap = None

from exports.unity_trajectories import extract_accepted_fitness_trajectory
from rbn.types import EvolutionHistory

DEFAULT_HEATMAP_BIN_WIDTH = 0.01
DEFAULT_HEATMAP_COLUMNS = 10
DEFAULT_LOAD_CUTOFF = 0.5


def _require_matplotlib():
    if plt is None or np is None or LinearSegmentedColormap is None:
        raise ImportError("matplotlib and numpy are required for batch fitness load plots")


def _load_colormap():
    return LinearSegmentedColormap.from_list(
        "batch_fitness_load",
        ["#ffffff", "#dbeafe", "#93c5fd", "#3b82f6", "#1e3a8a"],
        N=256,
    )


def _counts_to_grid(
    counts: list[int],
    *,
    columns: int,
) -> tuple[np.ndarray, int]:
    bin_total = len(counts)
    rows = int(np.ceil(bin_total / columns))
    padded = counts + [0] * (rows * columns - bin_total)
    matrix = np.array(padded, dtype=int).reshape(rows, columns)
    return matrix, rows


def collect_accepted_fitness_values(histories: list[EvolutionHistory]) -> list[float]:
    """Flatten accepted fitness values from every run."""
    values: list[float] = []
    for history in histories:
        values.extend(extract_accepted_fitness_trajectory(history))
    return values


def compute_fitness_bin_counts(
    fitness_values: list[float],
    *,
    bin_width: float = DEFAULT_HEATMAP_BIN_WIDTH,
) -> tuple[list[str], list[int], list[float]]:
    if bin_width <= 0 or bin_width > 1:
        raise ValueError("bin_width must be in (0, 1]")

    bin_count = int(round(1.0 / bin_width))
    edges = np.linspace(0.0, 1.0, bin_count + 1)
    counts, _ = np.histogram(fitness_values, bins=edges)

    labels: list[str] = []
    for index in range(bin_count):
        low = edges[index]
        high = edges[index + 1]
        labels.append(f"{low:.2f}-{high:.2f}")

    centers = ((edges[:-1] + edges[1:]) / 2.0).tolist()
    return labels, counts.tolist(), centers


def _write_batch_fitness_load_csv(
    labels: list[str],
    counts: list[int],
    centers: list[float],
    csv_path: Path,
    *,
    total: int,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(
            ["fitness_band", "band_center", "accepted_genome_count", "share_percent"]
        )
        for label, center, count in zip(labels, centers, counts):
            share = (count * 100.0 / total) if total else 0.0
            writer.writerow([label, f"{center:.4f}", count, f"{share:.2f}"])


def _annotate_heatmap_cells(
    axis,
    matrix: np.ndarray,
    counts: list[int],
    band_labels: list[str],
    *,
    columns: int,
    vmax: int,
    count_font_size: float,
    label_font_size: float,
) -> None:
    threshold = max(1, vmax * 0.45)
    for row in range(matrix.shape[0]):
        for col in range(matrix.shape[1]):
            index = row * columns + col
            if index >= len(counts):
                continue
            value = int(counts[index])
            text_color = "#8b919a" if value == 0 else (
                "white" if value >= threshold else "#111827"
            )
            axis.text(
                col,
                row - 0.14,
                band_labels[index],
                ha="center",
                va="center",
                fontsize=label_font_size,
                color=text_color,
            )
            axis.text(
                col,
                row + 0.20,
                f"{value:,}",
                ha="center",
                va="center",
                fontsize=count_font_size,
                color=text_color,
                fontweight="bold",
            )


def _draw_aggregate_heatmap_panel(
    axis,
    counts: list[int],
    band_labels: list[str],
    *,
    bin_width: float,
    columns: int,
    vmax: int,
    title: str,
):
    matrix, row_count = _counts_to_grid(counts, columns=columns)
    cmap = _load_colormap()
    image = axis.imshow(
        matrix,
        aspect="equal",
        origin="upper",
        cmap=cmap,
        vmin=0,
        vmax=vmax,
    )
    if len(counts) <= 25:
        count_font_size = 18
        label_font_size = 13
    elif len(counts) <= 60:
        count_font_size = 16
        label_font_size = 11
    else:
        count_font_size = 16
        label_font_size = 11
    _annotate_heatmap_cells(
        axis,
        matrix,
        counts,
        band_labels,
        columns=columns,
        vmax=vmax,
        count_font_size=count_font_size,
        label_font_size=label_font_size,
    )

    axis.set_xticks(np.arange(columns))
    axis.set_xticklabels([])
    axis.set_xlabel(f"Each cell is one fitness band of width {bin_width:.2f}")

    row_labels: list[str] = []
    for row in range(row_count):
        block_start = row * columns * bin_width
        block_end = block_start + columns * bin_width
        row_labels.append(f"{block_start:.2f}–{block_end:.2f}")

    axis.set_yticks(np.arange(row_count))
    axis.set_yticklabels(row_labels, fontsize=10)
    axis.set_ylabel("Fitness range (row)")
    axis.set_title(title)
    return image


def plot_batch_fitness_load_heatmap(
    histories: list[EvolutionHistory],
    save_path: str | Path,
    *,
    bin_width: float = DEFAULT_HEATMAP_BIN_WIDTH,
    load_cutoff: float = DEFAULT_LOAD_CUTOFF,
    columns: int = DEFAULT_HEATMAP_COLUMNS,
    csv_path: str | Path | None = None,
) -> Path:
    """
    Batch Fitness Load Heatmap.

    Aggregate accepted-genome load per fitness interval across all runs.
    Each cell is one interval (default 0.01 wide, e.g. 0.00-0.01). Cell colour
    and label show how many accepted genomes fell in that interval across the batch.
    """
    _require_matplotlib()

    if not histories:
        raise ValueError("histories must contain at least one run")

    fitness_values = collect_accepted_fitness_values(histories)
    if not fitness_values:
        raise ValueError("no accepted fitness values found")

    labels, counts, centers = compute_fitness_bin_counts(
        fitness_values,
        bin_width=bin_width,
    )
    total = sum(counts)
    vmax = max(1, max(counts))

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        _write_batch_fitness_load_csv(
            labels,
            counts,
            centers,
            Path(csv_path),
            total=total,
        )

    row_count = int(np.ceil(len(counts) / columns))
    figure, axis = plt.subplots(
        1,
        1,
        figsize=(22, max(18.0, 1.45 * row_count + 4.5)),
        constrained_layout=True,
    )

    image = _draw_aggregate_heatmap_panel(
        axis,
        counts,
        labels,
        bin_width=bin_width,
        columns=columns,
        vmax=vmax,
        title=f"Batch Fitness Load Heatmap — fitness 0.00 to 1.00 (bands of {bin_width:.2f})",
    )
    colorbar = figure.colorbar(image, ax=axis, fraction=0.025, pad=0.02)
    colorbar.set_label("Accepted genomes")

    tail_total = sum(count for center, count in zip(centers, counts) if center >= load_cutoff)
    head_total = total - tail_total
    figure.suptitle(
        "Batch Fitness Load Heatmap\n"
        f"{len(histories)} runs, {total:,} accepted genomes  |  "
        f"0.00–{load_cutoff:.2f}: {head_total:,}  |  "
        f"{load_cutoff:.2f}–1.00: {tail_total:,}  |  "
        "white cells = 0 accepted genomes",
        fontsize=11,
    )

    figure.savefig(save_path, bbox_inches="tight", dpi=200)
    plt.close(figure)
    return save_path
