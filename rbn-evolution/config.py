"""Project configuration: defaults, contexts, and output paths."""

from dataclasses import dataclass
from pathlib import Path
import shutil

from rbn.types import Contexts, State

DEFAULT_N_GENES = 5

OUTPUT_DIR = Path("outputs")
RUNS_OUTPUT_DIR = OUTPUT_DIR / "runs"

FITNESS_HISTORY_PLOT = OUTPUT_DIR / "fitness_history.png"
INITIAL_LANDSCAPE_PLOT = OUTPUT_DIR / "initial_attractor_landscape.png"
FINAL_LANDSCAPE_PLOT = OUTPUT_DIR / "final_attractor_landscape.png"
EVOLUTION_JSON = OUTPUT_DIR / "evolution.json"
CONTEXT_FITNESS_CSV = OUTPUT_DIR / "context_fitness.csv"
CONTEXT_FITNESS_TXT = OUTPUT_DIR / "context_fitness.txt"
EVOLUTIONARY_LANDSCAPE_PLOT = OUTPUT_DIR / "evolutionary_landscape.png"
EVOLUTIONARY_LANDSCAPE_CSV = OUTPUT_DIR / "evolutionary_landscape.csv"
BATCH_FITNESS_LOAD_HEATMAP_PLOT = OUTPUT_DIR / "batch_fitness_load_heatmap.png"
BATCH_FITNESS_LOAD_HEATMAP_CSV = OUTPUT_DIR / "batch_fitness_load_heatmap.csv"
FACTOR_COMPLETION_LANDSCAPE_PLOT = OUTPUT_DIR / "factor_completion_landscape.png"
FACTOR_COMPLETION_LANDSCAPE_CSV = OUTPUT_DIR / "factor_completion_landscape.csv"

UNITY_STREAMING_ASSETS_DIR = Path(
    r"C:\2026\DissertationProject\unity\rbn-visualisation\Assets\StreamingAssets"
)
UNITY_TRAJECTORIES_JSON = UNITY_STREAMING_ASSETS_DIR / "unity_trajectories.json"

RUN_PLOTS = (
    FITNESS_HISTORY_PLOT,
    INITIAL_LANDSCAPE_PLOT,
    FINAL_LANDSCAPE_PLOT,
)

RUN_EXPORTS = RUN_PLOTS + (
    EVOLUTION_JSON,
    CONTEXT_FITNESS_CSV,
    CONTEXT_FITNESS_TXT,
    Path("factorised_fitness_trajectory.png"),
    Path("factorised_fitness_trajectory.csv"),
)

BATCH_EXPORTS = (
    EVOLUTIONARY_LANDSCAPE_PLOT,
    EVOLUTIONARY_LANDSCAPE_CSV,
    BATCH_FITNESS_LOAD_HEATMAP_PLOT,
    BATCH_FITNESS_LOAD_HEATMAP_CSV,
    FACTOR_COMPLETION_LANDSCAPE_PLOT,
    FACTOR_COMPLETION_LANDSCAPE_CSV,
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
    factorised_trajectory_plot: Path
    factorised_trajectory_csv: Path

    @property
    def export_paths(self) -> tuple[Path, ...]:
        return (
            self.fitness_history_plot,
            self.initial_landscape_plot,
            self.final_landscape_plot,
            self.evolution_json,
            self.context_fitness_csv,
            self.context_fitness_txt,
            self.factorised_trajectory_plot,
            self.factorised_trajectory_csv,
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
    return RunOutputPaths(
        run_dir=run_dir,
        fitness_history_plot=run_dir / "fitness_history.png",
        initial_landscape_plot=run_dir / "initial_attractor_landscape.png",
        final_landscape_plot=run_dir / "final_attractor_landscape.png",
        evolution_json=run_dir / "evolution.json",
        context_fitness_csv=run_dir / "context_fitness.csv",
        context_fitness_txt=run_dir / "context_fitness.txt",
        factorised_trajectory_plot=run_dir / "factorised_fitness_trajectory.png",
        factorised_trajectory_csv=run_dir / "factorised_fitness_trajectory.csv",
    )


def prepare_batch_outputs(runs: int) -> None:
    """Clear previous batch outputs before starting a multi-run or single-run batch."""
    if runs < 1:
        raise ValueError("runs must be at least 1")

    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    if runs == 1:
        for export_path in RUN_EXPORTS + BATCH_EXPORTS:
            if export_path.exists():
                export_path.unlink()
        return

    for child in OUTPUT_DIR.iterdir():
        if child.is_dir() and child.name == "runs":
            shutil.rmtree(child)
        elif child.is_dir() and child.name.startswith("run_"):
            shutil.rmtree(child)
        elif child.is_file() and child.name in {
            *(path.name for path in RUN_EXPORTS),
            *(path.name for path in BATCH_EXPORTS),
        }:
            child.unlink()

    RUNS_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)


def prepare_run_output_dir(paths: RunOutputPaths) -> Path:
    """Ensure the run output directory exists and remove stale files for this run."""
    paths.run_dir.mkdir(parents=True, exist_ok=True)
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


def state_count(n_genes: int) -> int:
    """Number of binary states in the state space."""
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")
    return 1 << n_genes


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


def generated_contexts(n_genes: int) -> Contexts:
    """
    Auto-generated alternating anterior/posterior contexts.

    Useful as a starting template when adding a new n to CONTEXTS_BY_N.
    """
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")

    posterior_initial: State = "0" * n_genes
    posterior_target: State = "".join(str(index % 2) for index in range(n_genes))
    anterior_initial: State = "1" + "0" * (n_genes - 1)
    anterior_target: State = "".join(str((index + 1) % 2) for index in range(n_genes))

    return {
        posterior_initial: posterior_target,
        anterior_initial: anterior_target,
    }


def prepare_output_dir() -> Path:
    """Ensure outputs/ exists and remove stale files from a single-run layout."""
    prepare_batch_outputs(runs=1)
    return OUTPUT_DIR

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
