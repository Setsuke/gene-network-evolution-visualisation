"""
rbn-evolution demo: end-to-end pipeline from random network to evolved solution.

Pipeline (see run_demo):
    config/contexts  ->  truth table  ->  transition table  ->  attractors
    ->  fitness + evolution  ->  plots
"""

from config import (
    DATA_OUTPUT_DIR,
    DEFAULT_N_GENES,
    EVOLUTIONARY_GENOME_MDS_PATH_PLOT,
    EVOLUTIONARY_GENOME_MDS_PATH_CSV,
    EVOLUTIONARY_GENOME_UMAP_PATH_PLOT,
    EVOLUTIONARY_GENOME_UMAP_PATH_CSV,
    OUTPUT_DIR,
    BATCH_FITNESS_LOAD_HEATMAP_CSV,
    BATCH_FITNESS_LOAD_HEATMAP_PLOT,
    NEUTRAL_MOVE_SUMMARY_CSV,
    NEUTRAL_MOVE_SUMMARY_TABLE,
    NEUTRAL_MOVE_BAR_CHART_PLOT,
    NON_NEUTRAL_MOVE_SUMMARY_CSV,
    NON_NEUTRAL_MOVE_SUMMARY_TABLE,
    FACTOR_CHANGING_NEUTRAL_SUMMARY_TABLE,
    FACTOR_CHANGING_NEUTRAL_SUMMARY_CSV,
    ACCEPTED_MOVES_HIERARCHY_FLOW,
    ACCEPTED_MUTATION_CANDIDATES_BY_MOVE_TYPE_PLOT,
    ACCEPTED_MOVES_HIERARCHY_TXT,
    ACCEPTED_MOVES_HIERARCHY_CSV,
    EVOLUTION_PROGRESS_BREAKDOWN_CSV,
    EVOLUTION_PROGRESS_BREAKDOWN_PLOT,
    ACCEPTED_MOVE_TRANSITIONS_CSV,
    ACCEPTED_MOVE_TRANSITION_COUNTS_CSV,
    ACCEPTED_MOVE_TRANSITION_MATRIX_CSV,
    ACCEPTED_MOVE_TRANSITION_HEATMAP,
    PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV,
    PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT,
    GENOME_MDS_UNITY_JSON,
    GENOME_UMAP_UNITY_JSON,
    UNITY_TRAJECTORIES_JSON,
    UNITY_GENOME_MDS_JSON,
    UNITY_GENOME_UMAP_JSON,
    BOOLEAN_STATE_MDS_LAYOUT_JSON,
    BOOLEAN_STATE_MDS_LAYOUT_CSV,
    UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON,
    UNITY_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
    UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
    get_contexts,
    prepare_batch_outputs,
)
from batch_runner import RunTaskResult, describe_job_plan, resolve_job_count, run_batch_experiments
from exports.unity_trajectories import export_unity_trajectories_json
from exports.boolean_state_mds_layout import (
    export_boolean_state_mds_layout_csv,
    export_boolean_state_mds_layout_json,
)
from exports.unity_attractor_landscape import export_unity_attractor_landscape_json
from visualisation.accepted_move_transitions import plot_accepted_move_transition_heatmap
from visualisation.evolution_progress_breakdown import plot_evolution_progress_breakdown
from visualisation.genome_mds_path import plot_evolutionary_genome_mds_path
from visualisation.genome_umap_path import plot_evolutionary_genome_umap_path
from visualisation.accepted_moves_hierarchy import (
    plot_accepted_moves_hierarchy_flow,
    plot_accepted_moves_hierarchy_flow_summary,
)
from visualisation.factor_changing_neutral_moves import plot_factor_changing_neutral_summary_table
from visualisation.non_neutral_moves import plot_fitness_improving_summary_table
from visualisation.neutral_moves import (
    plot_neutral_move_bar_chart,
    plot_neutral_move_summary_table,
)
from visualisation.pre_solution_fitness_table import plot_pre_solution_fitness_table
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
        first_run_histories = histories[:1]
        first_run_labels = run_labels[:1]
        embedding_run_label = first_run_labels[0]

        print(f"\nGenerating evolutionary genome MDS path ({embedding_run_label} only)...")
        plot_evolutionary_genome_mds_path(
            first_run_histories,
            contexts,
            EVOLUTIONARY_GENOME_MDS_PATH_PLOT,
            labels=first_run_labels,
            csv_path=EVOLUTIONARY_GENOME_MDS_PATH_CSV,
            unity_json_paths=[GENOME_MDS_UNITY_JSON, UNITY_GENOME_MDS_JSON],
            highlight_best_run=False,
        )
        print(f"Evolutionary genome MDS path plot: {EVOLUTIONARY_GENOME_MDS_PATH_PLOT}")
        print(f"Evolutionary genome MDS path CSV: {EVOLUTIONARY_GENOME_MDS_PATH_CSV}")
        print(f"Evolutionary genome MDS Unity JSON: {UNITY_GENOME_MDS_JSON}")

        print(f"\nGenerating evolutionary genome UMAP path ({embedding_run_label} only)...")
        plot_evolutionary_genome_umap_path(
            first_run_histories,
            contexts,
            EVOLUTIONARY_GENOME_UMAP_PATH_PLOT,
            labels=first_run_labels,
            csv_path=EVOLUTIONARY_GENOME_UMAP_PATH_CSV,
            unity_json_paths=[GENOME_UMAP_UNITY_JSON, UNITY_GENOME_UMAP_JSON],
            highlight_best_run=False,
        )
        print(f"Evolutionary genome UMAP path plot: {EVOLUTIONARY_GENOME_UMAP_PATH_PLOT}")
        print(f"Evolutionary genome UMAP path CSV: {EVOLUTIONARY_GENOME_UMAP_PATH_CSV}")
        print(f"Evolutionary genome UMAP Unity JSON: {UNITY_GENOME_UMAP_JSON}")

        print("\nExporting fixed Boolean state-space MDS layout (32 nodes)...")
        export_boolean_state_mds_layout_csv(BOOLEAN_STATE_MDS_LAYOUT_CSV, n_genes)
        export_boolean_state_mds_layout_json(
            [BOOLEAN_STATE_MDS_LAYOUT_JSON, UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON],
            n_genes,
        )
        print(f"Boolean state MDS layout CSV: {BOOLEAN_STATE_MDS_LAYOUT_CSV}")
        print(f"Boolean state MDS layout JSON: {UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON}")

        print("\nExporting Unity boolean attractor landscape dynamics...")
        export_unity_attractor_landscape_json(
            histories,
            contexts,
            [
                UNITY_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
                UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
            ],
            n_genes=n_genes,
            labels=run_labels,
        )
        print(
            "Unity boolean attractor landscape JSON: "
            f"{UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON}"
        )

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

        print("\nGenerating pre-solution fitness distribution table...")
        plot_pre_solution_fitness_table(
            histories,
            contexts,
            PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT,
            csv_path=PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV,
        )
        print(f"Pre-solution fitness distribution plot: {PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT}")
        print(f"Pre-solution fitness distribution CSV: {PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV}")

        print("\nGenerating neutral move summary table...")
        plot_neutral_move_summary_table(
            histories,
            contexts,
            n_genes,
            NEUTRAL_MOVE_SUMMARY_TABLE,
            labels=run_labels,
            csv_path=NEUTRAL_MOVE_SUMMARY_CSV,
        )
        print(f"Neutral move summary table: {NEUTRAL_MOVE_SUMMARY_TABLE}")
        print(f"Neutral move summary CSV: {NEUTRAL_MOVE_SUMMARY_CSV}")

        print("\nGenerating neutral move bar chart...")
        plot_neutral_move_bar_chart(
            histories,
            contexts,
            n_genes,
            NEUTRAL_MOVE_BAR_CHART_PLOT,
            labels=run_labels,
        )
        print(f"Neutral move bar chart: {NEUTRAL_MOVE_BAR_CHART_PLOT}")

        print("\nGenerating factor-changing neutral accepted move summary table...")
        plot_factor_changing_neutral_summary_table(
            histories,
            contexts,
            n_genes,
            FACTOR_CHANGING_NEUTRAL_SUMMARY_TABLE,
            labels=run_labels,
            csv_path=FACTOR_CHANGING_NEUTRAL_SUMMARY_CSV,
        )
        print(f"Factor-changing neutral summary table: {FACTOR_CHANGING_NEUTRAL_SUMMARY_TABLE}")
        print(f"Factor-changing neutral summary CSV: {FACTOR_CHANGING_NEUTRAL_SUMMARY_CSV}")

        print("\nGenerating fitness-improving accepted move summary table...")
        plot_fitness_improving_summary_table(
            histories,
            contexts,
            n_genes,
            NON_NEUTRAL_MOVE_SUMMARY_TABLE,
            labels=run_labels,
            csv_path=NON_NEUTRAL_MOVE_SUMMARY_CSV,
        )
        print(f"Fitness-improving accepted move summary table: {NON_NEUTRAL_MOVE_SUMMARY_TABLE}")
        print(f"Fitness-improving accepted move summary CSV: {NON_NEUTRAL_MOVE_SUMMARY_CSV}")

        print("\nGenerating accepted moves hierarchy flow diagram...")
        plot_accepted_moves_hierarchy_flow(
            histories,
            contexts,
            n_genes,
            ACCEPTED_MOVES_HIERARCHY_FLOW,
            labels=run_labels,
            csv_path=ACCEPTED_MOVES_HIERARCHY_CSV,
            txt_path=ACCEPTED_MOVES_HIERARCHY_TXT,
        )
        print(f"Classification of Accepted Evolutionary Moves: {ACCEPTED_MOVES_HIERARCHY_FLOW}")
        print(f"Accepted moves hierarchy CSV: {ACCEPTED_MOVES_HIERARCHY_CSV}")
        print(f"Accepted moves hierarchy TXT: {ACCEPTED_MOVES_HIERARCHY_TXT}")

        print("\nGenerating accepted mutation candidates by move type plot...")
        plot_accepted_moves_hierarchy_flow_summary(
            histories,
            contexts,
            n_genes,
            ACCEPTED_MUTATION_CANDIDATES_BY_MOVE_TYPE_PLOT,
            labels=run_labels,
        )
        print(
            "Accepted mutation candidates by move type plot: "
            f"{ACCEPTED_MUTATION_CANDIDATES_BY_MOVE_TYPE_PLOT}"
        )

        print("\nGenerating evolution progress breakdown...")
        plot_evolution_progress_breakdown(
            histories,
            contexts,
            n_genes,
            EVOLUTION_PROGRESS_BREAKDOWN_PLOT,
            labels=run_labels,
            csv_path=EVOLUTION_PROGRESS_BREAKDOWN_CSV,
        )
        print(f"Evolution progress breakdown plot: {EVOLUTION_PROGRESS_BREAKDOWN_PLOT}")
        print(f"Evolution progress breakdown CSV: {EVOLUTION_PROGRESS_BREAKDOWN_CSV}")

        print("\nGenerating accepted move transition matrix...")
        plot_accepted_move_transition_heatmap(
            histories,
            contexts,
            ACCEPTED_MOVE_TRANSITION_HEATMAP,
            labels=run_labels,
            moves_csv_path=ACCEPTED_MOVE_TRANSITIONS_CSV,
            counts_csv_path=ACCEPTED_MOVE_TRANSITION_COUNTS_CSV,
            percentages_csv_path=ACCEPTED_MOVE_TRANSITION_MATRIX_CSV,
        )
        print(f"Accepted move transitions table: {ACCEPTED_MOVE_TRANSITIONS_CSV}")
        print(f"Accepted move transition counts: {ACCEPTED_MOVE_TRANSITION_COUNTS_CSV}")
        print(f"Accepted move transition matrix: {ACCEPTED_MOVE_TRANSITION_MATRIX_CSV}")
        print(f"Accepted move transition heatmap: {ACCEPTED_MOVE_TRANSITION_HEATMAP}")

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
            print(f"Figures saved in: {OUTPUT_DIR}")
            print(f"Batch CSV data saved in: {DATA_OUTPUT_DIR}")
        else:
            print(f"Per-run outputs saved under: {OUTPUT_DIR}/runs/run_001 .. run_{runs:03d}")
            print(f"Per-run CSV data saved under: .../data/")
            print(f"Batch figures saved in: {OUTPUT_DIR}")
            print(f"Batch CSV data saved in: {DATA_OUTPUT_DIR}")
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
