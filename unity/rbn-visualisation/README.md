# RBN Visualisation (Unity)

Unity **6000.3** project for interactive 3D exploration of RBN evolution outputs.

## Setup

1. Generate JSON from Python (repo root):

   ```bash
   cd rbn-evolution
   pip install -r requirements.txt
   python demo.py --n-genes 5 --generations 1000 --runs 100
   ```

2. Open this folder in Unity Hub.
3. Open `Assets/Scenes/SampleScene.unity` and press **Play**.

## Menu views

1. Genome MDS path  
2. Genome MDS + fitness path  
3. Boolean attractor landscape  
4. Shared-initial-genome MDS path  
5. Batch MDS path  
6. Batch MDS + fitness path  
7. Plateau drift (dual pane)

Runtime data lives in `Assets/StreamingAssets/` (exported by `demo.py`).
