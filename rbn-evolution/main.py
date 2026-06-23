from cli import parse_args
from demo import run_demo


def main():
    args = parse_args()
    run_demo(
        n_genes=args.n_genes,
        mutation_probability=args.mutation_probability,
        generations=args.generations,
        show_plots=args.show_plots,
        verbose=False,
    )


if __name__ == "__main__":
    main()
