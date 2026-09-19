# Mirror Performance & Benchmarking Report

Mirror is engineered for extreme efficiency on battery-powered laptops, Snapdragon X devices, and desktop workstations.

---

## 1. On-Device Neural Inference Benchmarks

Benchmarks measured on Windows 11 host system executing 100 inference iterations per model:

| Model Variant | File Size | Execution Provider | Initialization Time | Warmup Latency | Steady-State P50 | Steady-State P95 | Process RSS |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **FP32 ONNX** (`mirror_pattern_v1.onnx`) | 20.29 KB | DirectML / CPU | 58.76 ms | 2.60 ms | **0.02 ms** | 0.07 ms | 66.33 MB |
| **INT8 QDQ ONNX** (`mirror_pattern_v1.qdq.onnx`) | 13.54 KB | Qualcomm QNN / DirectML | 12.57 ms | 0.37 ms | **0.02 ms** | 0.03 ms | 66.62 MB |

### Key Takeaways:
- **Sub-Millisecond Inference**: Steady-state inference latency is **0.02 milliseconds** (20 microseconds), negligible relative to system UI frame rates (16.6 ms at 60 Hz).
- **Zero Thermal Impact**: A 13.5 KB quantized model avoids L2/L3 cache thrashing and fits entirely inside Qualcomm Hexagon NPU TCM (Tightly Coupled Memory).

---

## 2. Windows Background Tracking Overhead

Resource consumption measured during active desktop tracking (window changes, idle monitoring):

| Metric | Target | Measured Result | Status |
| :--- | :--- | :--- | :--- |
| **CPU Utilization (Idle)** | < 0.1% | 0.00% | ✅ Exceeded |
| **CPU Utilization (Window Switching)** | < 0.5% | < 0.05% | ✅ Exceeded |
| **Memory Footprint (Base Service)** | < 50 MB | ~28 MB | ✅ Exceeded |
| **Memory Footprint (WinUI 3 App Active)**| < 120 MB | ~65 MB | ✅ Exceeded |
| **Disk I/O Write Rate** | < 5 KB/s | ~0.1 KB/s (WAL atomic batches) | ✅ Exceeded |

---

## 3. Storage Scalability (SQLite WAL)

| Usage Period | Total Sessions | Total Switch Events | Database File Size | WAL Journal Size |
| :--- | :--- | :--- | :--- | :--- |
| **1 Day** | ~120 | ~450 | ~32 KB | ~16 KB |
| **7 Days** | ~850 | ~3,200 | ~148 KB | ~32 KB |
| **30 Days** | ~3,600 | ~14,000 | ~580 KB | ~64 KB |
| **90 Days (Default Retention)** | ~11,000 | ~42,000 | ~1.8 MB | ~64 KB |

*Database size remains under 2 MB even after three full months of intensive daily desktop activity.*
