"""Export each generation context fitness as CSV and aligned text table."""

from __future__ import annotations

import csv
from pathlib import Path

from rbn.types import Contexts, EvolutionHistory, State


def _context_order(contexts: Contexts) -> list[State]:
    return list(contexts.keys())


def _context_fitness_row(
    record: dict,
    context_inits: list[State],
) -> list[float | int | bool]:
    context_values = [
        float(record["details"][initial]["fitness"]) for initial in context_inits
    ]
    return [record["generation"], *context_values, float(record["fitness"])]


def _header_labels(context_count: int) -> list[str]:
    context_columns = [f"C{index}" for index in range(1, context_count + 1)]
    return ["generation", *context_columns, "total_fitness"]


def build_context_fitness_rows(
    history: EvolutionHistory,
    contexts: Contexts,
) -> tuple[list[str], list[list[float | int]]]:
    context_inits = _context_order(contexts)
    headers = _header_labels(len(context_inits))
    rows: list[list[float | int]] = []

    for record in history:
        row = _context_fitness_row(record, context_inits)
        rows.append([int(row[0]), *[float(value) for value in row[1:]]])

    return headers, rows


def format_context_fitness_table(
    history: EvolutionHistory,
    contexts: Contexts,
) -> str:
    headers, rows = build_context_fitness_rows(history, contexts)
    display_headers = ["Generation", *[header.upper() for header in headers[1:-1]], "Total fitness"]

    string_rows = [
        [str(value) for value in row[:1]]
        + [f"{value:g}" for value in row[1:]]
        for row in rows
    ]

    column_widths = [
        max(len(display_headers[index]), *(len(row[index]) for row in string_rows))
        for index in range(len(display_headers))
    ]

    def format_row(cells: list[str]) -> str:
        return " | ".join(
            cell.ljust(column_widths[index]) for index, cell in enumerate(cells)
        )

    lines = [format_row(display_headers), format_row(["-" * width for width in column_widths])]
    lines.extend(format_row(row) for row in string_rows)
    return "\n".join(lines)


def export_context_fitness_table(
    history: EvolutionHistory,
    contexts: Contexts,
    csv_path: str | Path,
    txt_path: str | Path | None = None,
) -> tuple[Path, Path | None]:
    """Write context fitness history to CSV and optionally to an aligned text table."""
    csv_path = Path(csv_path)
    csv_path.parent.mkdir(parents=True, exist_ok=True)

    headers, rows = build_context_fitness_rows(history, contexts)

    with csv_path.open("w", encoding="utf-8", newline="") as handle:
        writer = csv.writer(handle)
        writer.writerow(headers)
        writer.writerows(rows)

    written_txt: Path | None = None
    if txt_path is not None:
        written_txt = Path(txt_path)
        written_txt.write_text(
            format_context_fitness_table(history, contexts) + "\n",
            encoding="utf-8",
        )

    return csv_path, written_txt
