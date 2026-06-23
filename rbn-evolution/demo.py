"""
rbn-evolution demo: end-to-end pipeline from random network to evolved solution.

Pipeline (see run_demo):
    config/contexts  ->  truth table  ->  transition table  ->  attractors
    ->  fitness + evolution  ->  plots
"""

from config import (
    DEFAULT_N_GENES,
    EVOLUTION_JSON,
    FINAL_LANDSCAPE_PLOT,
    FITNESS_HISTORY_PLOT,
    INITIAL_LANDSCAPE_PLOT,
    OUTPUT_DIR,
    genome_length,
    get_contexts,
    prepare_output_dir,
)
from export_json import export_evolution_json
from rbn.attractors import find_attractors, print_attractors
from rbn.evolution import evolve_genome
from rbn.transition_table import build_transition_table_from_genome
from rbn.truth_table import generate_random_truth_table, print_truth_table, truth_table_to_genome
from visualisation.plots_2d import plot_attractor_landscape, plot_fitness_history


def run_demo(
    n_genes: int = DEFAULT_N_GENES,
    mutation_probability: float = 0.1,
    generations: int = 1000,
    show_plots: bool = False,
    verbose: bool = True,
) -> None:
    """
    Run the full rbn-evolution experiment.

    Pipeline:
        0. Config — load contexts (initial -> target) for this n from config.py
        1. Truth table — random boolean rules; encoded as an n * 2^n-bit genome
        2. Transition table — synchronous dynamics via the rotated-input rule
        3. Attractors — point attractors and limit cycles of the initial network
        4. Evolution — mutate genome; accept if fitness >= previous (contexts in config)
        5. Plots — fitness history + attractor landscapes (initial vs final)
        6. Export — evolution history written to outputs/evolution.json
    """
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")
    
    # 0. Config: contexts for this network size
    contexts = get_contexts(n_genes)

    if verbose:
        print(f"Network size: n={n_genes} ({genome_length(n_genes)}-bit genome)")
        print("Contexts:")
        for initial, target in contexts.items():
            print(f"  {initial} -> {target}")

    # 1. Truth table: random rules -> genome
    if verbose:
        print("\n[1/5] Generating truth table...")
    truth_table = generate_random_truth_table(n_genes)
    if verbose:
        print_truth_table(truth_table)

    initial_genome = truth_table_to_genome(truth_table)

    # 2. Transition table: truth table + rotated-input rule
    if verbose:
        print("\n[2/5] Building transition table...")
    initial_transition_table, _, _ = build_transition_table_from_genome(
        initial_genome, n_genes
    )

    # 3. Attractors: inspect initial state-transition diagram
    if verbose:
        print("\n[3/5] Detecting attractors (initial network)...")
    point_attractors, limit_cycles = find_attractors(initial_transition_table)
    print_attractors(point_attractors, limit_cycles)

    # 4. Evolution: fitness over contexts, bit-flip mutation
    if verbose:
        print("\n[4/5] Evolving genome...")
    history = evolve_genome(
        initial_genome=initial_genome,
        contexts=contexts,
        mutation_probability=mutation_probability,
        generations=generations,
        n_genes=n_genes,
    )

    # 5. Visualisation: fitness curve + attractor landscapes 
    if not show_plots:
        prepare_output_dir()

    if verbose:
        print("\n[5/5] Generating plots...")
    plot_fitness_history(
        history,
        None if show_plots else FITNESS_HISTORY_PLOT,
    )
    plot_attractor_landscape(
        history[0]["transition_table"],
        f"Initial Attractor Landscape (n={n_genes})",
        None if show_plots else INITIAL_LANDSCAPE_PLOT,
    )
    plot_attractor_landscape(
        history[-1]["transition_table"],
        f"Final Attractor Landscape (n={n_genes})",
        None if show_plots else FINAL_LANDSCAPE_PLOT,
    )

    # 6. Export: evolution history to JSON
    export_path = None if show_plots else EVOLUTION_JSON
    if export_path is not None:
        if verbose:
            print("\n[6/6] Exporting evolution JSON...")
        export_evolution_json(
            history=history,
            contexts=contexts,
            n_genes=n_genes,
            output_path=export_path,
            mutation_probability=mutation_probability,
            generations_requested=generations,
        )

    print("\nDone.")
    if not show_plots:
        print(f"Outputs saved in: {OUTPUT_DIR}")
    print(f"Initial fitness: {history[0]['fitness']}")
    print(f"Final fitness: {history[-1]['fitness']}")
    print(f"Total generations stored: {len(history)}")
    print(f"Accepted generations: {sum(1 for record in history if record['accepted'])}")
    if export_path is not None:
        print(f"Evolution JSON: {export_path}")


if __name__ == "__main__":
    from cli import parse_args

    args = parse_args()
    run_demo(
        n_genes=args.n_genes,
        mutation_probability=args.mutation_probability,
        generations=args.generations,
        show_plots=args.show_plots,
    )
