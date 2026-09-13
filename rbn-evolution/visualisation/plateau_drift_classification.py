"""Summary table of move categories shown in Unity Option 10 (Plateau Drift View)."""

from __future__ import annotations

import csv
from collections import Counter
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.genome_mds_path import extract_accepted_genome_paths

FITNESS_EPSILON = 1e-5

PLATEAU_DRIFT_CATEGORY_ORDER: tuple[str, ...] = (
    "only_irrelevant_transitions_changed",
    "path_transitions_changed_attractor_cycle_unchanged",
    "attractor_cycle_changed_factor_scores_unchanged",
    "factor_changing_neutral",
)

PLATEAU_DRIFT_EXIT_CATEGORIES: frozenset[str] = frozenset(
    {
        "direct_improvement",
        "compensatory_improvement",
    }
)

PLATEAU_DRIFT_CATEGORY_LABELS: dict[str, str] = {
    "only_irrelevant_transitions_changed": "Only irrelevant transitions changed",
    "path_transitions_changed_attractor_cycle_unchanged": "Path changed, cycle unchanged",
    "attractor_cycle_changed_factor_scores_unchanged": "Cycle changed, factor scores unchanged",
    "factor_changing_neutral": "Factor-changing neutral",
    "factor_neutral": "Factor neutral (unspecified subcategory)",
}

TABLE_HEADERS = [
    "Category",
    "Count",
    "Pct of plateau drift moves",
    "Episodes with >=1",
]


@dataclass(frozen=True)
class PlateauDriftClassificationSummary:
    run_count: int
    episode_count: int
    move_count: int
    category_counts: dict[str, int]
    episodes_with_category: dict[str, int]


def find_plateau_episodes(fitness_values: list[float]) -> list[tuple[int, int, int]]:
    """Return (start_step, end_step, exit_step) triples matching Unity plateau detection."""
    episodes: list[tuple[int, int, int]] = []
    search_step = 0
    step_count = len(fitness_values)

    while search_step < step_count - 1:
        plateau_fitness = fitness_values[search_step]
        end_step = search_step
        while (
            end_step + 1 < step_count
            and abs(fitness_values[end_step + 1] - plateau_fitness) <= FITNESS_EPSILON
        ):
            end_step += 1

        exit_step = end_step + 1
        has_neutral_moves = end_step > search_step
        has_exit = (
            exit_step < step_count
            and fitness_values[exit_step] > plateau_fitness + FITNESS_EPSILON
        )

        if has_neutral_moves and has_exit:
            episodes.append((search_step, end_step, exit_step))

        search_step = exit_step if has_exit else end_step + 1

    return episodes


def collect_plateau_drift_classification_summary(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    labels: list[str] | None = None,
) -> PlateauDriftClassificationSummary:
    """
    Count accepted move categories during plateau drift only.

    Counts moves from the first neutral step through the last plateau step
    (``start_step + 1`` .. ``end_step``). Exit moves at ``exit_step`` — direct
    or compensatory improvement — are excluded.
    """
    paths = extract_accepted_genome_paths(histories, contexts, labels=labels)
    move_counts: Counter[str] = Counter()
    episodes_with_category: Counter[str] = Counter()
    episode_count = 0

    for path in paths:
        fitness_values = list(path.fitness_values)
        change_types = list(path.change_types)
        for start_step, end_step, exit_step in find_plateau_episodes(fitness_values):
            episode_count += 1
            episode_categories: set[str] = set()
            for step_index in range(start_step + 1, end_step + 1):
                category = change_types[step_index]
                if category in PLATEAU_DRIFT_EXIT_CATEGORIES:
                    continue
                move_counts[category] += 1
                episode_categories.add(category)
            for category in episode_categories:
                episodes_with_category[category] += 1

    return PlateauDriftClassificationSummary(
        run_count=len(paths),
        episode_count=episode_count,
        move_count=sum(move_counts.values()),
        category_counts=dict(move_counts),
        episodes_with_category=dict(episodes_with_category),
    )


def build_plateau_drift_classification_rows(
    summary: PlateauDriftClassificationSummary,
) -> tuple[list[str], list[list[str | int | float]]]:
    rows: list[list[str | int | float]] = []
    total_moves = summary.move_count

    ordered_categories = list(PLATEAU_DRIFT_CATEGORY_ORDER)
    extra_categories = [
        category
        for category in summary.category_counts
        if category not in ordered_categories
    ]
    ordered_categories.extend(sorted(extra_categories))

    for category in ordered_categories:
        if category in PLATEAU_DRIFT_EXIT_CATEGORIES:
            continue
        count = summary.category_counts.get(category, 0)
        if category not in PLATEAU_DRIFT_CATEGORY_ORDER and count == 0:
            continue
        label = PLATEAU_DRIFT_CATEGORY_LABELS.get(
            category,
            category.replace("_", " ").capitalize(),
        )
        rows.append(
            [
                label,
                count,
                100.0 * count / total_moves if total_moves else 0.0,
                summary.episodes_with_category.get(category, 0),
            ]
        )

    rows.append(
        [
            "All plateau drift moves (excludes exit step)",
            total_moves,
            100.0 if total_moves else 0.0,
            summary.episode_count,
        ]
    )
    return TABLE_HEADERS, rows


def _write_summary_csv(
    headers: list[str],
    rows: list[list[str | int | float]],
    csv_path: Path,
) -> None:
    csv_path.parent.mkdir(parents=True, exist_ok=True)
    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(headers)
        writer.writerows(rows)


def plot_plateau_drift_classification_table(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
) -> Path:
    if plt is None:
        raise ImportError("matplotlib is required for plateau drift classification tables")

    from visualisation.non_neutral_moves import _style_summary_table

    summary = collect_plateau_drift_classification_summary(
        histories,
        contexts,
        labels=labels,
    )
    headers, rows = build_plateau_drift_classification_rows(summary)

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    if csv_path is not None:
        _write_summary_csv(headers, rows, Path(csv_path))

    display_rows = [
        [
            str(row[0]),
            str(row[1]),
            f"{float(row[2]):.2f}%",
            str(row[3]),
        ]
        for row in rows
    ]

    figure_height = max(4.0, 0.55 * len(display_rows) + 2.0)
    figure, axis = plt.subplots(figsize=(12.5, figure_height))
    axis.axis("off")
    table = axis.table(
        cellText=display_rows,
        colLabels=headers,
        loc="center",
        cellLoc="center",
        colWidths=[0.44, 0.12, 0.22, 0.18],
    )
    _style_summary_table(table, header_fontsize=9, body_fontsize=9)
    table.auto_set_font_size(False)
    table.scale(1.0, 1.65)

    figure.suptitle(
        "Classification — Plateau Drift View\n"
        f"{summary.run_count} runs  |  {summary.episode_count} plateau episodes  |  "
        f"{summary.move_count} drift moves (exit excluded)",
        fontsize=12,
        y=0.98,
    )
    figure.text(
        0.5,
        0.03,
        "Counts neutral accepted moves during plateau drift only (first drift step through "
        "last plateau step). Exit moves (direct/compensatory improvement) are excluded.",
        ha="center",
        fontsize=9,
        color="#334155",
    )
    figure.subplots_adjust(top=0.86, bottom=0.12)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.35)
    plt.close(figure)
    return save_path
