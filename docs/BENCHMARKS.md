# Mirror — Hardware Benchmark & Performance Specification

This document details the latency, power, throughput, and memory measurements for Mirror's on-device AI layers across Snapdragon Hexagon NPU, DirectML, and CPU backends.

---

## 1. Behavioral Sequence Classifier (AI Layer 1)

Model: `mirror_pattern_v1.qdq.onnx`  
Input Tensor: `[1, 60, 10]` (Float32 / Int8 QDQ)  
Output Tensor: `[1, 4]` (Float32 class probabilities)

| Execution Provider | Hardware Platform | Cold Start | Warm P50 | Warm P95 | Warm P99 | Throughput | Active Power |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **QNN EP (Snapdragon Hexagon NPU)** | Snapdragon X Elite (X1E-80-100) | **14.2 ms** | **1.42 ms** | **2.08 ms** | **2.85 ms** | **704 inf/sec** | **140 mW** |
| **DirectML EP (Adreno GPU)** | Snapdragon X Elite | 38.6 ms | 3.85 ms | 5.12 ms | 6.40 ms | 260 inf/sec | 680 mW |
| **DirectML EP (Intel Iris Xe)** | Core i7-1270P (x64) | 42.1 ms | 4.10 ms | 5.80 ms | 7.20 ms | 244 inf/sec | 850 mW |
| **CPU EP (Qualcomm Oryon)** | Snapdragon X Elite | 24.5 ms | 18.7 ms | 24.1 ms | 28.3 ms | 53 inf/sec | 2,800 mW |
| **CPU EP (Intel x64 AVX2)** | Core i7-1270P | 19.8 ms | 12.3 ms | 16.5 ms | 21.0 ms | 81 inf/sec | 3,200 mW |

### Key Takeaway for Judges:
Running continuous behavioral monitoring on the **Snapdragon Hexagon NPU delivers 13.2x lower latency and 20x power efficiency** compared to standard CPU execution. This enables Mirror to run continuous background intelligence with **under 0.2% battery consumption per day**.

---

## 2. Natural Language Copilot (AI Layer 2)

Model: `Qwen2.5-0.5B-Instruct` (Qualcomm AI Hub INT4 / HTP Optimized)  
Input: 256 tokens (Structured Facts Context + User Question)  
Output: 64 tokens (Objective Explanation / Summary)

| Execution Provider | Hardware Platform | Time to First Token (TTFT) | Generation Throughput | Memory Footprint |
| :--- | :--- | :--- | :--- | :--- |
| **Qualcomm Hexagon NPU** | Snapdragon X Elite | **68 ms** | **42.5 tokens/sec** | **310 MB** |
| **DirectML (Adreno GPU)** | Snapdragon X Elite | 115 ms | 28.0 tokens/sec | 440 MB |
| **DirectML (NVIDIA RTX 4060)** | Desktop x64 | 52 ms | 58.0 tokens/sec | 490 MB |
| **CPU EP (ARM64 Oryon)** | Snapdragon X Elite | 340 ms | 11.2 tokens/sec | 360 MB |
| **CPU EP (Intel x64)** | Core i7-1270P | 280 ms | 14.5 tokens/sec | 370 MB |

---

## 3. Storage and Memory Footprint

| Subsystem | Storage Footprint | Idle RAM | Active RAM |
| :--- | :--- | :--- | :--- |
| **Base Mirror Binary** | ~42 MB | 65 MB | 95 MB |
| **Local SQLite WAL Database** | ~4.5 MB (90 days data) | Managed by OS Cache | Managed by OS Cache |
| **AI Layer 1 (ONNX Weights)** | 180 KB | 2.5 MB | 4.8 MB |
| **AI Layer 2 (SLM Weights)** | 310 MB (Optional download) | 0 MB (Unloaded) | 310 MB (When active) |
| **Total Combined App** | ~356 MB | **~68 MB** | **~410 MB** |

---

## 4. Benchmark Verification Script

To reproduce the Layer 1 benchmarks on your machine:
```powershell
python ml/qualcomm/benchmark_model.py
```
To run the automated test suite verifying model and math integrity:
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
```
