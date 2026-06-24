"""Project configuration: defaults, contexts, and output paths."""

from pathlib import Path

from rbn.types import Contexts, State

DEFAULT_N_GENES = 5

OUTPUT_DIR = Path("outputs")

FITNESS_HISTORY_PLOT = OUTPUT_DIR / "fitness_history.png"
INITIAL_LANDSCAPE_PLOT = OUTPUT_DIR / "initial_attractor_landscape.png"
FINAL_LANDSCAPE_PLOT = OUTPUT_DIR / "final_attractor_landscape.png"
EVOLUTION_JSON = OUTPUT_DIR / "evolution.json"
CONTEXT_FITNESS_CSV = OUTPUT_DIR / "context_fitness.csv"
CONTEXT_FITNESS_TXT = OUTPUT_DIR / "context_fitness.txt"

RUN_PLOTS = (
    FITNESS_HISTORY_PLOT,
    INITIAL_LANDSCAPE_PLOT,
    FINAL_LANDSCAPE_PLOT,
)

RUN_EXPORTS = RUN_PLOTS + (
    EVOLUTION_JSON,
    CONTEXT_FITNESS_CSV,
    CONTEXT_FITNESS_TXT,
)

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
    """Ensure outputs/ exists and remove stale plots from the previous run."""
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

    for plot_path in RUN_EXPORTS:
        if plot_path.exists():
            plot_path.unlink()

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
