from typing import Dict, List, Tuple

from rbn.attractors import trace_path_until_cycle
from rbn.types import Contexts, State, TransitionTable


def build_factor_scores(
    context_details: Dict[State, Dict[str, object]],
    contexts: Contexts,
) -> List[float]:
    """
    Flat fitness-factor vector for one genome.

    Order: context 1 gene 0..n-1, context 2 gene 0..n-1, ...
    Each factor score is the per-gene score within its context.
    """
    factor_scores: List[float] = []
    for initial_state in contexts:
        gene_scores = context_details[initial_state]["gene_scores"]
        factor_scores.extend(float(score) for score in gene_scores)
    return factor_scores


def compute_gene_scores(cycle: List[State], target_state: State) -> List[float]:
    """
    Per-gene scores for one context attractor cycle.

    If target bit is 1, score = expression average in the cycle.
    If target bit is 0, score = 1 - expression average.
    """
    if not cycle:
        return [0.0] * len(target_state)

    cycle_length = len(cycle)
    gene_scores: List[float] = []

    for gene_index in range(len(target_state)):
        expression_average = (
            sum(int(state[gene_index]) for state in cycle) / cycle_length
        )
        target_bit = int(target_state[gene_index])

        if target_bit == 1:
            score = expression_average
        else:
            score = 1.0 - expression_average

        gene_scores.append(score)

    return gene_scores


def compute_context_fitness(cycle: List[State], target_state: State) -> float:
    """
    Compute fitness for one context.

    The final score is the product of all gene-level scores.
    """
    gene_scores = compute_gene_scores(cycle, target_state)
    fitness = 1.0
    for score in gene_scores:
        fitness *= score
    return fitness


def compute_total_fitness(
    transition_table: TransitionTable,
    contexts: Contexts,
) -> Tuple[float, Dict[State, Dict[str, object]]]:
    """
    Compute total fitness across all contexts.

    contexts example:
        {
            "00000": "01010",
            "10000": "10101"
        }
    """
    total_fitness = 1.0
    context_details: Dict[State, Dict[str, object]] = {}

    for initial_state, target_state in contexts.items():
        path, cycle = trace_path_until_cycle(initial_state, transition_table)
        context_fitness = compute_context_fitness(cycle, target_state)

        context_details[initial_state] = {
            "target": target_state,
            "path": path,
            "cycle": cycle,
            "fitness": context_fitness,
            "gene_scores": compute_gene_scores(cycle, target_state),
            "cycle_length": len(cycle),
        }

        total_fitness *= context_fitness

    return total_fitness, context_details
