from typing import Dict, List, Tuple

from rbn.types import State, TransitionTable


def trace_path_until_cycle(
    start_state: State,
    transition_table: TransitionTable,
    max_steps: int | None = None,
) -> Tuple[List[State], List[State]]:
    """
    Follow the dynamics from a start state until a state repeats.

    Returns:
        path: all states visited before repetition is detected
        cycle: the repeated attractor cycle
    """
    if max_steps is None:
        max_steps = len(transition_table)

    path: List[State] = []
    seen: Dict[State, int] = {}
    state = start_state

    for _ in range(max_steps):
        if state in seen:
            cycle_start = seen[state]
            cycle = path[cycle_start:]
            return path, cycle

        seen[state] = len(path)
        path.append(state)
        state = transition_table[state]

    return path, []


def find_attractors(
    transition_table: TransitionTable,
) -> Tuple[List[State], List[List[State]]]:
    """
    Find point attractors and limit cycles in the transition table.
    """
    visited_global = set()
    point_attractors: List[State] = []
    limit_cycles: List[List[State]] = []

    for start_state in transition_table.keys():
        if start_state in visited_global:
            continue

        path: List[State] = []
        seen_in_path: Dict[State, int] = {}
        state = start_state

        while True:
            if state in seen_in_path:
                cycle_start = seen_in_path[state]
                cycle = path[cycle_start:]

                if len(cycle) == 1:
                    point_attractors.append(cycle[0])
                else:
                    limit_cycles.append(cycle)
                break

            if state in visited_global:
                break

            seen_in_path[state] = len(path)
            path.append(state)
            visited_global.add(state)
            state = transition_table[state]

    return point_attractors, limit_cycles


def print_attractors(
    point_attractors: List[State],
    limit_cycles: List[List[State]],
) -> None:
    print("\nPOINT ATTRACTORS:")
    if point_attractors:
        for state in point_attractors:
            print(state)
    else:
        print("None")

    print("\nLIMIT CYCLES:")
    if limit_cycles:
        for cycle in limit_cycles:
            print(" -> ".join(cycle), "->", cycle[0])
    else:
        print("None")
