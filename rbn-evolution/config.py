"""Project configuration: defaults, contexts, and output paths."""

from dataclasses import dataclass
from pathlib import Path
import shutil

from rbn.types import Contexts, State

DEFAULT_N_GENES = 5

OUTPUT_DIR = Path("outputs")
DATA_OUTPUT_DIR = OUTPUT_DIR / "data"
RUNS_OUTPUT_DIR = OUTPUT_DIR / "runs"
RUN_DATA_SUBDIR = "data"

EVOLUTIONARY_GENOME_MDS_PATH_PLOT = OUTPUT_DIR / "evolutionary_genome_mds_path.png"
EVOLUTIONARY_GENOME_MDS_PATH_CSV = DATA_OUTPUT_DIR / "evolutionary_genome_mds_path.csv"
EVOLUTIONARY_GENOME_UMAP_PATH_PLOT = OUTPUT_DIR / "evolutionary_genome_umap_path.png"
EVOLUTIONARY_GENOME_UMAP_PATH_CSV = DATA_OUTPUT_DIR / "evolutionary_genome_umap_path.csv"
GENOME_MDS_UNITY_JSON = DATA_OUTPUT_DIR / "unity_genome_mds_path.json"
GENOME_UMAP_UNITY_JSON = DATA_OUTPUT_DIR / "unity_genome_umap_path.json"
BATCH_FITNESS_LOAD_HEATMAP_PLOT = OUTPUT_DIR / "batch_fitness_load_heatmap.png"
BATCH_FITNESS_LOAD_HEATMAP_CSV = DATA_OUTPUT_DIR / "batch_fitness_load_heatmap.csv"
NEUTRAL_MOVE_SUMMARY_TABLE = OUTPUT_DIR / "neutral_move_summary_table.png"
NEUTRAL_MOVE_SUMMARY_CSV = DATA_OUTPUT_DIR / "neutral_move_summary.csv"
NEUTRAL_MOVE_BAR_CHART_PLOT = OUTPUT_DIR / "neutral_move_bar_chart.png"
NON_NEUTRAL_MOVE_SUMMARY_TABLE = OUTPUT_DIR / "non_neutral_move_summary_table.png"
NON_NEUTRAL_MOVE_SUMMARY_CSV = DATA_OUTPUT_DIR / "non_neutral_move_summary.csv"
FACTOR_CHANGING_NEUTRAL_SUMMARY_TABLE = OUTPUT_DIR / "factor_changing_neutral_moves_summary_table.png"
FACTOR_CHANGING_NEUTRAL_SUMMARY_CSV = DATA_OUTPUT_DIR / "factor_changing_neutral_moves_summary.csv"
ACCEPTED_MOVES_HIERARCHY_FLOW = (
    OUTPUT_DIR / "Classification of Accepted Evolutionary Moves.png"
)
ACCEPTED_MUTATION_CANDIDATES_BY_MOVE_TYPE_PLOT = (
    OUTPUT_DIR / "accepted_mutation_candidates_by_move_type.png"
)
ACCEPTED_MOVES_HIERARCHY_CSV = DATA_OUTPUT_DIR / "accepted_moves_hierarchy.csv"
ACCEPTED_MOVES_HIERARCHY_TXT = DATA_OUTPUT_DIR / "accepted_moves_hierarchy_breakdown.txt"
EVOLUTION_PROGRESS_BREAKDOWN_PLOT = OUTPUT_DIR / "evolution_progress_breakdown.png"
EVOLUTION_PROGRESS_BREAKDOWN_CSV = DATA_OUTPUT_DIR / "evolution_progress_breakdown.csv"
ACCEPTED_MOVE_TRANSITIONS_CSV = DATA_OUTPUT_DIR / "accepted_move_transitions.csv"
ACCEPTED_MOVE_TRANSITION_COUNTS_CSV = DATA_OUTPUT_DIR / "accepted_move_transition_counts.csv"
ACCEPTED_MOVE_TRANSITION_MATRIX_CSV = DATA_OUTPUT_DIR / "accepted_move_transition_matrix.csv"
ACCEPTED_MOVE_TRANSITION_HEATMAP = OUTPUT_DIR / "accepted_move_transition_heatmap.png"
PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT = OUTPUT_DIR / "pre_solution_fitness_distribution.png"
PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV = DATA_OUTPUT_DIR / "pre_solution_fitness_distribution.csv"

UNITY_STREAMING_ASSETS_DIR = Path(
    r"C:\2026\DissertationProject\unity\rbn-visualisation\Assets\StreamingAssets"
)
UNITY_TRAJECTORIES_JSON = UNITY_STREAMING_ASSETS_DIR / "unity_trajectories.json"
UNITY_GENOME_MDS_JSON = UNITY_STREAMING_ASSETS_DIR / "unity_genome_mds_path.json"
UNITY_GENOME_UMAP_JSON = UNITY_STREAMING_ASSETS_DIR / "unity_genome_umap_path.json"
BOOLEAN_STATE_MDS_LAYOUT_JSON = OUTPUT_DIR / "boolean_state_mds_layout.json"
BOOLEAN_STATE_MDS_LAYOUT_CSV = DATA_OUTPUT_DIR / "boolean_state_mds_layout.csv"
UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON = (
    UNITY_STREAMING_ASSETS_DIR / "boolean_state_mds_layout.json"
)
UNITY_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON = (
    OUTPUT_DIR / "unity_boolean_attractor_landscape.json"
)
UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON = (
    UNITY_STREAMING_ASSETS_DIR / "unity_boolean_attractor_landscape.json"
)


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
