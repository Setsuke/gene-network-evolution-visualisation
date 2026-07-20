"""Shared helpers for accepted genome change taxonomy."""

from __future__ import annotations

from typing import Iterator, Literal

from rbn.fitness import build_factor_scores
from rbn.types import Contexts, EvolutionHistory, GenerationRecord

TopLevelMoveCategory = Literal[
    "factor_neutral",
    "factor_changing_neutral",
    "direct_improvement",
    "compensatory_improvement",
]

TOP_LEVEL_MOVE_CATEGORY_ORDER: tuple[TopLevelMoveCategory, ...] = (
    "factor_neutral",
    "factor_changing_neutral",
    "direct_improvement",
    "compensatory_improvement",
)

TOP_LEVEL_MOVE_CATEGORY_LABELS: dict[TopLevelMoveCategory, str] = {
    "factor_neutral": "Factor-neutral",
    "factor_changing_neutral": "Factor-changing neutral",
    "direct_improvement": "Direct improvement",
    "compensatory_improvement": "Compensatory improvement",
}


def iter_accepted_change_records(
    history: EvolutionHistory,
) -> Iterator[tuple[int, GenerationRecord, GenerationRecord]]:
    previous = history[0]
    accepted_step = 0

    for record in history[1:]:
        if record["genome"] == previous["genome"]:
            continue
        accepted_step += 1
        yield accepted_step, previous, record
        previous = record
        if record["fitness"] == 1.0:
            break

    if (
        history[-1]["genome"] != previous["genome"]
        and int(history[-1]["generation"]) != int(previous["generation"])
    ):
        accepted_step += 1
        yield accepted_step, previous, history[-1]


def count_accepted_genome_changes(histories: list[EvolutionHistory]) -> int:
    return sum(len(list(iter_accepted_change_records(history))) for history in histories)


def factor_score_deltas(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> list[float]:
    previous_scores = previous.get("factor_scores")
    current_scores = current.get("factor_scores")
    if previous_scores is None:
        previous_scores = build_factor_scores(previous["details"], contexts)
    if current_scores is None:
        current_scores = build_factor_scores(current["details"], contexts)
    return [
        float(current_score) - float(previous_score)
        for previous_score, current_score in zip(previous_scores, current_scores)
    ]


def factor_scores_unchanged(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> bool:
    return all(
        delta == 0.0 for delta in factor_score_deltas(previous, current, contexts)
    )


def is_fitness_neutral(
    previous: GenerationRecord,
    current: GenerationRecord,
) -> bool:
    return float(current["fitness"]) == float(previous["fitness"])


def is_fitness_improving(
    previous: GenerationRecord,
    current: GenerationRecord,
) -> bool:
    return float(current["fitness"]) > float(previous["fitness"])


def classify_top_level_accepted_move_category(
    previous: GenerationRecord,
    current: GenerationRecord,
    contexts: Contexts,
) -> TopLevelMoveCategory:
    if is_fitness_neutral(previous, current):
        if factor_scores_unchanged(previous, current, contexts):
            return "factor_neutral"
        return "factor_changing_neutral"

    if float(current["fitness"]) < float(previous["fitness"]):
        raise ValueError(
            "Accepted move with declining fitness is impossible under monotone acceptance"
        )

    deltas = factor_score_deltas(previous, current, contexts)
    if not deltas or all(delta == 0.0 for delta in deltas):
        raise ValueError(
            "Accepted move with improving fitness but unchanged factor scores is "
            "impossible because total fitness is the product of factor scores"
        )

    improved = any(delta > 0.0 for delta in deltas)
    worsened = any(delta < 0.0 for delta in deltas)

    if improved and worsened:
        return "compensatory_improvement"
    return "direct_improvement"
