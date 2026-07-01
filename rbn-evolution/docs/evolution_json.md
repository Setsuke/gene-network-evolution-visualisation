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

```bash
python demo.py --n-genes 5 --generations 10000 --mutation-probability 0.1 --runs 100
```

---

## Top-level schema

```text
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
```
