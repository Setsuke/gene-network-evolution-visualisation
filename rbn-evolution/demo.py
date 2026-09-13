"""
rbn-evolution demo: end-to-end pipeline from random network to evolved solution.

Pipeline (see run_demo):
    config/contexts  ->  truth table  ->  transition table  ->  attractors
    ->  fitness + evolution  ->  plots
"""

import config
from config import (
    DATA_OUTPUT_DIR,
    DEFAULT_N_GENES,
    EVOLUTIONARY_GENOME_MDS_PATH_CSV,
    OUTPUT_DIR,
    NEUTRAL_MOVE_SUMMARY_CSV,
    NEUTRAL_MOVE_SUMMARY_TABLE,
    NEUTRAL_MOVE_BAR_CHART_PLOT,
    NON_NEUTRAL_MOVE_SUMMARY_CSV,
    NON_NEUTRAL_MOVE_SUMMARY_TABLE,
    FACTOR_CHANGING_NEUTRAL_SUMMARY_TABLE,
    FACTOR_CHANGING_NEUTRAL_SUMMARY_CSV,
    PLATEAU_DRIFT_CLASSIFICATION_TABLE,
    PLATEAU_DRIFT_CLASSIFICATION_CSV,
    PLATEAU_DRIFT_EXIT_TYPES_TABLE,
    PLATEAU_DRIFT_EXIT_TYPES_CSV,
    DRIFTING_CATEGORIES_NORMALISED_PROGRESS_PLOT,
    DRIFTING_CATEGORIES_NORMALISED_PROGRESS_CSV,
    DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_PLOT,
    DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_CSV,
    DRIFT_EPISODE_LENGTH_HISTOGRAM_PLOT,
    DRIFT_EPISODE_GENOTYPE_EXPLORATION_EPISODES_CSV,
    DRIFT_EPISODE_GENOTYPE_EXPLORATION_QUARTILES_CSV,
    DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_QUARTILE_MAX_HAMMING_PLOT,
    DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_MEDIAN_START_END_HAMMING_PLOT,
    ACCEPTED_MOVES_HIERARCHY_FLOW,
    ACCEPTED_MOVES_FACTOR_NEUTRAL_FLOW,
    ACCEPTED_MOVES_FACTOR_CHANGING_NEUTRAL_FLOW,
    ACCEPTED_MOVES_FITNESS_IMPROVING_FLOW,
    ACCEPTED_MUTATION_CANDIDATES_BY_MOVE_TYPE_PLOT,
    ACCEPTED_MOVES_HIERARCHY_TXT,
    ACCEPTED_MOVES_HIERARCHY_CSV,
    EVOLUTION_PROGRESS_BREAKDOWN_CSV,
    EVOLUTION_PROGRESS_BREAKDOWN_PLOT,
    ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV,
    ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT,
    SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV,
    SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT,
    PROGRESS_ATTRACTOR_COUNTS_CSV,
    PROGRESS_ATTRACTOR_COUNTS_PLOT,
    ACCEPTED_MOVE_TRANSITIONS_CSV,
    ACCEPTED_MOVE_TRANSITION_COUNTS_CSV,
    ACCEPTED_MOVE_TRANSITION_MATRIX_CSV,
    PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV,
    PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT,
    MOST_DRIFTING_FITNESS_TABLE_CSV,
    MOST_DRIFTING_FITNESS_TABLE_PLOT,
    GENOME_MDS_UNITY_JSON,
    SHARED_INITIAL_GENOME_MDS_PLOT,
    SHARED_INITIAL_GENOME_MDS_CSV,
    GENOME_SHARED_MDS_UNITY_JSON,
    BATCH_GENOME_MDS_PATH_CSV,
    GENOME_BATCH_MDS_UNITY_JSON,
    BOOLEAN_STATE_MDS_LAYOUT_JSON,
    BOOLEAN_STATE_MDS_LAYOUT_CSV,
    UNITY_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
    UNITY_SHARED_EMBEDDING_MAX_RUNS,
    UNITY_BATCH_MDS_PATH_MAX_RUNS,
    get_contexts,
    prepare_batch_outputs,
)
from batch_runner import RunTaskResult, describe_job_plan, resolve_job_count, run_batch_experiments
from exports.boolean_state_mds_layout import (
    export_boolean_state_mds_layout_csv,
    export_boolean_state_mds_layout_json,
)
from exports.unity_attractor_landscape import export_unity_attractor_landscape_json
from visualisation.accepted_move_transitions import export_accepted_move_transition_tables
from visualisation.evolution_progress_breakdown import plot_evolution_progress_breakdown
from visualisation.accepted_genomes_by_progress_quartiles import (
    generate_accepted_genomes_by_progress_quartiles,
    generate_shared_accepted_genomes_by_progress_quartiles,
)
from visualisation.progress_attractor_counts import plot_progress_attractor_counts
from visualisation.genome_mds_path import export_evolutionary_genome_mds_path
from visualisation.accepted_moves_hierarchy import (
    plot_accepted_moves_category_flow_diagrams,
    plot_accepted_moves_hierarchy_flow,
    plot_accepted_moves_hierarchy_flow_summary,
)
from visualisation.factor_changing_neutral_moves import plot_factor_changing_neutral_summary_table
from visualisation.plateau_drift_classification import plot_plateau_drift_classification_table
from visualisation.plateau_drift_exit_types import plot_plateau_drift_exit_types_table
from visualisation.drifting_categories_normalised_progress import (
    plot_drifting_categories_normalised_progress,
    plot_drifting_categories_with_improvement_normalised_progress,
)
from visualisation.drift_episode_genotype_exploration import analyse_drift_episode_genotype_exploration
from visualisation.drift_episode_length_distribution import plot_drift_episode_length_histogram
from visualisation.non_neutral_moves import plot_fitness_improving_summary_table
from visualisation.neutral_moves import (
    plot_neutral_move_bar_chart,
    plot_neutral_move_summary_table,
)
from visualisation.pre_solution_fitness_table import plot_pre_solution_fitness_table
from visualisation.most_drifting_fitness_table import plot_most_drifting_fitness_table


def _unity_shared_embedding_batch(
    histories: list,
    run_labels: list[str],
) -> tuple[list, list[str]]:
    if len(histories) <= UNITY_SHARED_EMBEDDING_MAX_RUNS:
        return histories, run_labels

    print(
        f"  Unity Options 5–6: using first {UNITY_SHARED_EMBEDDING_MAX_RUNS} of "
        f"{len(histories)} runs for shared MDS export",
        flush=True,
    )
    return (
        histories[:UNITY_SHARED_EMBEDDING_MAX_RUNS],
        run_labels[:UNITY_SHARED_EMBEDDING_MAX_RUNS],
    )


def _unity_batch_mds_path_batch(
    histories: list,
    run_labels: list[str],
) -> tuple[list, list[str]]:
    if len(histories) <= UNITY_BATCH_MDS_PATH_MAX_RUNS:
        return histories, run_labels

    print(
        f"  Unity Option 7: using first {UNITY_BATCH_MDS_PATH_MAX_RUNS} of "
        f"{len(histories)} runs for batch MDS path export",
        flush=True,
    )
    return (
        histories[:UNITY_BATCH_MDS_PATH_MAX_RUNS],
        run_labels[:UNITY_BATCH_MDS_PATH_MAX_RUNS],
    )


def run_demo(
    n_genes: int = DEFAULT_N_GENES,
    mutation_probability: float = 0.1,
    generations: int = 1000,
    show_plots: bool = False,
    runs: int = 1,
    jobs: int | None = None,
    shared_initial_genome: bool = False,
    unity_streaming_assets=None,
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
    if unity_streaming_assets is not None:
        config.configure_unity_streaming_assets(unity_streaming_assets)

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
        shared_initial_genome=shared_initial_genome,
        progress_callback=on_run_complete if plan.jobs > 1 else None,
    )

    final_fitness_values = [history[-1]["fitness"] for history in histories]

    if not show_plots:
        print(f"\nUnity StreamingAssets export dir: {config.UNITY_STREAMING_ASSETS_DIR}")
        first_run_histories = histories[:1]
        first_run_labels = run_labels[:1]
        embedding_run_label = first_run_labels[0]

        print(f"\nExporting evolutionary genome MDS data ({embedding_run_label} only)...")
        export_evolutionary_genome_mds_path(
            first_run_histories,
            contexts,
            csv_path=EVOLUTIONARY_GENOME_MDS_PATH_CSV,
            labels=first_run_labels,
            unity_json_paths=[GENOME_MDS_UNITY_JSON, config.UNITY_GENOME_MDS_JSON],
            highlight_best_run=False,
        )
        print(f"Evolutionary genome MDS path CSV: {EVOLUTIONARY_GENOME_MDS_PATH_CSV}")
        print(f"Evolutionary genome MDS Unity JSON: {config.UNITY_GENOME_MDS_JSON}")

        if not shared_initial_genome and runs > 1:
            batch_mds_histories, batch_mds_labels = _unity_batch_mds_path_batch(
                histories,
                run_labels,
            )
            print(
                f"\nExporting batch MDS paths with distinct initial genomes "
                f"({len(batch_mds_histories)} run(s) for Unity Option 7)..."
            )
            export_evolutionary_genome_mds_path(
                batch_mds_histories,
                contexts,
                labels=batch_mds_labels,
                csv_path=BATCH_GENOME_MDS_PATH_CSV,
                unity_json_paths=[GENOME_BATCH_MDS_UNITY_JSON, config.UNITY_BATCH_GENOME_MDS_JSON],
                highlight_best_run=False,
                shared_initial_genome=False,
            )
            print(f"Batch genome MDS path CSV: {BATCH_GENOME_MDS_PATH_CSV}")
            print(f"Batch genome MDS Unity JSON: {config.UNITY_BATCH_GENOME_MDS_JSON}")

        shared_embedding_batch = None
        if shared_initial_genome:
            shared_embedding_batch = _unity_shared_embedding_batch(histories, run_labels)

        if shared_embedding_batch is not None:
            shared_histories, shared_labels = shared_embedding_batch
            print(
                f"\nGenerating shared-initial-genome MDS path "
                f"({len(shared_histories)} run(s) for Unity Option 5)..."
            )
            export_evolutionary_genome_mds_path(
                shared_histories,
                contexts,
                SHARED_INITIAL_GENOME_MDS_PLOT,
                labels=shared_labels,
                csv_path=SHARED_INITIAL_GENOME_MDS_CSV,
                unity_json_paths=[GENOME_SHARED_MDS_UNITY_JSON, config.UNITY_SHARED_GENOME_MDS_JSON],
                highlight_best_run=False,
                shared_initial_genome=True,
                shared_initial_genome_value=histories[0][0]["genome"],
            )
            print(f"Shared initial genome MDS plot: {SHARED_INITIAL_GENOME_MDS_PLOT}")
            print(f"Shared initial genome MDS CSV: {SHARED_INITIAL_GENOME_MDS_CSV}")
            print(f"Shared initial genome MDS Unity JSON: {config.UNITY_SHARED_GENOME_MDS_JSON}")

        print("\nExporting fixed Boolean state-space MDS layout (32 nodes)...")
        export_boolean_state_mds_layout_csv(BOOLEAN_STATE_MDS_LAYOUT_CSV, n_genes)
        export_boolean_state_mds_layout_json(
            [BOOLEAN_STATE_MDS_LAYOUT_JSON, config.UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON],
            n_genes,
        )
        print(f"Boolean state MDS layout CSV: {BOOLEAN_STATE_MDS_LAYOUT_CSV}")
        print(f"Boolean state MDS layout JSON: {config.UNITY_BOOLEAN_STATE_MDS_LAYOUT_JSON}")

        print("\nExporting Unity boolean attractor landscape dynamics...")
        export_unity_attractor_landscape_json(
            histories,
            contexts,
            [
                UNITY_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
                config.UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON,
            ],
            n_genes=n_genes,
            labels=run_labels,
        )
        print(
            "Unity boolean attractor landscape JSON: "
            f"{config.UNITY_STREAMING_BOOLEAN_ATTRACTOR_LANDSCAPE_JSON}"
        )

        print("\nGenerating last pre-solution fitness distribution table (one value per run)...")
        plot_pre_solution_fitness_table(
            histories,
            contexts,
            PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT,
            csv_path=PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV,
        )
        print(f"Pre-solution fitness distribution plot: {PRE_SOLUTION_FITNESS_DISTRIBUTION_PLOT}")
        print(f"Pre-solution fitness distribution CSV: {PRE_SOLUTION_FITNESS_DISTRIBUTION_CSV}")

        print("\nGenerating most drifting fitness table (accepted genomes pooled)...")
        plot_most_drifting_fitness_table(
            histories,
            contexts,
            MOST_DRIFTING_FITNESS_TABLE_PLOT,
            csv_path=MOST_DRIFTING_FITNESS_TABLE_CSV,
        )
        print(f"Most drifting fitness table plot: {MOST_DRIFTING_FITNESS_TABLE_PLOT}")
        print(f"Most drifting fitness table CSV: {MOST_DRIFTING_FITNESS_TABLE_CSV}")

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

        print("\nGenerating plateau drift classification table...")
        plot_plateau_drift_classification_table(
            histories,
            contexts,
            PLATEAU_DRIFT_CLASSIFICATION_TABLE,
            labels=run_labels,
            csv_path=PLATEAU_DRIFT_CLASSIFICATION_CSV,
        )
        print(f"Plateau drift classification table: {PLATEAU_DRIFT_CLASSIFICATION_TABLE}")
        print(f"Plateau drift classification CSV: {PLATEAU_DRIFT_CLASSIFICATION_CSV}")

        print("\nGenerating plateau drift exit types table...")
        plot_plateau_drift_exit_types_table(
            histories,
            contexts,
            PLATEAU_DRIFT_EXIT_TYPES_TABLE,
            labels=run_labels,
            csv_path=PLATEAU_DRIFT_EXIT_TYPES_CSV,
        )
        print(f"Plateau drift exit types table: {PLATEAU_DRIFT_EXIT_TYPES_TABLE}")
        print(f"Plateau drift exit types CSV: {PLATEAU_DRIFT_EXIT_TYPES_CSV}")

        print("\nGenerating drifting categories by normalised progress...")
        plot_drifting_categories_normalised_progress(
            histories,
            contexts,
            DRIFTING_CATEGORIES_NORMALISED_PROGRESS_PLOT,
            labels=run_labels,
            csv_path=DRIFTING_CATEGORIES_NORMALISED_PROGRESS_CSV,
        )
        print(
            "Drifting categories normalised progress plot: "
            f"{DRIFTING_CATEGORIES_NORMALISED_PROGRESS_PLOT}"
        )
        print(
            "Drifting categories normalised progress CSV: "
            f"{DRIFTING_CATEGORIES_NORMALISED_PROGRESS_CSV}"
        )

        print("\nGenerating drifting categories + improvement by normalised progress...")
        plot_drifting_categories_with_improvement_normalised_progress(
            histories,
            contexts,
            DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_PLOT,
            labels=run_labels,
            csv_path=DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_CSV,
        )
        print(
            "Drifting categories + improvement plot: "
            f"{DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_PLOT}"
        )
        print(
            "Drifting categories + improvement CSV: "
            f"{DRIFTING_CATEGORIES_WITH_IMPROVEMENT_NORMALISED_PROGRESS_CSV}"
        )

        print("\nGenerating drift episode length histogram...")
        plot_drift_episode_length_histogram(
            histories,
            contexts,
            save_path=DRIFT_EPISODE_LENGTH_HISTOGRAM_PLOT,
            labels=run_labels,
        )
        print(f"Drift episode length histogram: {DRIFT_EPISODE_LENGTH_HISTOGRAM_PLOT}")

        print("\nAnalysing drift-episode genotype-space exploration...")
        analyse_drift_episode_genotype_exploration(
            histories,
            contexts,
            episode_csv_path=DRIFT_EPISODE_GENOTYPE_EXPLORATION_EPISODES_CSV,
            quartile_csv_path=DRIFT_EPISODE_GENOTYPE_EXPLORATION_QUARTILES_CSV,
            length_quartile_max_hamming_plot=DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_QUARTILE_MAX_HAMMING_PLOT,
            length_median_start_end_hamming_plot=DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_MEDIAN_START_END_HAMMING_PLOT,
            labels=run_labels,
        )
        print(
            "Drift genotype exploration episodes CSV: "
            f"{DRIFT_EPISODE_GENOTYPE_EXPLORATION_EPISODES_CSV}"
        )
        print(
            "Drift length vs median start-end Hamming plot: "
            f"{DRIFT_EPISODE_GENOTYPE_EXPLORATION_LENGTH_MEDIAN_START_END_HAMMING_PLOT}"
        )

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

        print("\nGenerating accepted moves category flow diagrams...")
        plot_accepted_moves_category_flow_diagrams(
            histories,
            contexts,
            n_genes,
            factor_neutral_plot=ACCEPTED_MOVES_FACTOR_NEUTRAL_FLOW,
            factor_changing_neutral_plot=ACCEPTED_MOVES_FACTOR_CHANGING_NEUTRAL_FLOW,
            fitness_improving_plot=ACCEPTED_MOVES_FITNESS_IMPROVING_FLOW,
            labels=run_labels,
        )
        print(f"Factor-neutral accepted moves flow: {ACCEPTED_MOVES_FACTOR_NEUTRAL_FLOW}")
        print(
            "Factor-changing neutral accepted moves flow: "
            f"{ACCEPTED_MOVES_FACTOR_CHANGING_NEUTRAL_FLOW}"
        )
        print(f"Fitness-improving accepted moves flow: {ACCEPTED_MOVES_FITNESS_IMPROVING_FLOW}")

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

        print("\nGenerating accepted genomes by progress quartile chart...")
        if shared_initial_genome:
            generate_shared_accepted_genomes_by_progress_quartiles(
                shared_genome_mds_json_path=config.UNITY_SHARED_GENOME_MDS_JSON,
            )
            print(
                "Shared accepted genomes by progress quartiles plot: "
                f"{SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT}"
            )
            print(
                "Shared accepted genomes by progress quartiles CSV: "
                f"{SHARED_ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV}"
            )
        else:
            generate_accepted_genomes_by_progress_quartiles(histories=histories)
            print(
                "Accepted genomes by progress quartiles plot: "
                f"{ACCEPTED_GENOMES_PROGRESS_QUARTILES_PLOT}"
            )
            print(
                "Accepted genomes by progress quartiles CSV: "
                f"{ACCEPTED_GENOMES_PROGRESS_QUARTILES_CSV}"
            )

        print("\nGenerating progress vs attractor count plot...")
        plot_progress_attractor_counts(
            histories,
            PROGRESS_ATTRACTOR_COUNTS_PLOT,
            labels=run_labels,
            csv_path=PROGRESS_ATTRACTOR_COUNTS_CSV,
        )
        print(f"Progress attractor counts plot: {PROGRESS_ATTRACTOR_COUNTS_PLOT}")
        print(f"Progress attractor counts CSV: {PROGRESS_ATTRACTOR_COUNTS_CSV}")

        print("\nGenerating accepted move transition matrix CSVs...")
        export_accepted_move_transition_tables(
            histories,
            contexts,
            labels=run_labels,
            moves_csv_path=ACCEPTED_MOVE_TRANSITIONS_CSV,
            counts_csv_path=ACCEPTED_MOVE_TRANSITION_COUNTS_CSV,
            percentages_csv_path=ACCEPTED_MOVE_TRANSITION_MATRIX_CSV,
        )
        print(f"Accepted move transitions table: {ACCEPTED_MOVE_TRANSITIONS_CSV}")
        print(f"Accepted move transition counts: {ACCEPTED_MOVE_TRANSITION_COUNTS_CSV}")
        print(f"Accepted move transition matrix: {ACCEPTED_MOVE_TRANSITION_MATRIX_CSV}")

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
        shared_initial_genome=args.shared_initial_genome,
        unity_streaming_assets=args.unity_streaming_assets,
    )
