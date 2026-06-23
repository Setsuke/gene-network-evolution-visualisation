"""2D matplotlib/networkx plots for attractor landscapes and fitness history."""

from __future__ import annotations

from collections import defaultdict
from pathlib import Path
from typing import Dict, List, Tuple

try:
    import matplotlib.pyplot as plt
    import networkx as nx
except ImportError:
    plt = None
    nx = None

from rbn.types import GenerationRecord, State, TransitionTable

# --- Style constants ---

POINT_COLOR = "tab:blue"
CYCLE_COLOR = "orange"
TRANSIENT_COLOR = "lightgray"

ATTRACTOR_NODE_SIZE = 450
TRANSIENT_NODE_SIZE = 320
LABEL_FONT_SIZE = 8
SPRING_K = 1.4
SPRING_SCALE = 1.15
PANEL_WIDTH = 5.5
PANEL_HEIGHT = 5.5


# --- Optional dependencies ---


def _require_matplotlib():
    if plt is None:
        raise ImportError("matplotlib is required for plotting")


def _require_networkx():
    if nx is None:
        raise ImportError("networkx is required for plotting")


# --- Figure I/O ---


def _save_or_show(figure, save_path: str | Path | None) -> None:
    if save_path is None:
        plt.show()
        return

    path = Path(save_path)
    path.parent.mkdir(parents=True, exist_ok=True)
    figure.savefig(path, bbox_inches="tight", dpi=200)
    plt.close(figure)


# --- Attractor basin grouping ---


def _groups_by_attractor(
    transition_table: TransitionTable,
) -> Tuple[nx.DiGraph, List[Tuple[Tuple[State, ...], List[State]]]]:
    """Group every state by the attractor cycle it eventually reaches."""
    graph = nx.DiGraph((state, nxt) for state, nxt in transition_table.items())

    cycles = [tuple(cycle) for cycle in nx.simple_cycles(graph)]
    cycles = sorted(cycles, key=lambda cycle: (len(cycle), cycle))
    node_to_cycle = {node: cycle for cycle in cycles for node in cycle}

    memo: Dict[State, Tuple[State, ...]] = {}

    def attractor(state: State) -> Tuple[State, ...]:
        path: List[State] = []
        current = state
        while current not in memo and current not in node_to_cycle:
            path.append(current)
            current = transition_table[current]

        cycle = memo[current] if current in memo else node_to_cycle[current]
        for node in path:
            memo[node] = cycle
        return cycle

    groups: Dict[Tuple[State, ...], List[State]] = defaultdict(list)
    for state in transition_table:
        groups[attractor(state)].append(state)

    basins = sorted(groups.items(), key=lambda item: (len(item[0]), item[0]))
    return graph, basins


def _panel_title(cycle: Tuple[State, ...]) -> str:
    title = f"Attractor length {len(cycle)}"
    if len(cycle) == 1:
        return f"{title}\n{cycle[0]}"
    chain = " → ".join(cycle + (cycle[0],))
    return f"{title}\n{chain}"


def _node_colors(node_list: List[State], cycle: Tuple[State, ...], attractor_nodes: set[State]) -> List[str]:
    attractor_color = POINT_COLOR if len(cycle) == 1 else CYCLE_COLOR
    return [
        attractor_color if node in attractor_nodes else TRANSIENT_COLOR
        for node in node_list
    ]


def _node_sizes(node_list: List[State], attractor_nodes: set[State]) -> List[int]:
    return [
        ATTRACTOR_NODE_SIZE if node in attractor_nodes else TRANSIENT_NODE_SIZE
        for node in node_list
    ]


def _draw_basin_panel(
    axis,
    graph: nx.DiGraph,
    cycle: Tuple[State, ...],
    nodes: List[State],
    *,
    label_attractors: bool,
) -> None:
    subgraph = graph.subgraph(nodes)
    layout = nx.spring_layout(subgraph, seed=1, k=SPRING_K, scale=SPRING_SCALE)
    attractor_nodes = set(cycle)
    node_list = list(subgraph.nodes())

    nx.draw(
        subgraph,
        layout,
        ax=axis,
        with_labels=True,
        nodelist=node_list,
        node_color=_node_colors(node_list, cycle, attractor_nodes),
        node_size=_node_sizes(node_list, attractor_nodes),
        arrows=True,
        arrowstyle="-|>",
        arrowsize=15,
        font_size=LABEL_FONT_SIZE,
        width=1.2,
    )

    if label_attractors:
        axis.set_title(_panel_title(cycle))
    axis.axis("off")


# --- Public plot entry points ---


def plot_attractor_landscape(
    transition_table: TransitionTable,
    title: str = "Attractor Landscape",
    save_path: str | Path | None = None,
    label_attractors: bool = True,
) -> None:
    """Draw one panel per attractor basin using a spring layout."""
    _require_matplotlib()
    _require_networkx()

    graph, basins = _groups_by_attractor(transition_table)
    panel_count = max(len(basins), 1)

    figure, axes = plt.subplots(
        1, panel_count, figsize=(PANEL_WIDTH * panel_count, PANEL_HEIGHT)
    )
    if panel_count == 1:
        axes = [axes]

    for axis, (cycle, nodes) in zip(axes, basins):
        _draw_basin_panel(
            axis, graph, cycle, nodes, label_attractors=label_attractors
        )

    if title:
        figure.suptitle(title)

    figure.tight_layout()
    _save_or_show(figure, save_path)


def plot_fitness_history(
    history: List[GenerationRecord],
    save_path: str | Path | None = None,
) -> None:
    """Plot accepted fitness over generations."""
    _require_matplotlib()

    generations = [record["generation"] for record in history]
    fitness_values = [record["fitness"] for record in history]

    figure, axis = plt.subplots(figsize=(10, 5))
    axis.plot(generations, fitness_values, marker="o", markersize=3)
    axis.set_xlabel("Generation")
    axis.set_ylabel("Fitness")
    axis.set_title("Fitness over Generations")
    axis.grid(True)

    figure.tight_layout()
    _save_or_show(figure, save_path)
