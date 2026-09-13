"""Shared helpers for accepted evolutionary trajectories and genome distance."""

from __future__ import annotations

from rbn.types import Genome, TransitionTable


def hamming_distance(left: Genome, right: Genome) -> int:
    return sum(bit_left != bit_right for bit_left, bit_right in zip(left, right))


def count_changed_transitions(
    previous: TransitionTable,
    current: TransitionTable,
) -> int:
    return sum(1 for state in previous if previous[state] != current[state])
