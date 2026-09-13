"""Helpers for context-level fitness derived from per-gene factor scores."""

from __future__ import annotations

import math


def context_fitness_from_factor_scores(factor_scores: list[float], n_genes: int = 5) -> tuple[float, float]:
    if len(factor_scores) < 2 * n_genes:
        raise ValueError(
            f"expected at least {2 * n_genes} factor scores, got {len(factor_scores)}"
        )

    context_1 = math.prod(float(score) for score in factor_scores[:n_genes])
    context_2 = math.prod(float(score) for score in factor_scores[n_genes : 2 * n_genes])
    return context_1, context_2
