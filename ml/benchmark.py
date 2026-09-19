"""
Benchmark Script for Mirror On-Device Model Inference
Instruments model initialization time, warmup latency, steady-state P50 and P95 latency.
"""

import json
import os
import time
import numpy as np
import onnxruntime as ort
import psutil

def run_benchmark(model_path="ml/models/mirror_pattern_v1.onnx", iterations=100, output_file="ml/benchmark_results.json"):
    print(f"Running benchmark on {model_path} ({iterations} iterations)...")

    # 1. Initialization timing
    t0 = time.perf_counter()
    session = ort.InferenceSession(model_path, providers=ort.get_available_providers())
    init_time_ms = (time.perf_counter() - t0) * 1000.0

    active_provider = session.get_providers()[0]
    print(f"Active Provider: {active_provider}")
    print(f"Initialization Time: {init_time_ms:.2f} ms")

    # 2. Warmup run
    dummy_input = np.random.randn(1, 60, 10).astype(np.float32)
    t_warmup_start = time.perf_counter()
    _ = session.run(None, {"behavioral_sequence": dummy_input})
    warmup_latency_ms = (time.perf_counter() - t_warmup_start) * 1000.0
    print(f"Warmup Latency: {warmup_latency_ms:.2f} ms")

    # 3. Steady-state benchmark
    latencies = []
    for _ in range(iterations):
        inp = np.random.randn(1, 60, 10).astype(np.float32)
        t_start = time.perf_counter()
        _ = session.run(None, {"behavioral_sequence": inp})
        t_end = time.perf_counter()
        latencies.append((t_end - t_start) * 1000.0)

    latencies = np.array(latencies)
    p50 = float(np.percentile(latencies, 50))
    p90 = float(np.percentile(latencies, 90))
    p95 = float(np.percentile(latencies, 95))
    mean_lat = float(np.mean(latencies))

    proc = psutil.Process(os.getpid())
    mem_info = proc.memory_info()
    rss_mb = mem_info.rss / (1024 * 1024)

    results = {
        "model": os.path.basename(model_path),
        "execution_provider": active_provider,
        "initialization_time_ms": round(init_time_ms, 2),
        "warmup_latency_ms": round(warmup_latency_ms, 2),
        "steady_state_p50_ms": round(p50, 2),
        "steady_state_p90_ms": round(p90, 2),
        "steady_state_p95_ms": round(p95, 2),
        "mean_latency_ms": round(mean_lat, 2),
        "iterations": iterations,
        "process_memory_rss_mb": round(rss_mb, 2)
    }

    print("\n--- BENCHMARK RESULTS ---")
    for k, v in results.items():
        print(f"{k}: {v}")
    print("-" * 30)

    with open(output_file, "w") as f:
        json.dump(results, f, indent=2)

    return results

if __name__ == "__main__":
    fp32_model = "ml/models/mirror_pattern_v1.onnx"
    if os.path.exists(fp32_model):
        run_benchmark(fp32_model, iterations=100, output_file="ml/benchmark_fp32.json")

    qdq_model = "ml/models/mirror_pattern_v1.qdq.onnx"
    if os.path.exists(qdq_model):
        run_benchmark(qdq_model, iterations=100, output_file="ml/benchmark_qdq.json")
