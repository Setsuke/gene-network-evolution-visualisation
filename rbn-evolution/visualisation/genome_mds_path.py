"""MDS embedding of accepted genome paths from initial to final solution."""

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

from rbn.types import Contexts, EvolutionHistory, Genome
from visualisation.accepted_move_taxonomy import (
    classify_export_change_type,
    iter_accepted_change_records,
)
from visualisation.factorised.metrics import extract_accepted_trajectory_metrics

GENOME_EMBEDDING_DIMENSIONS = 3

INITIAL_COLOR = "#0284c7"
FINAL_COLOR = "#16a34a"
HIGHLIGHT_COLOR = "#dc2626"
PATH_ALPHA = 0.28
HIGHLIGHT_ALPHA = 0.95


@dataclass(frozen=True)
class AcceptedGenomePath:
    run_label: str
    genomes: tuple[Genome, ...]
    fitness_values: tuple[float, ...]
    change_types: tuple[str, ...]
    factor_scores: tuple[tuple[float, ...], ...]

    @property
    def final_fitness(self) -> float:
        return self.fitness_values[-1]


def _change_types_for_history(
    history: EvolutionHistory,
    contexts: Contexts,
    *,
    step_count: int,
) -> tuple[str, ...]:
    n_genes = len(history[0]["genome"])
    change_types: list[str] = ["initial"]
    for _, previous, current in iter_accepted_change_records(history):
        change_types.append(
            classify_export_change_type(
                previous,
                current,
                contexts,
                n_genes=n_genes,
            )
        )
    if len(change_types) != step_count:
        raise ValueError(
            "accepted trajectory length does not match classified move count "
            f"({len(change_types)} != {step_count})"
        )
    return tuple(change_types)


@dataclass(frozen=True)
class GenomePathEmbedding:
    run_label: str
    genome_indices: tuple[int, ...]
    final_fitness: float
    path_length: int


def _require_matplotlib() -> None:
    if plt is None or np is None:
        raise ImportError("matplotlib and numpy are required for genome MDS plots")


def extract_accepted_genome_paths(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
) -> list[AcceptedGenomePath]:
    paths: list[AcceptedGenomePath] = []
    for index, history in enumerate(histories):
        label = labels[index] if labels is not None else f"run_{index + 1:03d}"
        metrics = extract_accepted_trajectory_metrics(history, contexts)
        if not metrics:
            raise ValueError(f"run {label}: accepted trajectory is empty")

        change_types = _change_types_for_history(
            history,
            contexts,
            step_count=len(metrics),
        )
        paths.append(
            AcceptedGenomePath(
                run_label=label,
                genomes=tuple(metric.genome for metric in metrics),
                fitness_values=tuple(metric.total_fitness for metric in metrics),
                change_types=change_types,
                factor_scores=tuple(metric.factor_scores for metric in metrics),
            )
        )
    return paths


def accepted_genome_paths_as_tuples(
    paths: list[AcceptedGenomePath],
) -> list[tuple[str, list[Genome], list[float], list[str], float, list[list[float]]]]:
    return [
        (
            path.run_label,
            list(path.genomes),
            list(path.fitness_values),
            list(path.change_types),
            path.final_fitness,
            [list(scores) for scores in path.factor_scores],
        )
        for path in paths
    ]


def _collect_unique_genomes(paths: list[AcceptedGenomePath]) -> list[Genome]:
    seen: set[Genome] = set()
    unique: list[Genome] = []
    for path in paths:
        for genome in path.genomes:
            if genome not in seen:
                seen.add(genome)
                unique.append(genome)
    return unique


def genomes_to_bit_matrix(genomes: list[Genome]) -> np.ndarray:
    """Convert genomes to a 0/1 matrix for vectorised Hamming distance."""
    if not genomes:
        return np.zeros((0, 0), dtype=np.uint8)
    raw = np.frombuffer("".join(genomes).encode("ascii"), dtype=np.uint8)
    raw = raw.reshape(len(genomes), len(genomes[0]))
    return (raw == ord("1")).astype(np.uint8)


def _pairwise_hamming_matrix(genomes: list[Genome]) -> np.ndarray:
    bit_matrix = genomes_to_bit_matrix(genomes)
    count = bit_matrix.shape[0]
    if count == 0:
        return np.zeros((0, 0), dtype=float)

    matrix = np.zeros((count, count), dtype=np.float32)
    chunk_size = 128
    for start in range(0, count, chunk_size):
        end = min(start + chunk_size, count)
        differences = bit_matrix[start:end, None, :] != bit_matrix[None, :, :]
        matrix[start:end, :] = differences.sum(axis=2, dtype=np.float32)
    return matrix


def classical_mds(
    distance_matrix: np.ndarray,
    *,
    n_components: int = 2,
) -> np.ndarray:
    """Classical metric MDS via double-centred inner products."""
    count = distance_matrix.shape[0]
    if count == 0:
        return np.zeros((0, n_components), dtype=float)
    if count == 1:
        return np.zeros((1, n_components), dtype=float)
    if count == 2:
        distance = float(distance_matrix[0, 1])
        second = [0.0] * n_components
        second[0] = distance
        return np.array([second, [0.0] * n_components], dtype=float)

    squared = distance_matrix.astype(float) ** 2
    centering = np.eye(count) - np.ones((count, count)) / count
    inner_product = -0.5 * centering @ squared @ centering
    eigenvalues, eigenvectors = np.linalg.eigh(inner_product)

    order = np.argsort(eigenvalues)[::-1]
    eigenvalues = np.maximum(eigenvalues[order], 0.0)
    eigenvectors = eigenvectors[:, order]

    components = min(n_components, count)
    coordinates = eigenvectors[:, :components] * np.sqrt(eigenvalues[:components])
    if components < n_components:
        padding = np.zeros((count, n_components - components), dtype=float)
        coordinates = np.hstack([coordinates, padding])
    return coordinates


def _build_path_embeddings(
    paths: list[AcceptedGenomePath],
    genome_index: dict[Genome, int],
) -> list[GenomePathEmbedding]:
    embeddings: list[GenomePathEmbedding] = []
    for path in paths:
        indices = tuple(genome_index[genome] for genome in path.genomes)
        embeddings.append(
            GenomePathEmbedding(
                run_label=path.run_label,
                genome_indices=indices,
                final_fitness=path.final_fitness,
                path_length=len(path.genomes),
            )
        )
    return embeddings


def _axis_column_names(prefix: str, dimensions: int) -> tuple[str, ...]:
    axes = ("x", "y", "z")
    return tuple(f"{prefix}_{axes[index]}" for index in range(dimensions))


def _write_trajectory_csv(
    csv_path: Path,
    coordinates: np.ndarray,
    paths: list[AcceptedGenomePath],
    genome_index: dict[Genome, int],
    *,
    prefix: str = "mds",
) -> None:
    dimensions = coordinates.shape[1]
    axis_columns = _axis_column_names(prefix, dimensions)
    include_run_label = len(paths) > 1

    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        header = ["step", "genome_id", "fitness", *axis_columns, "change_type"]
        if include_run_label:
            header.insert(0, "run_label")
        writer.writerow(header)

        for path in paths:
            for step, (genome, fitness, change_type) in enumerate(
                zip(path.genomes, path.fitness_values, path.change_types)
            ):
                index = genome_index[genome]
                row = [
                    step,
                    f"g{index}",
                    f"{fitness:.6g}",
                    *[
                        f"{coordinates[index, axis]:.6g}"
                        for axis in range(dimensions)
                    ],
                    change_type,
                ]
                if include_run_label:
                    row.insert(0, path.run_label)
                writer.writerow(row)


def plot_genome_path_embedding(
    coordinates: np.ndarray,
    path_embeddings: list[GenomePathEmbedding],
    unique_genome_count: int,
    save_path: Path,
    *,
    x_label: str,
    y_label: str,
    title: str,
    caption: str,
    highlight_best_run: bool = True,
) -> None:
    figure, axis = plt.subplots(figsize=(11.5, 8.8))
    run_count = len(path_embeddings)
    cmap = plt.cm.viridis

    highlight_index = None
    if highlight_best_run and path_embeddings:
        highlight_index = max(
            range(run_count),
            key=lambda index: path_embeddings[index].final_fitness,
        )

    for index, embedding in enumerate(path_embeddings):
        path_coords = coordinates[list(embedding.genome_indices), :]
        color = cmap(index / max(run_count - 1, 1))
        linewidth = 1.0
        alpha = PATH_ALPHA
        zorder = 2
        if highlight_index is not None and index == highlight_index:
            linewidth = 2.6
            alpha = HIGHLIGHT_ALPHA
            color = HIGHLIGHT_COLOR
            zorder = 4

        axis.plot(
            path_coords[:, 0],
            path_coords[:, 1],
            color=color,
            linewidth=linewidth,
            alpha=alpha,
            zorder=zorder,
        )
        axis.scatter(
            path_coords[0, 0],
            path_coords[0, 1],
            s=34 if index != highlight_index else 52,
            color=INITIAL_COLOR,
            edgecolors=HIGHLIGHT_COLOR if index == highlight_index else "white",
            linewidths=1.2 if index == highlight_index else 0.5,
            zorder=zorder + 1,
        )
        axis.scatter(
            path_coords[-1, 0],
            path_coords[-1, 1],
            s=46 if index != highlight_index else 64,
            color=FINAL_COLOR,
            marker="s",
            edgecolors=HIGHLIGHT_COLOR if index == highlight_index else "white",
            linewidths=1.2 if index == highlight_index else 0.5,
            zorder=zorder + 1,
        )

    if run_count == 1:
        path_coords = coordinates[list(path_embeddings[0].genome_indices), :]
        for step_index, (x_value, y_value) in enumerate(path_coords):
            if step_index in (0, len(path_coords) - 1):
                continue
            axis.text(
                x_value,
                y_value,
                str(step_index),
                fontsize=8,
                color="#475569",
                ha="center",
                va="bottom",
            )

    axis.scatter([], [], s=40, color=INITIAL_COLOR, label="Initial genome")
    axis.scatter([], [], s=48, color=FINAL_COLOR, marker="s", label="Final solution")
    if highlight_index is not None and run_count > 1:
        best = path_embeddings[highlight_index]
        axis.plot(
            [],
            [],
            color=HIGHLIGHT_COLOR,
            linewidth=2.6,
            label=f"Highest fitness ({best.run_label})",
        )
    axis.set_xlabel(x_label)
    axis.set_ylabel(y_label)
    axis.set_title(title)
    axis.grid(alpha=0.25)
    axis.legend(loc="best", fontsize=9, frameon=True)

    figure.text(
        0.5,
        0.02,
        caption,
        ha="center",
        va="bottom",
        fontsize=9.5,
        color="#334155",
    )
    figure.subplots_adjust(bottom=0.12)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.35)
    plt.close(figure)


def export_evolutionary_genome_mds_path(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path | None = None,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
    unity_json_paths: str | Path | list[str | Path] | None = None,
    highlight_best_run: bool = True,
    n_components: int = GENOME_EMBEDDING_DIMENSIONS,
    shared_initial_genome: bool = False,
    shared_initial_genome_value: Genome | None = None,
) -> Path | None:
    """
    Embed accepted genomes with classical MDS on Hamming distance and export
    trajectory data for CSV / Unity. When ``save_path`` is set, also write a
    2D PNG preview (used for the shared-initial-genome Unity view).
    """
    if not histories:
        raise ValueError("histories must contain at least one evolution run")
    if n_components < 2:
        raise ValueError("n_components must be at least 2")

    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    unique_genomes = _collect_unique_genomes(paths)
    if len(unique_genomes) < 2:
        raise ValueError("Need at least two distinct accepted genomes for MDS embedding")

    genome_index = {genome: index for index, genome in enumerate(unique_genomes)}
    print(
        f"  computing MDS on {len(unique_genomes)} unique genomes "
        f"({n_components}D)...",
        flush=True,
    )
    distance_matrix = _pairwise_hamming_matrix(unique_genomes)
    coordinates = classical_mds(distance_matrix, n_components=n_components)
    path_embeddings = _build_path_embeddings(paths, genome_index)

    if csv_path is not None:
        _write_trajectory_csv(
            Path(csv_path),
            coordinates,
            paths,
            genome_index,
            prefix="mds",
        )

    if unity_json_paths is not None:
        from exports.unity_genome_embeddings import (
            build_unity_genome_embedding_payload,
            export_unity_genome_embedding_json,
        )

        payload = build_unity_genome_embedding_payload(
            method="mds",
            coordinates=coordinates,
            paths=accepted_genome_paths_as_tuples(paths),
            dimensions=n_components,
            gene_count=len(next(iter(contexts.values()))),
            context_count=len(contexts),
            contexts=contexts,
            shared_initial_genome=shared_initial_genome,
            initial_genome_index=(
                genome_index[shared_initial_genome_value]
                if shared_initial_genome and shared_initial_genome_value is not None
                else None
            ),
        )
        export_unity_genome_embedding_json(
            payload,
            unity_json_paths,
            unique_genomes=unique_genomes if shared_initial_genome else None,
        )

    if save_path is None:
        return None

    _require_matplotlib()
    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    plot_coordinates = coordinates[:, :2]
    total_steps = sum(embedding.path_length for embedding in path_embeddings)
    plot_genome_path_embedding(
        plot_coordinates,
        path_embeddings,
        len(unique_genomes),
        save_path,
        x_label="MDS dimension 1  (Hamming distance)",
        y_label="MDS dimension 2  (Hamming distance)",
        title="Evolutionary genome path (MDS embedding of accepted genomes)",
        caption=(
            f"Classical MDS ({n_components}D) on pairwise Hamming distance across "
            f"{len(unique_genomes)} unique accepted genomes from "
            f"{len(path_embeddings)} run(s). Preview shows dimensions 1–2; "
            f"dimension 3 is exported for Unity. Each polyline connects the "
            f"initial generated genome to the final accepted solution "
            f"({total_steps} accepted steps total)."
        ),
        highlight_best_run=highlight_best_run,
    )
    return save_path
