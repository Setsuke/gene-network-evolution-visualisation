# Gene network evolution visualisation (Unity)

Unity **6000.3.17f1** viewer for the dissertation *Understanding Punctuated Equilibria through the Visualisation of Gene Network Evolution* — interactive 3D exploration of evolved random Boolean network runs.

## Setup

1. **Clone the repo** and generate runtime JSON (not stored in git — some files exceed GitHub’s 100 MB limit):

   ```bash
   cd rbn-evolution
   pip install -r requirements.txt
   python demo.py --n-genes 5 --generations 1000 --runs 100
   ```

   JSON is written to `Assets/StreamingAssets/` automatically (skip with `--show-plots`).

2. Open **this folder** (`unity/rbn-visualisation`) in Unity Hub (**6000.3.17f1**).
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.

## Menu views

1. Genome MDS path  
2. Genome MDS + fitness path  
3. Boolean attractor landscape  
4. Shared-initial-genome MDS path  
5. Batch MDS path  
6. Batch MDS + fitness path  
7. Plateau drift (dual pane)

Runtime data lives in `Assets/StreamingAssets/` (exported by `demo.py`; see `Assets/StreamingAssets/README.txt`).
