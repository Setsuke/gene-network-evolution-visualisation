"""Project configuration: defaults, contexts, and output paths."""

from dataclasses import dataclass
from pathlib import Path
import os
import shutil

from rbn.types import Contexts, State

PROJECT_DIR = Path(__file__).resolve().parent

DEFAULT_N_GENES = 5

OUTPUT_DIR = Path("outputs")
DATA_OUTPUT_DIR = OUTPUT_DIR / "data"
RUNS_OUTPUT_DIR = OUTPUT_DIR / "runs"
RUN_DATA_SUBDIR = "data"

EVOLUTIONARY_GENOME_MDS_PATH_CSV = DATA_OUTPUT_DIR / "evolutionary_genome_mds_path.csv"
GENOME_MDS_UNITY_JSON = DATA_OUTPUT_DIR / "unity_genome_mds_path.json"
SHARED_INITIAL_GENOME_MDS_PLOT = OUTPUT_DIR / "shared_initial_genome_mds_path.png"
SHARED_INITIAL_GENOME_MDS_CSV = DATA_OUTPUT_DIR / "shared_initial_genome_mds_path.csv"
GENOME_SHARED_MDS_UNITY_JSON = DATA_OUTPUT_DIR / "unity_shared_genome_mds_path.json"
BATCH_GENOME_MDS_PATH_CSV = DATA_OUTPUT_DIR / "batch_genome_mds_path.csv"
GENOME_BATCH_MDS_UNITY_JSON = DATA_OUTPUT_DIR / "unity_batch_genome_mds_path.json"
NEUTRAL_MOVE_SUMMARY_TABLE = OUTPUT_DIR / "neutral_move_summary_table.png"
NEUTRAL_MOVE_SUMMARY_CSV = DATA_OUTPUT_DIR / "neutral_move_summary.csv"
NEUTRAL_MOVE_BAR_CHART_PLOT = OUTPUT_DIR / "neutral_move_bar_chart.png"
NON_NEUTRAL_MOVE_SUMMARY_TABLE = OUTPUT_DIR / "non_neutral_move_summary_table.png"
NON_NEUTRAL_MOVE_SUMMARY_CSV = DATA_OUTPUT_DIR / "non_neutral_move_summary.csv"
FACTOR_CHANGING_NEUTRAL_SUMMARY_TABLE = OUTPUT_DIR / "factor_changing_neutral_moves_summary_table.png"
FACTOR_CHANGING_NEUTRAL_SUMMARY_CSV = DATA_OUTPUT_DIR / "factor_changing_neutral_moves_summary.csv"
PLATEAU_DRIFT_CLASSIFICATION_TABLE = OUTPUT_DIR / "plateau_drift_classification_table.png"
PLATEAU_DRIFT_CLASSIFICATION_CSV = DATA_OUTPUT_DIR / "plateau_drift_classification.csv"
PLATEAU_DRIFT_EXIT_TYPES_TABLE = OUTPUT_DIR / "plateau_drift_exit_types_table.png"
PLATEAU_DRIFT_EXIT_TYPES_CSV = DATA_OUTPUT_DIR / "plateau_drift_exit_types.csv"
ACCEPTED_MOVES_HIERARCHY_FLOW = (
    OUTPUT_DIR / "Classification of Accepted Evolutionary Moves.png"
)
ACCEPTED_MOVES_FACTOR_NEUTRAL_FLOW = (
    OUTPUT_DIR / "accepted_moves_factor_neutral_flow.png"
)
ACCEPTED_MOVES_FACTOR_CHANGING_NEUTRAL_FLOW = (
    OUTPUT_DIR / "accepted_moves_factor_changing_neutral_flow.png"
)
ACCEPTED_MOVES_FITNESS_IMPROVING_FLOW = (
    OUTPUT_DIR / "accepted_moves_fitness_improving_flow.png"
)
ACCEPTED_MUTATION_CANDIDATES_BY_MOVE_TYPE_PLOT = (
    OUTPUT_DIR / "accepted_mutation_candidates_by_move_type.png"
)
ACCEPTED_MOVES_HIERARCHY_CSV = DATA_OUTPUT_DIR / "accepted_moves_hierarchy.csv"
ACCEPTED_MOVES_HIERARCHY_TXT = DATA_OUTPUT_DIR / "accepted_moves_hierarchy_breakdown.txt"
EVOLUTION_PROGRESS_BREAKDOWN_PLOT = OUTPUT_DIR / "evolution_progress_breakdown.png"
EVOLUTION_PROGRESS_BREAKDOWN_CSV = DATA_OUTPUT_DIR / "evolution_progress_breakdown.csv"
ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT = (
    OUTPUT_DIR / "accepted_genomes_by_progress_quartiles.png"
)
ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV = (
    DATA_OUTPUT_DIR / "accepted_genomes_by_progress_quartiles.csv"
)
SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT = (
    OUTPUT_DIR / "shared_accepted_genomes_by_progress_quartiles.png"
)
SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV = (
    DATA_OUTPUT_DIR / "shared_accepted_genomes_by_progress_quartiles.csv"
)
PROGRESS_ATTRACTOR_COUNTS_PLOT = OUTPUT_DIR / "progress_attractor_counts.png"
PROGRESS_ATTRACTOR_COUNTS_CSV = DATA_OUTPUT_DIR / "progress_attractor_counts.csv"
DRIFTING_CATEGORIES_NORMALISED_PROGRESS_PLOT = (
    OUTPUT_DIR / "drifting_categories_normalised_progress.png"
)
DRIFTING_CATEGORIES_NORMALISED_PROGRESS_CSV = (
    DATA_OUTPUT_DIR / "drifting_categories_normalised_progress.csv"
)
DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_PLOT = (
    OUTPUT_DIR / "drifting_categories_with_improvement_normalised_progress.png"
)
DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_CSV = (
    DATA_OUTPUT_DIR / "drifting_categories_with_improvement_normalised_progress.csv"
)
DRIFT_EPISODE_LENGTH_HISTOGRAM_PLOT = OUTPUT_DIR / "drift_episode_length_histogram.png"
DRIFT_EPISODE_GENOTYPE_EXPLORATION_EPISODES_CSV = (
    DATA_OUTPUT_DIR / "drift_episode_genotype_exploration_episodes.csv"
)
DRIFT_EPISODE_GENOTYPE_EXPLORATION_QUARTILES_CSV = (
    DATA_OUTPUT_DIR / "drift_episode_genotype_exploration_quartiles.csv"
)
DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_QUARTILE_MAX_HAMMING_PLOT = (
    OUTPUT_DIR / "drift_exploration_length_quartile_max_hamming.png"
)
DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_MEDIAN_START_END_HAMMING_PLOT = (
    OUTPUT_DIR / "drift_episode_length_median_start_end_hamming.png"
)
ACCEPTED_MOVE_TRANSITIONS_CSV = DATA_OUTPUT_DIR / "accepted_move_transitions.csv"
ACCEPTED_MOVE_TRANSITION_COUNTS_CSV = DATA_OUTPUT_DIR / "accepted_move_transition_counts.csv"
ACCEPTED_MOVE_TRANSITION_MATRIX_CSV = DATA_OUTPUT_DIR / "accepted_move_transition_matrix.csv"
PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT = OUTPUT_DIR / "pre_solution_fitness_distribution.png"
PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV = DATA_OUTPUT_DIR / "pre_solution_fitness_distribution.csv"
MOST_DRIFTING_FITNESS_TABLE_PLOT = OUTPUT_DIR / "most_drifting_fitness_table.png"
MOST_DRIFTING_FITNESS_TABLE_CSV = DATA_OUTPUT_DIR / "most_drifting_fitness_table.csv"

BOOLEAN_STATE_MDS_LAYOUT_JSON = OUTPUT_DIR / "boolean_state_mds_layout.json"
BOOLEAN_STATE_MDS_LAYOUT_CSV = DATA_OUTPUT_DIR / "boolean_state_mds_layout.csv"
UNITY_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON = (
    OUTPUT_DIR / "unity_boolean_attractor_landscape.json"
)


def _unity_streaming_assets_candidates() -> tuple[Path, ...]:
    """Known layouts relative to this package (rbn-evolution/)."""
    dissertation_root = PROJECT_DIR.parent.parent.parent
    return (
        dissertation_root / "unity" / "rbn-visualisation" / "Assets" / "StreamingAssets",
        PROJECT_DIR.parent.parent / "unity" / "rbn-visualisation" / "Assets" / "StreamingAssets",
        PROJECT_DIR / "unity" / "rbn-visualisation" / "Assets" / "StreamingAssets",
    )


def resolve_unity_streaming_assets_dir(
    explicit: Path | str | None = None,
) -> Path:
    """
    Resolve Unity StreamingAssets for JSON export.

    Priority:
    1. explicit path (CLI --unity-streaming-assets)
    2. env UNITY_STREAMING_ASSETS or RBN_UNITY_STREAMING_ASSETS
    3. first existing directory among known relative layouts
    4. default candidate path (may not exist yet)
    """
    if explicit is not None and str(explicit).strip():
        return Path(explicit).expanduser().resolve()

    for env_name in ("UNITY_STREAMING_ASSETS", "RBN_UNITY_STREAMING_ASSETS"):
        env_value = os.environ.get(env_name, "").strip()
        if env_value:
            return Path(env_value).expanduser().resolve()

    for candidate in _unity_streaming_assets_candidates():
        if candidate.is_dir():
            return candidate.resolve()

    return _unity_streaming_assets_candidates()[0].resolve()


def _assign_unity_streaming_assets_paths(streaming_assets_dir: Path) -> None:
    global UNITY_STREAMING_ASSETS_DIR
    global UNITY_GENOME_MDS_JSON
    global UNITY_SHARED_GENOME_MDS_JSON
    global UNITY_BATCH_GENOME_MDS_JSON
    global UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON
    global UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON

    UNITY_STREAMING_ASSETS_DIR = streaming_assets_dir
    UNITY_GENOME_MDS_JSON = streaming_assets_dir / "unity_genome_mds_path.json"
    UNITY_SHARED_GENOME_MDS_JSON = (
        streaming_assets_dir / "unity_shared_genome_mds_path.json"
    )
    UNITY_BATCH_GENOME_MDS_JSON = (
        streaming_assets_dir / "unity_batch_genome_mds_path.json"
    )
    UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON = (
        streaming_assets_dir / "boolean_state_mds_layout.json"
    )
    UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON = (
        streaming_assets_dir / "unity_boolean_attractor_landscape.json"
    )


def configure_unity_streaming_assets(
    explicit: Path | str | None = None,
) -> Path:
    """Set Unity export paths; call before run_demo when using CLI override."""
    resolved = resolve_unity_streaming_assets_dir(explicit)
    _assign_unity_streaming_assets_paths(resolved)
    return resolved


_assign_unity_streaming_assets_paths(resolve_unity_streaming_assets_dir())

# Unity shared-initial-genome MDS: cap runs to keep embedding tractable.
UNITY_SHARED_EMBEDDING_MAX_RUNS = 200
# Unity Option 9 (distinct initial genomes, full paths): same cap for MDS tractability.
UNITY_BATCH_MDS_PATH_MAX_RUNS = 200


@dataclass(frozen=True)
class RunOutputPaths:
    run_dir: Path
    fitness_history_plot: Path
    initial_landscape_plot: Path
    final_landscape_plot: Path
    evolution_json: Path
    context_fitness_csv: Path
    context_fitness_txt: Path
    accepted_fitness_plot: Path
    factorised_trajectory_plot: Path
    factorised_trajectory_csv: Path
    factor_delta_plot: Path

    @property
    def export_paths(self) -> tuple[Path, ...]:
        return (
            self.fitness_history_plot,
            self.initial_landscape_plot,
            self.final_landscape_plot,
            self.evolution_json,
            self.context_fitness_csv,
            self.context_fitness_txt,
            self.accepted_fitness_plot,
            self.factorised_trajectory_plot,
            self.factorised_trajectory_csv,
            self.factor_delta_plot,
        )


def get_run_output_paths(run_index: int, runs: int) -> RunOutputPaths:
    """Return output paths for a single run (flat outputs/ when runs=1)."""
    if run_index < 1 or run_index > runs:
        raise ValueError(f"run_index must be between 1 and {runs}, got {run_index}")
    if runs < 1:
        raise ValueError("runs must be at least 1")

    run_dir = (
        OUTPUT_DIR
        if runs == 1
        else RUNS_OUTPUT_DIR / f"run_{run_index:03d}"
    )
    data_dir = run_dir / RUN_DATA_SUBDIR
    return RunOutputPaths(
        run_dir=run_dir,
        fitness_history_plot=run_dir / "fitness_history.png",
        initial_landscape_plot=run_dir / "initial_attractor_landscape.png",
        final_landscape_plot=run_dir / "final_attractor_landscape.png",
        evolution_json=run_dir / "evolution.json",
        context_fitness_csv=data_dir / "context_fitness.csv",
        context_fitness_txt=data_dir / "context_fitness.txt",
        accepted_fitness_plot=run_dir / "accepted_fitness_trajectory.png",
        factorised_trajectory_plot=run_dir / "factorised_fitness_trajectory.png",
        factorised_trajectory_csv=data_dir / "factorised_fitness_trajectory.csv",
        factor_delta_plot=run_dir / "factor_delta_heatmap.png",
    )


def prepare_batch_outputs(runs: int) -> None:
    """Remove all previous outputs before starting a new run or batch."""
    if runs < 1:
        raise ValueError("runs must be at least 1")

    if OUTPUT_DIR.exists():
        for child in OUTPUT_DIR.iterdir():
            if child.is_dir():
                shutil.rmtree(child)
            else:
                child.unlink()
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    DATA_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    if runs > 1:
        RUNS_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def prepare_run_output_dir(paths: RunOutputPaths) -> Path:
    """Ensure the run output directory exists and remove stale files for this run."""
    paths.run_dir.mkdir(parents=True, exist_ok=True)
    paths.context_fitness_csv.parent.mkdir(parents=True, exist_ok=True)
    for export_path in paths.export_paths:
        if export_path.exists():
            export_path.unlink()
    return paths.run_dir
# Fitness contexts by n
# Each entry: initial_state -> target_state

CONTEXTS_BY_N: dict[int, Contexts] = {
    3: {
        "000": "010",
        "100": "101",
    },
    4: {
        "0000": "0101",
        "1000": "1010",
    },
    5: {
        "00000": "01010",
        "10000": "10101",
        #"10001": "01110",
    },
    6: {
        "000000": "010101",
        "100000": "101010",
    },
    7: {
        "0000000": "0101010",
        "1000000": "1010101",
    },
    8: {
        "00000000": "01010101",
        "10000000": "10101010",
    },
}


def genome_length(n_genes: int) -> int:
    """Number of bits in a genome for an n-gene network."""
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")
    return (1 << n_genes) * n_genes


def get_contexts(n_genes: int) -> Contexts:
    """Return fitness contexts configured for n_genes."""
    if n_genes not in CONTEXTS_BY_N:
        available = ", ".join(str(n) for n in sorted(CONTEXTS_BY_N))
        raise ValueError(
            f"No contexts defined for n={n_genes}. "
            f"Add an entry to CONTEXTS_BY_N in config.py. "
            f"Currently defined: {available or 'none'}"
        )

    contexts = CONTEXTS_BY_N[n_genes]
    _validate_contexts(contexts, n_genes)
    return dict(contexts)


def _validate_contexts(contexts: Contexts, n_genes: int) -> None:
    for initial, target in contexts.items():
        if len(initial) != n_genes:
            raise ValueError(
                f"Context initial state '{initial}' has length {len(initial)}, "
                f"expected {n_genes} for n={n_genes}"
            )
        if len(target) != n_genes:
            raise ValueError(
                f"Context target state '{target}' has length {len(target)}, "
                f"expected {n_genes} for n={n_genes}"
            )
        if any(bit not in "01" for bit in initial + target):
            raise ValueError(
                f"Context ({initial!r} -> {target!r}) must contain only 0 and 1"
            )
