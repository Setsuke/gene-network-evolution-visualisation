# `evolution.json`

**Path:** `outputs/evolution.json` (or `outputs/runs/run_NNN/evolution.json` for batch runs)  
**Producer:** `exports/evolution_json.py` via `experiment_runner.py` (called from `demo.py` → `batch_runner.py`)

Not written when using `--show-plots`.

## Purpose

Full evolutionary history on disk for post-run analysis and Unity visualisation (accepted/rejected steps, per-context trajectories, `edgeTo` transition tables).

## Generate

```bash
python demo.py --n-genes 5 --generations 1000 --runs 100
```

## Top-level schema

```text
evolution.json
├── schemaVersion
├── nGenes
├── stateCount             # 2^n
├── genomeLength           # n · 2^n
├── mutationProbability
├── generationsRequested
├── generationsStored
├── contexts[]             # initial → target (from config.py)
├── summary
└── generations[]
    ├── generation
    ├── genome
    ├── fitness
    ├── accepted
    ├── changedBits
    ├── contexts[]
    └── edgeTo[]
```
