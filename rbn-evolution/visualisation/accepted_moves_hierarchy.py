"""Hierarchical breakdown of accepted genome change classification."""

from __future__ import annotations

import csv
from dataclasses import dataclass
from pathlib import Path

try:
    import matplotlib.pyplot as plt
    from matplotlib.patches import FancyBboxPatch, Polygon
except ImportError:
    plt = None
    FancyBboxPatch = None
    Polygon = None

from rbn.types import Contexts, EvolutionHistory
from visualisation.accepted_move_taxonomy import count_accepted_genome_changes
from visualisation.factor_changing_neutral_moves import (
    FACTOR_CHANGING_CHART_GROUPS,
    OTHER_FACTOR_CHANGING_NEUTRAL_MOVES,
    ZERO_MASKED_IMPROVEMENT_DETERIORATION,
    collect_factor_changing_neutral_events,
)
from visualisation.neutral_moves import (
    CATEGORY_ORDER,
    collect_factor_neutral_accepted_events,
)
from visualisation.non_neutral_moves import (
    FITNESS_IMPROVING_CATEGORY_LABELS,
    FITNESS_IMPROVING_CATEGORY_ORDER,
    collect_fitness_improving_events,
)

FACTOR_NEUTRAL_ACCEPTED_MOVES = "Factor-neutral accepted moves"
FACTOR_CHANGING_NEUTRAL_ACCEPTED_MOVES = "Factor-changing neutral accepted moves"
FITNESS_IMPROVING_ACCEPTED_MOVES = "Fitness-improving accepted moves"

FACTOR_NEUTRAL_BREAKDOWN_LABELS = {
    "no_transition_changed": "No transition changed",
    "only_irrelevant_transitions_changed": "Only irrelevant transitions changed",
    "path_transitions_changed_attractor_cycle_unchanged": "Path changed, cycle unchanged",
    "attractor_cycle_changed_factor_scores_unchanged": "Cycle changed, factor scores unchanged",
}

LEAF_MIN_BAND_HEIGHT = 0.036

GROUP_COLORS = {
    FACTOR_NEUTRAL_ACCEPTED_MOVES: "#0284c7",
    FACTOR_CHANGING_NEUTRAL_ACCEPTED_MOVES: "#d97706",
    FITNESS_IMPROVING_ACCEPTED_MOVES: "#16a34a",
}

CHILD_COLORS = {
    "Only irrelevant transitions changed": "#38bdf8",
    "Path changed, cycle unchanged": "#f59e0b",
    "Cycle changed, factor scores unchanged": "#a855f7",
    "No transition changed": "#cbd5e1",
    "Compensatory neutral changes": "#fb923c",
    ZERO_MASKED_IMPROVEMENT_DETERIORATION: "#fdba74",
    OTHER_FACTOR_CHANGING_NEUTRAL_MOVES: "#94a3b8",
    "Compensatory improvement": "#4ade80",
    "Direct improvement": "#facc15",
}

ROOT_COLOR = "#334155"

FLOW_CALLOUT_SHORT_LABELS = {
    OTHER_FACTOR_CHANGING_NEUTRAL_MOVES: "Other FC neutral moves",
}

DIRECT_IMPROVEMENT_LABEL = "Direct improvement"


def _callout_label_text(band: _FlowBand) -> str:
    label = FLOW_CALLOUT_SHORT_LABELS.get(band.label, band.label)
    return f"{label}  {band.count} ({band.pct:.1f}%)"


@dataclass(frozen=True)
class BreakdownEntry:
    label: str
    count: int
    pct_of_all_accepted: float


@dataclass(frozen=True)
class BreakdownGroup:
    label: str
    count: int
    pct_of_all_accepted: float
    children: tuple[BreakdownEntry, ...]


@dataclass(frozen=True)
class AcceptedMovesBreakdown:
    total: int
    groups: tuple[BreakdownGroup, ...]


def _pct(count: int, total: int) -> float:
    return 100.0 * count / total if total else 0.0


def build_accepted_moves_breakdown(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> AcceptedMovesBreakdown:
    total = count_accepted_genome_changes(histories)
    factor_neutral = collect_factor_neutral_accepted_events(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    factor_changing = collect_factor_changing_neutral_events(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    fitness_improving = collect_fitness_improving_events(
        histories, contexts, n_genes=n_genes, labels=labels
    )

    factor_neutral_children: list[BreakdownEntry] = []
    for category in CATEGORY_ORDER:
        count = sum(1 for event in factor_neutral if event.category == category)
        if count == 0:
            continue
        factor_neutral_children.append(
            BreakdownEntry(
                FACTOR_NEUTRAL_BREAKDOWN_LABELS[category],
                count,
                _pct(count, total),
            )
        )

    factor_changing_children: list[BreakdownEntry] = []
    for _group_key, label, categories in FACTOR_CHANGING_CHART_GROUPS:
        count = sum(1 for event in factor_changing if event.category in categories)
        if count == 0:
            continue
        factor_changing_children.append(
            BreakdownEntry(
                label,
                count,
                _pct(count, total),
            )
        )

    fitness_children: list[BreakdownEntry] = []
    for category in FITNESS_IMPROVING_CATEGORY_ORDER:
        count = sum(1 for event in fitness_improving if event.category == category)
        if count == 0:
            continue
        fitness_children.append(
            BreakdownEntry(
                FITNESS_IMPROVING_CATEGORY_LABELS[category],
                count,
                _pct(count, total),
            )
        )

    groups = (
        BreakdownGroup(
            FACTOR_NEUTRAL_ACCEPTED_MOVES,
            len(factor_neutral),
            _pct(len(factor_neutral), total),
            tuple(factor_neutral_children),
        ),
        BreakdownGroup(
            FACTOR_CHANGING_NEUTRAL_ACCEPTED_MOVES,
            len(factor_changing),
            _pct(len(factor_changing), total),
            tuple(factor_changing_children),
        ),
        BreakdownGroup(
            FITNESS_IMPROVING_ACCEPTED_MOVES,
            len(fitness_improving),
            _pct(len(fitness_improving), total),
            tuple(fitness_children),
        ),
    )
    return AcceptedMovesBreakdown(total=total, groups=groups)


def build_accepted_moves_hierarchy_rows(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    *,
    n_genes: int,
    labels: list[str] | None = None,
) -> tuple[list[str], list[list[str | int | float]]]:
    breakdown = build_accepted_moves_breakdown(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    headers = ["group", "count", "pct_of_all_accepted"]
    rows: list[list[str | int | float]] = []

    for group in breakdown.groups:
        rows.append([group.label, group.count, group.pct_of_all_accepted])
        for child in group.children:
            rows.append([f"  {child.label}", child.count, child.pct_of_all_accepted])

    rows.append(["All accepted genome changes", breakdown.total, 100.0 if breakdown.total else 0.0])
    return headers, rows


def _append_child_branches(
    lines: list[str],
    *,
    prefix: str,
    children: tuple[BreakdownEntry, ...],
) -> None:
    for index, child in enumerate(children):
        is_last = index == len(children) - 1
        connector = "└── " if is_last else "├── "
        lines.append(f"{prefix}{connector}{child.label}: {child.pct_of_all_accepted:.1f}%")


def build_accepted_moves_breakdown_lines(
    breakdown: AcceptedMovesBreakdown,
) -> list[str]:
    lines = ["All accepted genome changes", "│"]
    for group_index, group in enumerate(breakdown.groups):
        is_last_group = group_index == len(breakdown.groups) - 1
        connector = "└── " if is_last_group else "├── "
        lines.append(f"{connector}{group.label}: {group.pct_of_all_accepted:.1f}%")
        child_prefix = "    " if is_last_group else "│   "
        _append_child_branches(lines, prefix=child_prefix, children=group.children)
    return lines


def _write_breakdown_txt(lines: list[str], txt_path: Path) -> None:
    txt_path.parent.mkdir(parents=True, exist_ok=True)
    txt_path.write_text("\n".join(lines) + "\n", encoding="utf-8")


@dataclass(frozen=True)
class _FlowBand:
    label: str
    count: int
    pct: float
    color: str
    y_bottom: float
    y_top: float

    @property
    def y_center(self) -> float:
        return (self.y_bottom + self.y_top) / 2.0


def _layout_bands(
    items: list[tuple[str, int, float, str]],
    *,
    y_min: float,
    y_max: float,
) -> list[_FlowBand]:
    total = sum(count for _, count, _, _ in items)
    span = y_max - y_min
    y_cursor = y_max
    bands: list[_FlowBand] = []
    for label, count, pct, color in items:
        height = span * count / total if total else 0.0
        bands.append(
            _FlowBand(
                label=label,
                count=count,
                pct=pct,
                color=color,
                y_bottom=y_cursor - height,
                y_top=y_cursor,
            )
        )
        y_cursor -= height
    return bands


def _layout_bands_with_minimum_height(
    items: list[tuple[str, int, float, str]],
    *,
    y_min: float,
    y_max: float,
    min_band_height: float = LEAF_MIN_BAND_HEIGHT,
) -> list[_FlowBand]:
    """Layout bands with a visual floor so each leaf fits one callout line."""
    if not items:
        return []

    span = y_max - y_min
    item_count = len(items)
    total = sum(count for _, count, _, _ in items)

    if min_band_height * item_count >= span:
        heights = [span / item_count] * item_count
    else:
        extra_span = span - min_band_height * item_count
        heights = [
            min_band_height
            + (extra_span * count / total if total else extra_span / item_count)
            for _, count, _, _ in items
        ]

    y_cursor = y_max
    bands: list[_FlowBand] = []
    for (label, count, pct, color), height in zip(items, heights):
        bands.append(
            _FlowBand(
                label=label,
                count=count,
                pct=pct,
                color=color,
                y_bottom=y_cursor - height,
                y_top=y_cursor,
            )
        )
        y_cursor -= height
    return bands


def _layout_group_bands_with_leaf_minimums(
    groups: tuple[BreakdownGroup, ...],
    *,
    y_min: float,
    y_max: float,
    min_leaf_height: float = LEAF_MIN_BAND_HEIGHT,
) -> list[_FlowBand]:
    """Size move-type bands so each group's subcategories can use a readable min height."""
    active_groups = [group for group in groups if group.count > 0]
    if not active_groups:
        return []

    span = y_max - y_min
    total_count = sum(group.count for group in active_groups)
    min_spans = [
        min_leaf_height * sum(1 for child in group.children if child.count > 0)
        for group in active_groups
    ]
    total_min = sum(min_spans)

    if total_min >= span:
        heights = [
            span * group.count / total_count if total_count else span / len(active_groups)
            for group in active_groups
        ]
    else:
        extra = span - total_min
        heights = [
            min_span
            + (extra * group.count / total_count if total_count else extra / len(active_groups))
            for min_span, group in zip(min_spans, active_groups)
        ]

    y_cursor = y_max
    bands: list[_FlowBand] = []
    for group, height in zip(active_groups, heights):
        bands.append(
            _FlowBand(
                label=group.label,
                count=group.count,
                pct=group.pct_of_all_accepted,
                color=GROUP_COLORS[group.label],
                y_bottom=y_cursor - height,
                y_top=y_cursor,
            )
        )
        y_cursor -= height
    return bands


def _draw_aligned_flow_ribbons(
    axis,
    x0: float,
    x1: float,
    leaf_bands: list[_FlowBand],
    *,
    node_width: float,
) -> None:
    for child_band in leaf_bands:
        _draw_flow_ribbon(
            axis,
            x0 + node_width / 2.0,
            child_band.y_bottom,
            child_band.y_top,
            x1 - node_width / 2.0,
            child_band.y_bottom,
            child_band.y_top,
            child_band.color,
        )


def _draw_flow_ribbon(
    axis,
    x0: float,
    y0_bottom: float,
    y0_top: float,
    x1: float,
    y1_bottom: float,
    y1_top: float,
    color: str,
    *,
    alpha: float = 0.42,
) -> None:
    axis.add_patch(
        Polygon(
            [
                (x0, y0_top),
                (x1, y1_top),
                (x1, y1_bottom),
                (x0, y0_bottom),
            ],
            closed=True,
            facecolor=color,
            edgecolor="none",
            alpha=alpha,
        )
    )


def _draw_flow_node(
    axis,
    x: float,
    band: _FlowBand,
    *,
    node_width: float,
    label_side: str,
    label_y: float | None = None,
    draw_label: bool = True,
) -> None:
    band_height = band.y_top - band.y_bottom
    axis.add_patch(
        FancyBboxPatch(
            (x - node_width / 2.0, band.y_bottom),
            node_width,
            band_height,
            boxstyle="round,pad=0.002,rounding_size=0.012",
            linewidth=1.0,
            edgecolor="white",
            facecolor=band.color,
            zorder=3,
        )
    )
    if not draw_label:
        return

    label = f"{band.label}\n{band.count} ({band.pct:.1f}%)"
    if label_side == "left":
        axis.text(
            x - node_width / 2.0 - 0.022,
            label_y if label_y is not None else band.y_center,
            label,
            ha="right",
            va="center",
            fontsize=9,
            color="#0f172a",
            linespacing=1.25,
            zorder=5,
        )
    elif label_side == "right":
        axis.text(
            x + node_width / 2.0 + 0.015,
            label_y if label_y is not None else band.y_center,
            label,
            ha="left",
            va="center",
            fontsize=8.5,
            color="#0f172a",
            linespacing=1.2,
            zorder=5,
        )
    else:
        if band_height < 0.05:
            return
        axis.text(
            x,
            band.y_center,
            label,
            ha="center",
            va="center",
            fontsize=9,
            color="#0f172a",
            fontweight="bold",
            linespacing=1.2,
            zorder=5,
        )


def _draw_callout_connector(
    axis,
    band: _FlowBand,
    *,
    connector_x: float,
    label_x: float,
    band_y: float,
    label_y: float,
) -> None:
    label_edge_x = label_x - 0.006

    if band.label == DIRECT_IMPROVEMENT_LABEL:
        axis.plot(
            [connector_x, label_edge_x],
            [band_y, band_y],
            color="#64748b",
            linewidth=0.9,
            zorder=4,
        )
        return

    if abs(label_y - band_y) < 0.016:
        axis.plot(
            [connector_x, label_edge_x],
            [band_y, label_y],
            color="#64748b",
            linewidth=0.9,
            zorder=4,
        )
        return

    elbow_x = connector_x + 0.016
    axis.plot(
        [connector_x, elbow_x, elbow_x, label_edge_x],
        [band_y, band_y, label_y, label_y],
        color="#64748b",
        linewidth=0.9,
        zorder=4,
    )


def _draw_flow_leaf_callouts(
    axis,
    x: float,
    bands: list[_FlowBand],
    label_positions: list[float],
    *,
    node_width: float,
) -> None:
    if not bands:
        return

    label_x = x + node_width / 2.0 + 0.045
    connector_x = x + node_width / 2.0 + 0.004
    for band, label_y in zip(bands, label_positions):
        _draw_flow_node(
            axis,
            x,
            band,
            node_width=node_width,
            label_side="right",
            draw_label=False,
        )
        _draw_callout_connector(
            axis,
            band,
            connector_x=connector_x,
            label_x=label_x,
            band_y=band.y_center,
            label_y=label_y,
        )
        axis.text(
            label_x,
            label_y,
            _callout_label_text(band),
            ha="left",
            va="center",
            fontsize=8.5,
            color="#0f172a",
            linespacing=1.15,
            zorder=5,
        )


def _plot_accepted_moves_flow_diagram(
    figure,
    axis,
    breakdown: AcceptedMovesBreakdown,
    *,
    include_subcategories: bool = True,
) -> None:
    y_min, y_max = 0.06, 0.94
    node_width = 0.028

    root_band = _FlowBand(
        label="All accepted\ngenome changes",
        count=breakdown.total,
        pct=100.0 if breakdown.total else 0.0,
        color=ROOT_COLOR,
        y_bottom=y_min,
        y_top=y_max,
    )

    group_items = [
        (
            group.label,
            group.count,
            group.pct_of_all_accepted,
            GROUP_COLORS[group.label],
        )
        for group in breakdown.groups
        if group.count > 0
    ]
    active_groups = [group for group in breakdown.groups if group.count > 0]
    if include_subcategories:
        group_bands = _layout_group_bands_with_leaf_minimums(
            breakdown.groups,
            y_min=y_min,
            y_max=y_max,
        )
    else:
        group_bands = _layout_bands(group_items, y_min=y_min, y_max=y_max)

    if include_subcategories:
        x_root, x_group, x_leaf = 0.11, 0.46, 0.82
    else:
        x_root, x_group = 0.24, 0.76

    for group_band in group_bands:
        _draw_flow_ribbon(
            axis,
            x_root + node_width / 2.0,
            group_band.y_bottom,
            group_band.y_top,
            x_group - node_width / 2.0,
            group_band.y_bottom,
            group_band.y_top,
            group_band.color,
        )

    if include_subcategories:
        leaf_bands: list[_FlowBand] = []

        for group, group_band in zip(active_groups, group_bands):
            child_items = [
                (
                    child.label,
                    child.count,
                    child.pct_of_all_accepted,
                    CHILD_COLORS.get(child.label, GROUP_COLORS[group.label]),
                )
                for child in group.children
                if child.count > 0
            ]
            group_leaf_bands = _layout_bands_with_minimum_height(
                child_items,
                y_min=group_band.y_bottom,
                y_max=group_band.y_top,
            )
            leaf_bands.extend(group_leaf_bands)
            _draw_aligned_flow_ribbons(
                axis,
                x_group,
                x_leaf,
                group_leaf_bands,
                node_width=node_width,
            )

    axis.text(x_root, 0.985, "All accepted", ha="center", va="top", fontsize=10, color="#475569")
    axis.text(x_group, 0.985, "Move type", ha="center", va="top", fontsize=10, color="#475569")
    if include_subcategories:
        axis.text(
            x_leaf + 0.08,
            0.985,
            "Subcategory",
            ha="center",
            va="top",
            fontsize=10,
            color="#475569",
        )
        axis.set_xlim(0.0, 1.14)
    else:
        axis.set_xlim(0.0, 1.0)
    axis.set_ylim(0.0, 1.0)
    axis.axis("off")

    if include_subcategories:
        label_x = x_leaf + node_width / 2.0 + 0.045
        leaf_label_positions = [band.y_center for band in leaf_bands]
        _draw_flow_leaf_callouts(
            axis,
            x_leaf,
            leaf_bands,
            leaf_label_positions,
            node_width=node_width,
        )

    _draw_flow_node(axis, x_root, root_band, node_width=node_width, label_side="left")
    for group_band in group_bands:
        _draw_flow_node(axis, x_group, group_band, node_width=node_width, label_side="left")


def _plot_single_group_flow_diagram(
    figure,
    axis,
    group: BreakdownGroup,
    *,
    group_color: str,
) -> None:
    y_min, y_max = 0.06, 0.94
    node_width = 0.034
    x_root, x_leaf = 0.18, 0.78

    root_band = _FlowBand(
        label=group.label,
        count=group.count,
        pct=group.pct_of_all_accepted,
        color=group_color,
        y_bottom=y_min,
        y_top=y_max,
    )

    child_items = [
        (
            child.label,
            child.count,
            child.pct_of_all_accepted,
            CHILD_COLORS.get(child.label, group_color),
        )
        for child in group.children
        if child.count > 0
    ]
    leaf_bands = _layout_bands_with_minimum_height(child_items, y_min=y_min, y_max=y_max)

    _draw_aligned_flow_ribbons(
        axis,
        x_root,
        x_leaf,
        leaf_bands,
        node_width=node_width,
    )

    axis.text(x_root, 0.985, "Move type", ha="center", va="top", fontsize=10, color="#475569")
    axis.text(
        x_leaf + 0.08,
        0.985,
        "Subcategory",
        ha="center",
        va="top",
        fontsize=10,
        color="#475569",
    )
    axis.set_xlim(0.0, 1.14)
    axis.set_ylim(0.0, 1.0)
    axis.axis("off")

    label_x = x_leaf + node_width / 2.0 + 0.045
    leaf_label_positions = [band.y_center for band in leaf_bands]
    _draw_flow_leaf_callouts(
        axis,
        x_leaf,
        leaf_bands,
        leaf_label_positions,
        node_width=node_width,
    )
    _draw_flow_node(axis, x_root, root_band, node_width=node_width, label_side="left")


def plot_accepted_moves_category_flow_diagrams(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    *,
    factor_neutral_plot: str | Path,
    factor_changing_neutral_plot: str | Path,
    fitness_improving_plot: str | Path,
    labels: list[str] | None = None,
) -> tuple[Path, Path, Path]:
    if plt is None or FancyBboxPatch is None or Polygon is None:
        raise ImportError("matplotlib is required for hierarchy flow diagrams")

    breakdown = build_accepted_moves_breakdown(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    plot_specs = (
        (FACTOR_NEUTRAL_ACCEPTED_MOVES, factor_neutral_plot, GROUP_COLORS[FACTOR_NEUTRAL_ACCEPTED_MOVES]),
        (
            FACTOR_CHANGING_NEUTRAL_ACCEPTED_MOVES,
            factor_changing_neutral_plot,
            GROUP_COLORS[FACTOR_CHANGING_NEUTRAL_ACCEPTED_MOVES],
        ),
        (
            FITNESS_IMPROVING_ACCEPTED_MOVES,
            fitness_improving_plot,
            GROUP_COLORS[FITNESS_IMPROVING_ACCEPTED_MOVES],
        ),
    )
    saved_paths: list[Path] = []
    groups_by_label = {group.label: group for group in breakdown.groups}

    for group_label, save_path, group_color in plot_specs:
        group = groups_by_label[group_label]
        save_path = Path(save_path)
        save_path.parent.mkdir(parents=True, exist_ok=True)

        figure, axis = plt.subplots(figsize=(12.5, 8.5))
        _plot_single_group_flow_diagram(
            figure,
            axis,
            group,
            group_color=group_color,
        )
        figure.suptitle(
            f"{group.label}\n"
            f"{len(histories)} runs  |  {group.count} moves ({group.pct_of_all_accepted:.1f}% of all accepted)",
            fontsize=13,
            y=0.98,
        )
        figure.subplots_adjust(left=0.02, right=0.98, top=0.90, bottom=0.03)
        figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.2)
        plt.close(figure)
        saved_paths.append(save_path)

    return tuple(saved_paths)  # type: ignore[return-value]


def plot_accepted_moves_hierarchy_flow(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
    csv_path: str | Path | None = None,
    txt_path: str | Path | None = None,
) -> Path:
    if plt is None or FancyBboxPatch is None or Polygon is None:
        raise ImportError("matplotlib is required for hierarchy flow diagrams")

    breakdown = build_accepted_moves_breakdown(
        histories, contexts, n_genes=n_genes, labels=labels
    )
    lines = build_accepted_moves_breakdown_lines(breakdown)

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    if csv_path is not None:
        headers, rows = build_accepted_moves_hierarchy_rows(
            histories, contexts, n_genes=n_genes, labels=labels
        )
        csv_path = Path(csv_path)
        csv_path.parent.mkdir(parents=True, exist_ok=True)
        with csv_path.open("w", encoding="utf-8", newline="") as handle:
            writer = csv.writer(handle)
            writer.writerow(headers)
            writer.writerows(rows)

    if txt_path is not None:
        _write_breakdown_txt(lines, Path(txt_path))

    figure, axis = plt.subplots(figsize=(16.5, 9.5))
    _plot_accepted_moves_flow_diagram(figure, axis, breakdown, include_subcategories=True)
    figure.suptitle(
        "Classification of Accepted Evolutionary Moves\n"
        f"{len(histories)} runs  |  {breakdown.total} accepted genome changes",
        fontsize=13,
        y=0.98,
    )
    figure.subplots_adjust(left=0.02, right=0.98, top=0.92, bottom=0.03)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.2)
    plt.close(figure)
    return save_path


def plot_accepted_moves_hierarchy_flow_summary(
    histories: list[EvolutionHistory],
    contexts: Contexts,
    n_genes: int,
    save_path: str | Path,
    *,
    labels: list[str] | None = None,
) -> Path:
    if plt is None or FancyBboxPatch is None or Polygon is None:
        raise ImportError("matplotlib is required for hierarchy flow diagrams")

    breakdown = build_accepted_moves_breakdown(
        histories, contexts, n_genes=n_genes, labels=labels
    )

    save_path = Path(save_path)
    save_path.parent.mkdir(parents=True, exist_ok=True)

    figure, axis = plt.subplots(figsize=(11.0, 9.0))
    _plot_accepted_moves_flow_diagram(
        figure, axis, breakdown, include_subcategories=False
    )
    figure.suptitle(
        f"Accepted mutation candidates by move type across {len(histories)} runs",
        fontsize=13,
        y=0.98,
    )
    figure.subplots_adjust(left=0.02, right=0.98, top=0.92, bottom=0.03)
    figure.savefig(save_path, bbox_inches="tight", dpi=200, pad_inches=0.2)
    plt.close(figure)
    return save_path


plot_accepted_moves_hierarchy_breakdown = plot_accepted_moves_hierarchy_flow
plot_accepted_moves_hierarchy_table = plot_accepted_moves_hierarchy_flow
