import argparse
from pathlib import Path

from config import DEFAULT_N_GENES


def parse_args():
    parser = argparse.ArgumentParser(
        description="rbn-evolution: Random Boolean Network evolution demo"
    )

    parser.add_argument(
        "--n-genes",
        type=int,
        default=DEFAULT_N_GENES,
        help="Number of genes in the network (state space size = 2^n).",
    )

    parser.add_argument(
        "--mutation-probability",
        type=float,
        default=0.1,
        help="Probability that each rule bit flips during mutation.",
    )

    parser.add_argument(
        "--generations",
        type=int,
        default=1000,
        help="Number of generations to evolve.",
    )

    parser.add_argument(
        "--show-plots",
        action="store_true",
        help="Show plots interactively instead of only saving PNG files.",
    )

    parser.add_argument(
        "--runs",
        type=int,
        default=1,
        help="Number of independent runs to execute (outputs go to outputs/runs/run_001/ when runs > 1).",
    )

    parser.add_argument(
        "--jobs",
        type=int,
        default=None,
        help=(
            "Parallel workers for batch runs. Default: auto from CPU and RAM "
            "(reserves ~28 GB for OS/Unity/IDE and ~3 GB per run)."
        ),
    )

    parser.add_argument(
        "--shared-initial-genome",
        action="store_true",
        help=(
            "Use one randomly generated initial genome for every run in the batch. "
            "Each run still evolves independently from that shared starting network."
        ),
    )

    parser.add_argument(
        "--unity-streaming-assets",
        type=Path,
        default=None,
        help=(
            "Unity StreamingAssets folder where batch JSON exports are copied. "
            "Overrides UNITY_STREAMING_ASSETS / RBN_UNITY_STREAMING_ASSETS."
        ),
    )

    return parser.parse_args()
