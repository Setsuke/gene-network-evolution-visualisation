import random
from typing import List

from rbn.transition_table import build_transition_table_from_genome
from rbn.fitness import build_factor_scores, compute_total_fitness
from rbn.types import Contexts, GenerationRecord, Genome


def mutate_genome(genome: Genome, mutation_probability: float) -> Genome:
    """
    Mutate a binary genome.

    Each bit has probability p of flipping:
        0 -> 1
        1 -> 0
    """
    if not 0.0 <= mutation_probability <= 1.0:
        raise ValueError("mutation_probability must be between 0 and 1")

    mutated_bits: List[str] = []

    for bit in genome:
        if random.random() < mutation_probability:
            mutated_bits.append("1" if bit == "0" else "0")
        else:
            mutated_bits.append(bit)

    return "".join(mutated_bits)


def count_changed_bits(genome_a: Genome, genome_b: Genome) -> int:
    """Count how many bits differ between two genomes."""
    return sum(1 for a, b in zip(genome_a, genome_b) if a != b)


def evolve_genome(
    initial_genome: Genome,
    contexts: Contexts,
    mutation_probability: float = 0.1,
    generations: int = 100,
    n_genes: int = 5,
    stop_at_perfect_fitness: bool = True,
) -> List[GenerationRecord]:
    """
    Evolve a genome using mutation and selection.
    At each generation:
        1. Mutate current genome to create candidate genome.
        2. Build candidate transition table.
        3. Compute candidate fitness.
        4. Accept candidate only if candidate fitness >= current fitness.
    """
    current_genome = initial_genome

    current_transition_table, _, _ = build_transition_table_from_genome(
        current_genome, n_genes
    )

    current_fitness, current_details = compute_total_fitness(
        current_transition_table, contexts
    )

    history: List[GenerationRecord] = [
        {
            "generation": 0,
            "genome": current_genome,
            "fitness": current_fitness,
            "accepted": True,
            "changed_bits": 0,
            "transition_table": current_transition_table,
            "details": current_details,
            "factor_scores": build_factor_scores(current_details, contexts),
        }
    ]

    for generation in range(1, generations + 1):
        candidate_genome = mutate_genome(current_genome, mutation_probability)
        changed_bits = count_changed_bits(current_genome, candidate_genome)

        candidate_transition_table, _, _ = build_transition_table_from_genome(
            candidate_genome, n_genes
        )

        candidate_fitness, candidate_details = compute_total_fitness(
            candidate_transition_table, contexts
        )

        accepted = candidate_fitness >= current_fitness

        if accepted:
            current_genome = candidate_genome
            current_fitness = candidate_fitness
            current_transition_table = candidate_transition_table
            current_details = candidate_details

        history.append(
            {
                "generation": generation,
                "genome": current_genome,
                "fitness": current_fitness,
                "accepted": accepted,
                "changed_bits": changed_bits,
                "transition_table": current_transition_table,
                "details": current_details,
                "factor_scores": build_factor_scores(current_details, contexts),
            }
        )

        if stop_at_perfect_fitness and current_fitness == 1.0:
            print(f"Perfect fitness found at generation {generation}")
            break

    return history
