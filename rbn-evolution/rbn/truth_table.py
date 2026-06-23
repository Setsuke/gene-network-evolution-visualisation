from itertools import product
import random
from typing import Dict, List

from rbn.types import TruthTable, Genome, State


def generate_random_truth_table(n_genes: int = 5) -> TruthTable:
    """
    Generate a random truth table.

    Genome length = 2^n_genes * n_genes bits.
    """
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")

    truth_table: TruthTable = []
    all_inputs = list(product([0, 1], repeat=n_genes))

    for inp in all_inputs:
        out = tuple(random.randint(0, 1) for _ in range(n_genes))
        truth_table.append({"input": inp, "output": out})

    return truth_table


def print_truth_table(truth_table: TruthTable) -> None:
    print("\nTRUTH TABLE:")
    for row in truth_table:
        inp = "".join(map(str, row["input"]))
        out = "".join(map(str, row["output"]))
        print(f"{inp} -> {out}")


def truth_table_to_genome(truth_table: TruthTable) -> Genome:
    """
    Convert the truth table outputs into a single binary genome string.
    """
    genome_bits: List[str] = []

    for row in truth_table:
        genome_bits.extend(map(str, row["output"]))

    return "".join(genome_bits)


def genome_to_truth_table(
    genome: Genome,
    n_genes: int = 5,
) -> TruthTable:
    """
    Convert a binary genome string back into a truth table.

    The input rows are always all possible binary states in lexicographic order.
    The output rows are taken from the genome.
    """
    if n_genes < 1:
        raise ValueError("n_genes must be at least 1")

    expected_length = (2 ** n_genes) * n_genes

    if len(genome) != expected_length:
        raise ValueError(
            f"Genome length should be {expected_length}, but got {len(genome)}"
        )

    if any(bit not in "01" for bit in genome):
        raise ValueError("Genome must contain only 0 and 1 characters")

    all_inputs = list(product([0, 1], repeat=n_genes))
    truth_table: TruthTable = []
    index = 0

    for inp in all_inputs:
        output_bits = tuple(int(bit) for bit in genome[index:index + n_genes])
        index += n_genes
        truth_table.append({"input": inp, "output": output_bits})

    return truth_table


def truth_table_to_dict(truth_table: TruthTable) -> Dict[State, State]:
    """
    Convert the truth table into a dictionary:
        input_state_string -> output_state_string
    """
    truth_dict: Dict[State, State] = {}

    for row in truth_table:
        inp = "".join(map(str, row["input"]))
        out = "".join(map(str, row["output"]))
        truth_dict[inp] = out

    return truth_dict