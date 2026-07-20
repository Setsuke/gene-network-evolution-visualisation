"""UMAP embedding of accepted genome paths from initial to final solution."""

from __future__ import annotations

from pathlib import Path

try:
    import umap
except ImportError:
    umap = None

from rbn.types import Contexts, EvolutionHistory, Genome
from visualisation.genome_mds_path import (
    GENOME_EMBEDDING_DIMENSIONS,
    _build_path_embeddings,
    _collect_unique_genomes,
    _require_matplotlib,
    _write_trajectory_csv,
    accepted_genome_paths_as_tuples,
    extract_accepted_genome_paths,
    genomes_to_bit_matrix,
    plot_genome_path_embedding,
)

UMAP_RANDOM_STATE = 42


def _require_umap() -> None:
    if umap is None:
        raise ImportError(
            "umap-learn is required for genome UMAP plots. Install with: pip install umap-learn"
        )


def _umap_epochs(genome_count: int) -> int:
    if genome_count <= 300:
        return 200
    if genome_count <= 1500:
        return 100
    return 50


def umap_embed_genomes(
    genomes: list[Genome],
    *,
    n_components: int = GENOME_EMBEDDING_DIMENSIONS,
    n_neighbors: int | None = None,
    min_dist: float = 0.1,
    random_state: int = UMAP_RANDOM_STATE,
):
    _require_umap()
    count = len(genomes)
    if count < 2:
        raise ValueError("Need at least two genomes for UMAP embedding")
    if n_components < 2:
        raise ValueError("n_components must be at least 2")

    if n_neighbors is None:
        n_neighbors = min(15, max(2, count - 1))

    bit_matrix = genomes_to_bit_matrix(genomes)
    reducer = umap.UMAP(
        metric="hamming",
        n_components=n_components,
        n_neighbors=n_neighbors,
        min_dist=min_dist,
        random_state=random_state,
        n_epochs=_umap_epochs(count),
        n_jobs=1,
        low_memory=True,
    )
    return reducer.fit_transform(bit_matrix)


def plot_evolutionary_genome_umap_path(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
    unity_json_paths: str | Path | list[str | Path] | None = None,
    highlight_best_run: bool = True,
    n_components: int = GENOME_EMBEDDING_DIMENSIONS,
    n_neighbors: int | None = None,
    min_dist: float = 0.1,
) -> Path:
    """
    Embed accepted genomes with UMAP on Hamming distance and draw each run's
    path from the initial genome to the final solution.

    Computes ``n_components`` coordinates (default 3: x, y, z) for CSV / Unity
    export; the PNG preview uses the first two dimensions only.
    """
    _require_matplotlib()
    _require_umap()

    if not histories:
        raise ValueError("histories must contain at least one evolution run")

    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    unique_genomes = _collect_unique_genomes(paths)
    if len(unique_genomes) < 2:
        raise ValueError("Need at least two distinct accepted genomes for UMAP embedding")

    genome_index = {genome: index for index, genome in enumerate(unique_genomes)}
    print(
        f"  computing UMAP on {len(unique_genomes)} unique genomes "
        f"({n_components}D)...",
        flush=True,
    )
    coordinates = umap_embed_genomes(
        unique_genomes,
        n_components=n_components,
        n_neighbors=n_neighbors,
        min_dist=min_dist,
    )
    path_embeddings = _build_path_embeddings(paths, genome_index)

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    if csv_path is not None:
        _write_trajectory_csv(
            Path(csv_path),
            coordinates,
            paths,
            genome_index,
            prefix="umap",
        )

    if unity_json_paths is not None:
        from exports.unity_genome_embeddings import (
            build_unity_genome_embedding_payload,
            export_unity_genome_embedding_json,
        )

        payload = build_unity_genome_embedding_payload(
            method="umap",
            coordinates=coordinates,
            paths=accepted_genome_paths_as_tuples(paths),
            dimensions=n_components,
            gene_count=len(next(iter(contexts.values()))),
            context_count=len(contexts),
            contexts=contexts,
        )
        export_unity_genome_embedding_json(payload, unity_json_paths)

    plot_coordinates = coordinates[:, :2]
    total_steps = sum(embedding.path_length for embedding in path_embeddings)
    effective_neighbors = n_neighbors
    if effective_neighbors is None:
        effective_neighbors = min(15, max(2, len(unique_genomes) - 1))

    plot_genome_path_embedding(
        plot_coordinates,
        path_embeddings,
        len(unique_genomes),
        save_path,
        x_label="UMAP dimension 1",
        y_label="UMAP dimension 2",
        title="Evolutionary genome path (UMAP embedding of accepted genomes)",
        caption=(
            f"UMAP ({n_components}D) on Hamming distance across {len(unique_genomes)} unique "
            f"accepted genomes from {len(path_embeddings)} run(s) "
            f"(n_neighbors={effective_neighbors}, min_dist={min_dist}, "
            f"n_epochs={_umap_epochs(len(unique_genomes))}). Preview shows dimensions 1–2; "
            f"dimension 3 is exported for Unity. Each polyline connects "
            f"the initial generated genome to the final accepted solution "
            f"({total_steps} accepted steps total)."
        ),
        highlight_best_run=highlight_best_run,
    )
    return save_path
