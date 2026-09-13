"""Parallel batch execution with CPU/RAM-aware worker defaults."""

from __future__ import annotations

import os
import platform
from concurrent.futures import ProcessPoolExecutor, as_completed
from dataclasses import dataclass
from typing import Callable

from config import get_run_output_paths
from experiment_runner import run_single_experiment
from rbn.truth_table import generate_random_truth_table, truth_table_to_genome
from rbn.types import Contexts, EvolutionHistory, Genome

# Headroom for Windows, Unity, VS Code, Chrome, and other desktop apps.
RESERVED_RAM_GB = 28.0
# Conservative peak RAM per run (history + transition tables + matplotlib exports).
ESTIMATED_RAM_PER_RUN_GB = 3.0
# CPU cores left free for the OS and IDE.
RESERVED_CPU_CORES = 2


@dataclass(frozen=True)
class RunTask:
    run_index: int
    runs: int
    n_genes: int
    contexts: Contexts
    mutation_probability: float
    generations: int
    show_plots: bool
    initial_genome: Genome | None = None


@dataclass(frozen=True)
class RunTaskResult:
    run_index: int
    history: EvolutionHistory
    run_label: str


@dataclass(frozen=True)
class JobPlan:
    jobs: int
    cpu_count: int
    total_ram_gb: float | None
    cpu_limit: int
    ram_limit: int | None
    reserved_ram_gb: float


def detect_total_ram_gb() -> float | None:
    system = platform.system()
    if system == "Windows":
        return _windows_total_ram_gb()
    if system == "Linux":
        return _linux_total_ram_gb()
    if system == "Darwin":
        return _macos_total_ram_gb()
    return None


def _windows_total_ram_gb() -> float | None:
    try:
        import ctypes

        class MemoryStatusEx(ctypes.Structure):
            _fields_ = [
                ("dwLength", ctypes.c_ulong),
                ("dwMemoryLoad", ctypes.c_ulong),
                ("ullTotalPhys", ctypes.c_ulonglong),
                ("ullAvailPhys", ctypes.c_ulonglong),
                ("ullTotalPageFile", ctypes.c_ulonglong),
                ("ullAvailPageFile", ctypes.c_ulonglong),
                ("ullTotalVirtual", ctypes.c_ulonglong),
                ("ullAvailVirtual", ctypes.c_ulonglong),
                ("ullAvailExtendedVirtual", ctypes.c_ulonglong),
            ]

        status = MemoryStatusEx()
        status.dwLength = ctypes.sizeof(MemoryStatusEx)
        if not ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(status)):
            return None
        return status.ullTotalPhys / (1024**3)
    except (AttributeError, OSError):
        return None


def _linux_total_ram_gb() -> float | None:
    try:
        with open("/proc/meminfo", encoding="utf-8") as handle:
            for line in handle:
                if line.startswith("MemTotal:"):
                    kilobytes = int(line.split()[1])
                    return kilobytes / (1024**2)
    except OSError:
        return None
    return None


def _macos_total_ram_gb() -> float | None:
    try:
        import subprocess

        bytes_total = int(subprocess.check_output(["sysctl", "-n", "hw.memsize"], text=True).strip())
        return bytes_total / (1024**3)
    except (OSError, ValueError, subprocess.SubprocessError):
        return None


def resolve_job_count(
    *,
    runs: int,
    jobs: int | None = None,
    reserved_ram_gb: float = RESERVED_RAM_GB,
    estimated_ram_per_run_gb: float = ESTIMATED_RAM_PER_RUN_GB,
    reserved_cpu_cores: int = RESERVED_CPU_CORES,
) -> JobPlan:
    if runs < 1:
        raise ValueError("runs must be at least 1")

    cpu_count = os.cpu_count() or 4
    cpu_limit = max(1, cpu_count - reserved_cpu_cores)

    total_ram_gb = detect_total_ram_gb()
    ram_limit: int | None
    if total_ram_gb is None:
        ram_limit = None
    else:
        available_ram_gb = max(estimated_ram_per_run_gb, total_ram_gb - reserved_ram_gb)
        ram_limit = max(1, int(available_ram_gb // estimated_ram_per_run_gb))

    if jobs is not None:
        if jobs < 1:
            raise ValueError("jobs must be at least 1")
        chosen = min(jobs, runs)
    else:
        limits = [cpu_limit, runs]
        if ram_limit is not None:
            limits.append(ram_limit)
        chosen = max(1, min(limits))

    return JobPlan(
        jobs=chosen,
        cpu_count=cpu_count,
        total_ram_gb=total_ram_gb,
        cpu_limit=cpu_limit,
        ram_limit=ram_limit,
        reserved_ram_gb=reserved_ram_gb,
    )


def describe_job_plan(plan: JobPlan, runs: int) -> str:
    ram_text = (
        f"{plan.total_ram_gb:.0f} GB RAM detected, reserving {plan.reserved_ram_gb:.0f} GB"
        if plan.total_ram_gb is not None
        else "RAM not detected"
    )
    ram_limit_text = (
        f"{plan.ram_limit} workers by RAM"
        if plan.ram_limit is not None
        else "RAM cap skipped"
    )
    return (
        f"Parallel batch: {runs} runs with {plan.jobs} worker(s) "
        f"({plan.cpu_count} logical cores, {ram_text}, "
        f"{plan.cpu_limit} workers by CPU, {ram_limit_text})"
    )


def _run_task(task: RunTask) -> RunTaskResult:
    run_label = f"Run {task.run_index}/{task.runs}" if task.runs > 1 else "run_001"
    output_paths = None if task.show_plots else get_run_output_paths(task.run_index, task.runs)
    history = run_single_experiment(
        n_genes=task.n_genes,
        contexts=task.contexts,
        mutation_probability=task.mutation_probability,
        generations=task.generations,
        show_plots=task.show_plots,
        output_paths=output_paths,
        run_label=run_label if task.runs > 1 else None,
        quiet=True,
        initial_genome=task.initial_genome,
    )
    return RunTaskResult(run_index=task.run_index, history=history, run_label=run_label)


def run_batch_experiments(
    *,
    runs: int,
    n_genes: int,
    contexts: Contexts,
    mutation_probability: float,
    generations: int,
    show_plots: bool,
    jobs: int | None,
    shared_initial_genome: bool = False,
    progress_callback: Callable[[RunTaskResult], None] | None = None,
) -> tuple[list[EvolutionHistory], list[str], JobPlan]:
    plan = resolve_job_count(runs=runs, jobs=jobs)

    batch_initial_genome: Genome | None = None
    if shared_initial_genome:
        batch_initial_genome = truth_table_to_genome(generate_random_truth_table(n_genes))
        print(
            f"Shared initial genome enabled: all {runs} run(s) start from the same "
            f"random {len(batch_initial_genome)}-bit network."
        )

    tasks = [
        RunTask(
            run_index=run_index,
            runs=runs,
            n_genes=n_genes,
            contexts=dict(contexts),
            mutation_probability=mutation_probability,
            generations=generations,
            show_plots=show_plots,
            initial_genome=batch_initial_genome,
        )
        for run_index in range(1, runs + 1)
    ]

    if plan.jobs == 1 or runs == 1:
        results: list[RunTaskResult] = []
        for task in tasks:
            run_label = f"Run {task.run_index}/{task.runs}" if task.runs > 1 else None
            output_paths = None if show_plots else get_run_output_paths(task.run_index, task.runs)
            history = run_single_experiment(
                n_genes=n_genes,
                contexts=contexts,
                mutation_probability=mutation_probability,
                generations=generations,
                show_plots=show_plots,
                output_paths=output_paths,
                run_label=run_label,
                quiet=False,
                initial_genome=task.initial_genome,
            )
            result = RunTaskResult(
                run_index=task.run_index,
                history=history,
                run_label=f"Run {task.run_index}/{task.runs}" if task.runs > 1 else "run_001",
            )
            results.append(result)
            if progress_callback is not None:
                progress_callback(result)
    else:
        results = []
        with ProcessPoolExecutor(max_workers=plan.jobs) as executor:
            futures = {executor.submit(_run_task, task): task.run_index for task in tasks}
            for future in as_completed(futures):
                result = future.result()
                results.append(result)
                if progress_callback is not None:
                    progress_callback(result)

        results.sort(key=lambda item: item.run_index)

    histories = [result.history for result in results]
    run_labels = [result.run_label for result in results]
    return histories, run_labels, plan
