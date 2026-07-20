"""Helpers for comparing dynamics between consecutive accepted genome changes."""

from __future__ import annotations

from rbn.transition_table import build_transition_table_from_genome
from rbn.types import Contexts, GenerationRecord, State, TransitionTable
from visualisation.factorised.metrics import context_order


def _transition_table_for_record(record: GenerationRecord, n_genes: int) -> TransitionTable:
    transition_table = record.get("transition_table") or {}
    if transition_table:
        return transition_table
    table, _, _ = build_transition_table_from_genome(record["genome"], n_genes)
    return table


def _attractor_cycle_tuple(record: GenerationRecord, initial: State) -> tuple[str, ...]:
    return tuple(record["details"][initial]["cycle"])


def _count_context_attractor_cycles_changed(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> int:
    changed = 0
    for initial in context_order(contexts):
        if _attractor_cycle_tuple(previous, initial) != _attractor_cycle_tuple(current, initial):
            changed += 1
    return changed
