from typing import Dict, List, Tuple

from rbn.types import Genome, State, TransitionTable, TruthTable
from rbn.truth_table import genome_to_truth_table, truth_table_to_dict


def rotate_left(s: str, n: int = 1) -> str:
    """Rotate a binary state string to the left by n positions."""
    n = n % len(s)
    return s[n:] + s[:n]


def next_state_from_rules(state: State, truth_dict: Dict[State, State]) -> State:
    """
    Compute the next state using the rotated-input rule.

    Important:
    This does NOT simply do next_state = truth_dict[state].

    Instead, each gene reads the current state from its own perspective.
    For a 5-gene state ABCDE:
        gene A uses row ABCDE and takes output column A
        gene B uses row BCDEA and takes output column B
        gene C uses row CDEAB and takes output column C
        gene D uses row DEABC and takes output column D
        gene E uses row EABCD and takes output column E

    The five selected output bits are combined into the next state.
    """
    n_genes = len(state)
    next_bits: List[str] = []

    for col in range(n_genes):
        rotated_state = rotate_left(state, col)
        output_bits = truth_dict[rotated_state]
        next_bits.append(output_bits[col])

    return "".join(next_bits)


def build_transition_table_from_truth_table(truth_table: TruthTable) -> TransitionTable:
    """Build transition table from a truth table."""
    truth_dict = truth_table_to_dict(truth_table)
    transition_table: TransitionTable = {}

    for state in truth_dict.keys():
        transition_table[state] = next_state_from_rules(state, truth_dict)

    return transition_table


def build_transition_table_from_genome(
    genome: Genome,
    n_genes: int = 5,
) -> Tuple[TransitionTable, TruthTable, Dict[State, State]]:
    """Build transition table, truth table, and truth dictionary from a genome."""
    truth_table = genome_to_truth_table(genome, n_genes)
    truth_dict = truth_table_to_dict(truth_table)
    transition_table = build_transition_table_from_truth_table(truth_table)
    return transition_table, truth_table, truth_dict
