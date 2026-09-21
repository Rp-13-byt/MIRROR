# Mirror — Edge AI Architecture & Hardware Acceleration Map

This document outlines the zero-cloud, hardware-accelerated on-device computing architecture of Mirror on Windows 11 and Qualcomm Snapdragon PCs.

---

## 1. High-Level Architecture

```text
                                WINDOWS 11 OPERATING SYSTEM
   ┌─────────────────────────────────────────────────────────────────────────────────┐
   │                                                                                 │
   │  Raw OS Telemetry (WinEventHooks / Active Window / Idle Timers)                 │
   │                                     │                                           │
   │                                     ▼                                           │
   │  ┌───────────────────────────────────────────────────────────────────────────┐  │
   │  │                       PRIVACY ENFORCEMENT GATE                            │  │
   │  │  • URL & Path Stripping       • Keystroke Blockade                       │  │
   │  │  • Process Name Whitelist     • Forbidden Field Sanitization              │  │
   │  └──────────────────────────────────┬────────────────────────────────────────┘  │
   │                                     │ Clean Process & Duration Events           │
   │                                     ▼                                           │
   │  ┌───────────────────────────────────────────────────────────────────────────┐  │
   │  │                    LOCAL SQLite WAL STORAGE (DPAPI)                       │  │
   │  │  • Activity Sessions          • Focus Sessions     • Baselines            │  │
   │  └──────────────────┬─────────────────────────────────────┬──────────────────┘  │
   │                     │                                     │                     │
   │                     ▼ (Feature Extractor)                 ▼ (Structured Facts)  │
   │  ┌───────────────────────────────────────┐ ┌─────────────────────────────────┐  │
   │  │ AI LAYER 1: BEHAVIORAL CLASSIFIER    │ │ AI LAYER 2: LOCAL COPILOT       │  │
   │  │ • 60x10 Sequence Tensor (1-hr window) │ │ • Structured Local Context Plan │  │
   │  │ • Hexagon NPU via QNN EP (HTP)        │ │ • Qualcomm AI Hub Model (Qwen)  │  │
   │  │ • 1.4 ms P50 Latency (INT8 QDQ)       │ │ • Grounded Zero-Hallucination   │  │
   │  │ • 140 mW Active Power Draw            │ │ • Strict Anti-Therapist Guard   │  │
   │  └──────────────────┬────────────────────┘ └─────────────────┬───────────────┘  │
   │                     │                                        │                  │
   │                     ▼                                        ▼                  │
   │  ┌───────────────────────────────────────────────────────────────────────────┐  │
   │  │                    WINUI 3 FLUENT NATIVE USER INTERFACE                   │  │
   │  │  • Real-Time Activity Canvas           • Explainable Insight Inspector   │  │
   │  │  • "Ask Mirror" Natural Language       • Engineering Mode & NPU Benchmarks│  │
   │  └───────────────────────────────────────────────────────────────────────────┘  │
   │                                                                                 │
   └─────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Hardware Allocation Matrix

| Compute Component | Hardware Target | Power Budget | Purpose | Why this Hardware? |
| :--- | :--- | :--- | :--- | :--- |
| **Telemetry Ingestion & Filtering** | CPU (Efficiency Cores) | < 50 mW | Win32 hooks, regex sanitization, SQLite WAL inserts | Low compute intensity, continuous background loop. |
| **Baseline Statistics** | CPU (Background Task) | < 100 mW | Median, IQR, MAD calculations over 14-day rolling window | Infrequent (runs hourly or on app launch). |
| **Behavioral Pattern Classifier (AI Layer 1)** | **Qualcomm Hexagon NPU** (via QNN EP) | **~140 mW** | Continuous 60-min window classification (60x10 sequence) | NPU runs model in 1.4 ms with 95% less power than CPU. Crucial for all-day battery life on Snapdragon laptops. |
| **Natural Language Copilot (AI Layer 2)** | **Qualcomm Hexagon NPU** or DirectML GPU | **~1.2 W (Burst)** | Answering user questions, explaining patterns, summarizing days | On-demand burst processing without freezing UI thread or draining battery. |
| **UI Rendering & Animations** | GPU (Adreno / iGPU) | Variable | WinUI 3, Win2D vector rendering, Mica material | Native Windows composition engine. |

---

## 3. Fallback Hierarchy

Mirror provides absolute functional resilience across diverse hardware environments:

```text
[Qualcomm Hexagon NPU (QNN EP)]
             ↓ (If NPU unavailable or on x86_64)
    [DirectML GPU (Dml EP)]
             ↓ (If GPU unavailable or on low-power battery saver)
     [CPU (CPUExecutionProvider with AVX2/NEON)]
```

At every moment, the active execution provider is truthfully exposed in the UI (Diagnostics Page & Copilot header).

---

## 4. Privacy Boundary Guarantees

1. **No Outbound Sockets**: Mirror does not open any TCP/UDP sockets, HTTP clients, or DNS requests.
2. **Zero Remote Models**: Neither OpenAI, Azure, Anthropic, nor Google cloud APIs are invoked. All inference weights reside on local disk.
3. **No Window Content Inspection**: Mirror inspects only the executable name (`DisplayName`), start time, end time, and duration.
