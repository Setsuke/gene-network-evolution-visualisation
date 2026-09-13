# Interactive-3D-visualisation-of-RBN-dynamics

MSc Advanced Computer Science dissertation project: Interactive 3D visualisation of network dynamics.

## Quick start

```bash
cd rbn-evolution
pip install -r requirements.txt
python demo.py --n-genes 5 --generations 1000 --runs 100
```

**Dependencies:** `numpy`, `matplotlib`, `networkx` (see `rbn-evolution/requirements.txt`).

- **Outputs:** `rbn-evolution/outputs/` (PNGs, CSVs, `evolution.json`).
- **Unity JSON:** copied to Unity `Assets/StreamingAssets/` when saving (default: no `--show-plots`).
- **Interactive plots only:** add `--show-plots` (skips file export and batch analytics).

## Unity export path

Auto-detected if the repo layout is `DissertationProject/unity/rbn-visualisation/Assets/StreamingAssets`.

Override only when Unity lives elsewhere:

```bash
# CLI
python demo.py --runs 100 --unity-streaming-assets "C:/path/to/Assets/StreamingAssets"

# or env (PowerShell)
$env:UNITY_STREAMING_ASSETS = "C:/path/to/Assets/StreamingAssets"
```

The pipeline prints `Unity StreamingAssets export dir: ...` before copying JSON.

## Common flags

| Flag | Default | Description |
|------|---------|-------------|
| `--n-genes` | `5` | Network size (genome length = n·2ⁿ bits) |
| `--generations` | `1000` | Max evolution steps per run |
| `--mutation-probability` | `0.1` | Per-bit mutation rate |
| `--runs` | `1` | Independent runs (`outputs/runs/run_001/` when > 1) |
| `--jobs` | auto | Parallel workers for batch runs |
| `--shared-initial-genome` | off | Same random start genome for every run |
| `--show-plots` | off | Show plots on screen; **no** saved outputs |
| `--unity-streaming-assets` | auto | Unity `StreamingAssets` folder for JSON export |

**Note:** Unity MDS exports are capped at **200 runs** (`config.py`); batch evolution itself is not capped.

## Project layout

```
rbn-evolution/
  demo.py          # main pipeline
  requirements.txt # Python dependencies
  config.py        # contexts, paths, Unity export resolution
  rbn/             # core logic
  visualisation/   # plots and batch analytics
  docs/            # JSON schema notes
  outputs/         # generated
```

JSON schema: [`rbn-evolution/docs/evolution_json.md`](rbn-evolution/docs/evolution_json.md)

## Unity

1. Run `demo.py` (without `--show-plots`) to refresh StreamingAssets JSON.
2. Open `unity/rbn-visualisation` in Unity and press Play.
