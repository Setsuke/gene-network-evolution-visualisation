# Interactive-3D-visualisation-of-RBN-dynamics

MSc Advanced Computer Science dissertation project: Interactive 3D visualisation of network dynamics.

## Quick start

```bash
cd rbn-evolution
pip install matplotlib networkx
python demo.py --n-genes 5 --generations 10000 --mutation-probability 0.05 
```

Outputs are written to `rbn-evolution/outputs/` (PNG plots and `evolution.json`). Use `--show-plots` to view graphs on screen without saving files.

## Options

`--n-genes`: 5(default) - Network size (genome length = n·2ⁿ bits)
`--generations`: 1000(default) - Maximum number of evolutions
`--mutation-probability`: 0.1(default) - bit mutation rate
`--show-plots`: off(default) - Show plots instead of saving to `outputs/`

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
