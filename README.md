# Interactive-3D-visualisation-of-RBN-dynamics

MSc Advanced Computer Science dissertation project: Interactive 3D visualisation of network dynamics.

## Quick start

```bash
cd rbn-evolution
pip install matplotlib networkx umap-learn
python demo.py --n-genes 5 --generations 10000 --mutation-probability 0.1 --runs 100
```

Outputs are written to `rbn-evolution/outputs/` (PNG plots and `evolution.json`). Use `--show-plots` to view graphs on screen without saving files.

## Options

| Flag | Default | Description |
|------|---------|-------------|
| `--n-genes` | `5` | Network size (genome length = n·2ⁿ bits) |
| `--generations` | `1000` | Maximum number of evolution steps |
| `--mutation-probability` | `0.1` | Per-bit mutation rate |
| `--show-plots` | off | Show plots on screen instead of saving to `outputs/` |
| `--runs` | `1` | Number of independent runs |

## Project layout

```
rbn-evolution/
  demo.py          # main pipeline
  config.py        # contexts and output paths
  rbn/             # core logic
  visualisation/   # 2D plots
  docs/            # JSON schema notes
  outputs/         # generated
```

JSON export format: [`rbn-evolution/docs/evolution_json.md`](rbn-evolution/docs/evolution_json.md)
