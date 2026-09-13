from __future__ import annotations

from config import RunOutputPaths, genome_length, prepare_run_output_dir
from exports.context_fitness import export_context_fitness_table
from exports.evolution_json import export_evolution_json
from rbn.attractors import find_attractors, print_attractors
from rbn.evolution import evolve_genome
from rbn.transition_table import build_transition_table_from_genome
from rbn.truth_table import generate_random_truth_table, print_truth_table, truth_table_to_genome
from rbn.types import EvolutionHistory, Genome
from visualisation.accepted_fitness_plot import plot_accepted_fitness_trajectory
from visualisation.factorised import (
    plot_factor_delta_heatmap,
    plot_factorised_fitness_trajectory,
)
from visualisation.plots_2d import plot_attractor_landscape, plot_fitness_history


def run_single_experiment(
    *,
    n_genes: int,
    contexts,
    mutation_probability: float,
    generations: int,
    show_plots: bool,
    output_paths: RunOutputPaths | None,
    run_label: str | None = None,
    quiet: bool = False,
    initial_genome: Genome | None = None,
) -> EvolutionHistory:
    """Run one full pipeline from random or fixed initial genome through export."""
    def log(message: str = "", *, force: bool = False) -> None:
        if not quiet or force:
            print(message)

    if run_label:
        log(f"\n{'=' * 60}")
        log(run_label)
        log(f"{'=' * 60}")

    log(f"Network size: n={n_genes} ({genome_length(n_genes)}-bit genome)")
    if not quiet:
        log("Contexts:")
        for initial, target in contexts.items():
            log(f"  {initial} -> {target}")

    log("\n[1/5] Preparing initial network...")
    if initial_genome is None:
        truth_table = generate_random_truth_table(n_genes)
        if not quiet:
            print_truth_table(truth_table)
        initial_genome = truth_table_to_genome(truth_table)
    elif not quiet:
        log("  using shared initial genome from batch configuration")

    log("\n[2/5] Building transition table...")
    initial_transition_table, _, _ = build_transition_table_from_genome(
        initial_genome, n_genes
    )

    log("\n[3/5] Detecting attractors (initial network)...")
    point_attractors, limit_cycles = find_attractors(initial_transition_table)
    if not quiet:
        print_attractors(point_attractors, limit_cycles)

    log("\n[4/5] Evolving genome...")
    history = evolve_genome(
        initial_genome=initial_genome,
        contexts=contexts,
        mutation_probability=mutation_probability,
        generations=generations,
        n_genes=n_genes,
    )

    if not show_plots and output_paths is not None:
        prepare_run_output_dir(output_paths)

    log("\n[5/5] Generating plots...")
    plot_fitness_history(
        history,
        None if show_plots or output_paths is None else output_paths.fitness_history_plot,
    )
    plot_attractor_landscape(
        history[0]["transition_table"],
        f"Initial Attractor Landscape (n={n_genes})",
        None if show_plots or output_paths is None else output_paths.initial_landscape_plot,
    )
    plot_attractor_landscape(
        history[-1]["transition_table"],
        f"Final Attractor Landscape (n={n_genes})",
        None if show_plots or output_paths is None else output_paths.final_landscape_plot,
    )
    plot_accepted_fitness_trajectory(
        history,
        contexts,
        None if show_plots or output_paths is None else output_paths.accepted_fitness_plot,
    )
    if output_paths is not None and not show_plots:
        plot_factorised_fitness_trajectory(
            history,
            contexts,
            n_genes,
            output_paths.factorised_trajectory_plot,
            csv_path=output_paths.factorised_trajectory_csv,
        )
        plot_factor_delta_heatmap(
            history,
            contexts,
            n_genes,
            output_paths.factor_delta_plot,
        )

    if output_paths is not None:
        log("\n[6/7] Exporting evolution JSON...")
        export_evolution_json(
            history=history,
            contexts=contexts,
            n_genes=n_genes,
            output_path=output_paths.evolution_json,
            mutation_probability=mutation_probability,
            generations_requested=generations,
        )
        log("\n[7/7] Exporting context fitness table...")
        export_context_fitness_table(
            history=history,
            contexts=contexts,
            csv_path=output_paths.context_fitness_csv,
            txt_path=output_paths.context_fitness_txt,
        )

    log("\nRun summary.")
    if output_paths is not None:
        log(f"Outputs saved in: {output_paths.run_dir}")
    log(f"Initial fitness: {history[0]['fitness']}")
    log(f"Final fitness: {history[-1]['fitness']}")
    log(f"Total generations stored: {len(history)}")
    log(f"Accepted generations: {sum(1 for record in history if record['accepted'])}")
    if output_paths is not None and not quiet:
        log(f"Evolution JSON: {output_paths.evolution_json}")
        log(f"Context fitness CSV: {output_paths.context_fitness_csv}")
        log(f"Context fitness table: {output_paths.context_fitness_txt}")
        log(f"Accepted fitness plot: {output_paths.accepted_fitness_plot}")
        log(f"Factorised trajectory plot: {output_paths.factorised_trajectory_plot}")
        log(f"Factorised trajectory CSV: {output_paths.factorised_trajectory_csv}")
        log(f"Factor delta heatmap: {output_paths.factor_delta_plot}")

    return history
