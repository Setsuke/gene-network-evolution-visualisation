"""Export accepted-genome attractor dynamics for Unity Option 5."""



from __future__ import annotations



import json

from pathlib import Path

from typing import Any



from exports.evolution_json import (

    index_to_state,

    state_to_index,

    transition_table_to_edge_to,

)

from rbn.attractors import find_attractors

from rbn.types import Contexts, EvolutionHistory, GenerationRecord, State, TransitionTable

from visualisation.accepted_move_taxonomy import (

    classify_top_level_accepted_move_category,

    iter_accepted_change_records,

)

from visualisation.factorised.metrics import extract_accepted_trajectory_metrics



SCHEMA_VERSION = 1





def _extract_accepted_records(history: EvolutionHistory) -> list[GenerationRecord]:

    if not history:

        return []



    records: list[GenerationRecord] = []

    last_genome: str | None = None

    for record in history:

        genome = record["genome"]

        if last_genome is None or genome != last_genome:

            records.append(record)

            last_genome = genome

            if float(record["fitness"]) == 1.0:

                break



    if history[-1]["genome"] != last_genome:

        records.append(history[-1])

    return records





def _change_types_for_history(

    history: EvolutionHistory,

    contexts: Contexts,

    *,

    step_count: int,

) -> tuple[str, ...]:

    change_types: list[str] = ["initial"]

    for _, previous, current in iter_accepted_change_records(history):

        change_types.append(

            classify_top_level_accepted_move_category(previous, current, contexts)

        )

    if len(change_types) != step_count:

        raise ValueError(

            "accepted trajectory length does not match classified move count "

            f"({len(change_types)} != {step_count})"

        )

    return tuple(change_types)





def _build_context_paths(

    details: dict[State, dict[str, object]],

    contexts: Contexts,

    n_genes: int,

) -> list[dict[str, Any]]:

    paths: list[dict[str, Any]] = []

    for initial, target in contexts.items():

        context_details = details[initial]

        path = context_details["path"]

        cycle = context_details["cycle"]

        paths.append(

            {

                "initialIndex": state_to_index(initial, n_genes),

                "targetIndex": state_to_index(target, n_genes),

                "initialState": initial,

                "targetState": target,

                "pathIndices": [state_to_index(state, n_genes) for state in path],

                "cycleIndices": [state_to_index(state, n_genes) for state in cycle],

                "fitness": float(context_details["fitness"]),

            }

        )

    return paths





def _build_attractor_entries(

    transition_table: TransitionTable,

    n_genes: int,

) -> list[dict[str, Any]]:

    point_attractors, limit_cycles = find_attractors(transition_table)

    entries: list[dict[str, Any]] = []

    for state in point_attractors:

        entries.append(

            {

                "type": "point",

                "stateIndices": [state_to_index(state, n_genes)],

            }

        )

    for cycle in limit_cycles:

        entries.append(

            {

                "type": "limit_cycle",

                "stateIndices": [state_to_index(state, n_genes) for state in cycle],

            }

        )

    return entries





def _serialize_contexts(contexts: Contexts, n_genes: int) -> list[dict[str, Any]]:

    entries: list[dict[str, Any]] = []

    for index, (initial, target) in enumerate(contexts.items(), start=1):

        entries.append(

            {

                "label": f"Context {index}",

                "initialState": initial,

                "targetState": target,

                "initialIndex": state_to_index(initial, n_genes),

                "targetIndex": state_to_index(target, n_genes),

            }

        )

    return entries





def build_unity_attractor_landscape_payload(

    histories: list[EvolutionHistory],

    contexts: Contexts,

    *,

    n_genes: int,

    labels: list[str] | None = None,

    layout_file_name: str = "boolean_state_mds_layout.json",

) -> dict[str, Any]:

    """

    Build per-accepted-genome attractor dynamics for Unity.



    Node positions come from the separate Boolean state layout file. This payload

    only carries transition tables, context paths, and attractor structure.

    """

    runs: list[dict[str, Any]] = []

    for run_index, history in enumerate(histories):

        label = labels[run_index] if labels is not None else f"run_{run_index + 1:03d}"

        accepted_records = _extract_accepted_records(history)

        if not accepted_records:

            continue



        metrics = extract_accepted_trajectory_metrics(history, contexts)

        change_types = _change_types_for_history(

            history,

            contexts,

            step_count=len(metrics),

        )

        if len(change_types) != len(accepted_records):

            raise ValueError(

                f"run {label}: accepted record count does not match trajectory metrics"

            )



        steps: list[dict[str, Any]] = []

        for step_index, (record, change_type) in enumerate(

            zip(accepted_records, change_types)

        ):

            steps.append(

                {

                    "step": step_index,

                    "generation": int(record["generation"]),

                    "fitness": float(record["fitness"]),

                    "changeType": change_type,

                    "edgeTo": transition_table_to_edge_to(

                        record["transition_table"],

                        n_genes,

                    ),

                    "contextPaths": _build_context_paths(

                        record["details"],

                        contexts,

                        n_genes,

                    ),

                    "attractors": _build_attractor_entries(

                        record["transition_table"],

                        n_genes,

                    ),

                }

            )



        runs.append(

            {

                "sourceLabel": label,

                "finalFitness": float(accepted_records[-1]["fitness"]),

                "acceptedCount": len(steps),

                "steps": steps,

            }

        )



    return {

        "schemaVersion": SCHEMA_VERSION,

        "layoutFile": layout_file_name,

        "space": "boolean_state_dynamics",

        "description": (

            "Accepted-genome transition dynamics over the fixed Boolean state layout. "

            "Genome changes update edges and highlights only; node positions stay fixed."

        ),

        "geneCount": n_genes,

        "stateCount": 1 << n_genes,

        "contexts": _serialize_contexts(contexts, n_genes),

        "runCount": len(runs),

        "runs": runs,

    }





def export_unity_attractor_landscape_json(

    histories: list[EvolutionHistory],

    contexts: Contexts,

    output_paths: str | Path | list[str | Path],

    *,

    n_genes: int,

    labels: list[str] | None = None,

    layout_file_name: str = "boolean_state_mds_layout.json",

) -> list[Path]:

    payload = build_unity_attractor_landscape_payload(

        histories,

        contexts,

        n_genes=n_genes,

        labels=labels,

        layout_file_name=layout_file_name,

    )

    if isinstance(output_paths, (str, Path)):

        paths = [Path(output_paths)]

    else:

        paths = [Path(path) for path in output_paths]



    written: list[Path] = []

    for path in paths:

        path.parent.mkdir(parents=True, exist_ok=True)

        with path.open("w", encoding="utf-8") as handle:

            json.dump(payload, handle, indent=2)

        written.append(path)

    return written

