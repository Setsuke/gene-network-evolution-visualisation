# gene-network-evolution-visualisation

MSc Advanced Computer Science dissertation project: interactive 3D visualisation of random Boolean network evolution dynamics (Python pipeline + Unity viewer).

## Quick start

```bash
cd rbn-evolution
pip install -r requirements.txt
python demo.py --n-genes 5 --generations 1000 --runs 100
```

**Dependencies:** `numpy`, `matplotlib`, `networkx` (see `rbn-evolution/requirements.txt`).

- **Outputs:** `rbn-evolution/outputs/` (PNGs, CSVs, `evolution.json`; gitignored).
- **Unity JSON:** copied to `unity/rbn-visualisation/Assets/StreamingAssets/` when saving (default: no `--show-plots`).
- **After clone:** run `demo.py` once before opening Unity — StreamingAssets JSON are **not** in git (some exceed GitHub’s 100 MB limit). See `unity/rbn-visualisation/Assets/StreamingAssets/README.txt`.
- **Interactive plots only:** add `--show-plots` (skips file export and batch analytics).

## Unity export path

Auto-detected at `unity/rbn-visualisation/Assets/StreamingAssets` in this repo (also supports a sibling `DissertationProject/unity/...` layout).

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
rbn-evolution/           # Python pipeline
  demo.py
  requirements.txt
  config.py
  rbn/
  visualisation/
  docs/
  outputs/               # generated (gitignored)

unity/rbn-visualisation/ # Unity 6000.3.17f1 project (Play demo)
  Assets/
    StreamingAssets/     # JSON from demo.py (gitignored except README.txt)
  ProjectSettings/
  Packages/
```

JSON schema: [`rbn-evolution/docs/evolution_json.md`](rbn-evolution/docs/evolution_json.md)

## Unity

1. From repo root, run `demo.py` (without `--show-plots`) to generate StreamingAssets JSON.
2. Open `unity/rbn-visualisation` in **Unity 6000.3.17f1** (Unity Hub).
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.

Menu views: genome MDS path, MDS + fitness, attractor landscape, shared-initial-genome MDS, batch MDS, batch MDS + fitness, plateau drift (dual pane). Details in [`unity/rbn-visualisation/README.md`](unity/rbn-visualisation/README.md).
