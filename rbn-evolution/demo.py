"""
rbn-evolution demo: end-to-end pipeline from random network to evolved solution.

Pipeline (see run_demo):
    config/contexts  ->  truth table  ->  transition table  ->  attractors
    ->  fitness + evolution  ->  plots
"""

from config import (
    DEFAULT_N_GENES,
    EVOLUTIONARY_LANDSCAPE_CSV,
    EVOLUTIONARY_LANDSCAPE_PLOT,
    OUTPUT_DIR,
    BATCH_FITNESS_LOAD_HEATMAP_CSV,
    BATCH_FITNESS_LOAD_HEATMAP_PLOT,
    FACTOR_COMPLETION_LANDSCAPE_CSV,
    FACTOR_COMPLETION_LANDSCAPE_PLOT,
    UNITY_TRAJECTORIES_JSON,
    get_contexts,
    prepare_batch_outputs,
)
from batch_runner import RunTaskResult, describe_job_plan, resolve_job_count, run_batch_experiments
from exports.unity_trajectories import export_unity_trajectories_json
from visualisation.evolutionary_landscape import plot_evolutionary_landscape
from visualisation.factorised import plot_factor_completion_landscape
from visualisation.batch_fitness_load import plot_batch_fitness_load_heatmap


def run_demo(
    n_genes: int = DEFAULT_N_GENES,
    mutation_probability: float = 0.1,
    generations: int = 1000,
    show_plots: bool = False,
    runs: int = 1,
    jobs: int | None = None,
) -> None:
    """
    Run the full rbn-evolution experiment one or more times.

    Pipeline per run:
        0. Config — load contexts (initial -> target) for this n from config.py
        1. Truth table — random boolean rules; encoded as an n * 2^n-bit genome
        2. Transition table — synchronous dynamics via the rotated-input rule
        3. Attractors — point attractors and limit cycles of the initial network
        4. Evolution — mutate genome; accept if fitness >= previous (contexts in config)
        5. Plots — fitness history + attractor landscapes (initial vs final)
        6. Export — evolution history written to evolution.json
        7. Export — context fitness table (CSV + text)

    When runs > 1, per-run outputs go to outputs/runs/run_001/, outputs/runs/run_002/, ...
    Parallel workers are chosen automatically from CPU and RAM unless --jobs is set.
    """
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")
    if runs < 1:
        raise ValueError("runs must be at least 1")

    contexts = get_contexts(n_genes)

    if not show_plots:
        prepare_batch_outputs(runs)

    effective_jobs = 1 if show_plots else jobs
    if show_plots and jobs not in (None, 1):
        print("Note: --show-plots forces sequential execution (jobs=1).")

    plan = resolve_job_count(runs=runs, jobs=effective_jobs)
    if runs > 1:
        print(describe_job_plan(plan, runs))

    def on_run_complete(result: RunTaskResult) -> None:
        final_fitness = result.history[-1]["fitness"]
        print(f"  completed {result.run_label}: final fitness = {final_fitness:.6g}")

    histories, run_labels, _ = run_batch_experiments(
        runs=runs,
        n_genes=n_genes,
        contexts=contexts,
        mutation_probability=mutation_probability,
        generations=generations,
        show_plots=show_plots,
        jobs=effective_jobs,
        progress_callback=on_run_complete if plan.jobs > 1 else None,
    )

    final_fitness_values = [history[-1]["fitness"] for history in histories]

    if not show_plots:
        print("\nGenerating evolutionary landscape...")
        plot_evolutionary_landscape(
            histories,
            EVOLUTIONARY_LANDSCAPE_PLOT,
            labels=run_labels,
            csv_path=EVOLUTIONARY_LANDSCAPE_CSV,
        )
        print(f"Evolutionary landscape plot: {EVOLUTIONARY_LANDSCAPE_PLOT}")
        print(f"Evolutionary landscape CSV: {EVOLUTIONARY_LANDSCAPE_CSV}")

        print("\nGenerating Batch Fitness Load Heatmap...")
        plot_batch_fitness_load_heatmap(
            histories,
            BATCH_FITNESS_LOAD_HEATMAP_PLOT,
            bin_width=0.01,
            load_cutoff=0.5,
            csv_path=BATCH_FITNESS_LOAD_HEATMAP_CSV,
        )
        print(f"Batch Fitness Load Heatmap plot: {BATCH_FITNESS_LOAD_HEATMAP_PLOT}")
        print(f"Batch Fitness Load Heatmap CSV: {BATCH_FITNESS_LOAD_HEATMAP_CSV}")

        print("\nGenerating factor completion landscape...")
        plot_factor_completion_landscape(
            histories,
            contexts,
            n_genes,
            FACTOR_COMPLETION_LANDSCAPE_PLOT,
            labels=run_labels,
            csv_path=FACTOR_COMPLETION_LANDSCAPE_CSV,
            rank_by="time_to_solution",
        )
        print(f"Factor completion landscape plot: {FACTOR_COMPLETION_LANDSCAPE_PLOT}")
        print(f"Factor completion landscape CSV: {FACTOR_COMPLETION_LANDSCAPE_CSV}")

        print("\nExporting Unity trajectory JSON...")
        export_unity_trajectories_json(
            histories,
            UNITY_TRAJECTORIES_JSON,
            labels=run_labels,
        )
        print(f"Unity trajectories JSON: {UNITY_TRAJECTORIES_JSON}")

    print("\nDone.")
    if not show_plots:
        if runs == 1:
            print(f"All outputs saved in: {OUTPUT_DIR}")
        else:
            print(f"Per-run outputs saved under: {OUTPUT_DIR}/runs/run_001 .. run_{runs:03d}")
            print(f"Final fitness values: {final_fitness_values}")


if __name__ == "__main__":
    from cli import parse_args

    args = parse_args()
    run_demo(
        n_genes=args.n_genes,
        mutation_probability=args.mutation_probability,
        generations=args.generations,
        show_plots=args.show_plots,
        runs=args.runs,
        jobs=args.jobs,
    )
