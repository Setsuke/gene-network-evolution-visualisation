# `evolution.json` documentation

Default path: `outputs/evolution.json`
Producer: `export_json.py`, called from `demo.py`.

---

## Purpose

Persists the full history of an evolutionary run to disk. During execution, that history only lives in RAM. The file is intended for:

- Post-run analysis
- 3D visualisation in Unity
- Inspecting Accepted and rejected generations
- Reconstructing dynamics (`edgeTo`) for each generation without re-running the simulation

---

## How it is generated

python demo.py --n-genes 5 --generations 1000 --mutation-probability 0.05

Notes:

- Written to `outputs/evolution.json`
- `prepare_output_dir()` deletes the previous JSON at the start of a run (along with PNGs)
- With `--show-plots`, export is skipped (interactive windows only)

---

## Top-level schema

evolution.json
├── schemaVersion          # format version
├── nGenes                 # n (genes / bits per state)
├── stateCount             # 2^n
├── genomeLength           # n · 2^n (for n=5 = 5 * 32 = 160 bits)
├── mutationProbability    # p used in the run
├── generationsRequested   # --generations limit
├── generationsStored      # actual entries (may be less if stopped at fitness=1)
├── contexts[]             # initial → target pairs (from config)
├── summary                # global statistics
└── generations[]          # one entry per generation
    ├── generation
    ├── genome
    ├── fitness
    ├── accepted
    ├── changedBits
    ├── contexts[]         # trajectories and fitness per context
    └── edgeTo[]           # compact transition table

