from typing import Dict, List, Tuple, TypedDict


# A state is a binary string such as: "10101"
State = str


# A genome is a long binary string containing all truth table outputs.
# For n = 5:
# 32 rows * 5 output bits = 160 bits
Genome = str


class TruthTableRow(TypedDict):
    """
    One row in the truth table.

    Example:
        input:  (0, 0, 0, 0, 0)
        output: (1, 0, 1, 1, 0)
    """
    input: Tuple[int, ...]
    output: Tuple[int, ...]


# A full truth table is a list of truth table rows
TruthTable = List[TruthTableRow]


# A transition table maps each current state to its next state
TransitionTable = Dict[State, State]


# A context maps an initial state to a target state
# Example:
# {
#     "00000": "01010",
#     "10000": "10101",
# }
Contexts = Dict[State, State]


class ContextDetails(TypedDict):
    """
    Information about the lifetime dynamics for one context.
    """
    target: State
    path: List[State]
    cycle: List[State]
    fitness: float
    gene_scores: List[float]
    cycle_length: int


# Details for all contexts
# Example:
# {
#     "00000": {
#         "target": "01010",
#         "path": ["00000", "01011", "10101"],
#         "cycle": ["10101"],
#         "fitness": 1.0,
#     }
# }
AllContextDetails = Dict[State, ContextDetails]


class GenerationRecord(TypedDict):
    """
    Information stored for one evolutionary generation.

    genome/fitness/transition_table/details always describe the best accepted
    network so far. candidate_fitness/changed_bits describe the latest attempt.
    """
    generation: int
    genome: Genome
    fitness: float
    accepted: bool
    changed_bits: int
    candidate_fitness: float
    transition_table: TransitionTable
    details: AllContextDetails


# Full evolution history
EvolutionHistory = List[GenerationRecord]