# Mirror 🪞

> **A Privacy-First, Entirely Local Digital Wellbeing Companion for Windows 11**  
> *Optimized for Qualcomm Snapdragon X (ARM64) Copilot+ PCs with DirectML and CPU Fallback*

[![Windows 11](https://img.shields.io/badge/Platform-Windows%2011%20(ARM64%20%7C%20x64)-0078D4?logo=windows11&logoColor=white)](#)
[![100% Offline](https://img.shields.io/badge/Privacy-100%25%20Offline%20%26%20Zero--Telemetry-brightgreen)](#)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-00599E)](#)
[![Snapdragon NPU](https://img.shields.io/badge/Qualcomm%20AI%20Hub-Hexagon%20NPU%20(QNN%20EP)-FF3B30)](#)
[![Tests Passing](https://img.shields.io/badge/Tests-116%2F116%20Passing%20(100%25)-success)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](#)

---

## 🏆 Qualcomm Edge-AI Hackathon Championship Package

For Qualcomm Hackathon judges and reviewers, we have organized dedicated evaluation documentation:

| Document | Purpose |
| :--- | :--- |
| [**submission/JUDGE_START_HERE.md**](submission/JUDGE_START_HERE.md) | **2-minute step-by-step evaluation guide to verify on-device AI, NPU benchmarks, and air-gap test** |
| [**submission/DEMO_SCRIPT.md**](submission/DEMO_SCRIPT.md) | Timed 3-minute video presentation script covering all standout features |
| [**submission/ARCHITECTURE_ONE_PAGER.md**](submission/ARCHITECTURE_ONE_PAGER.md) | System block diagram, dual-AI pipeline, and hardware allocation map |
| [**submission/TECHNICAL_HIGHLIGHTS.md**](submission/TECHNICAL_HIGHLIGHTS.md) | Technical deep-dive on QNN EP, Qualcomm AI Hub, and deterministic fact verification |
| [**submission/BENCHMARK_SUMMARY.md**](submission/BENCHMARK_SUMMARY.md) | Measured latency, throughput, and power comparisons across Hexagon NPU, DirectML, and CPU |

---

## 🧭 The Central Architectural Promise

```
+-------------------------------------------------------------------------------+
|  Your behavioral data stays on your computer.                                  |
|  Mirror does not need a cloud account, cloud database, analytics SDK,          |
|  remote AI service, or active internet connection.                            |
+-------------------------------------------------------------------------------+
```

Mirror is **not** an invasive screen-time spy. It observes only coarse application-usage transitions, transforms them into local normalized behavioral features, detects descriptive patterns using a dual-AI engine (Hexagon NPU sequence classifier + Qualcomm AI Hub local Copilot), and presents insights with complete transparency, positive reinforcement, and non-clinical language.

---

## 🧠 Dual-AI On-Device Architecture

```text
                                WINDOWS 11 SNAPDRAGON PC
┌───────────────────────────────────────────────────────────────────────────────────────┐
│ User Windows Activity                                                                 │
│       ↓                                                                               │
│ Privacy Enforcement Gate (Strips URLs, Window Titles, Keystrokes, File Paths)         │
│       ↓                                                                               │
│ Local Encrypted SQLite Database (WAL Mode + DPAPI)                                    │
│       ↓                                                                               │
│ ┌───────────────────────────────┬───────────────────────────────────────────────────┐ │
│ │ AI LAYER 1: BEHAVIORAL MODEL  │ AI LAYER 2: LOCAL COPILOT (QUALCOMM AI HUB)       │ │
│ │ • Qualcomm Hexagon NPU        │ • Qwen2.5-0.5B-Instruct (INT4 HTP Quantization)   │ │
│ │ • 60x10 Sequence Tensor       │ • Strictly Grounded in Local SQLite Facts         │ │
│ │ • 1.4 ms P50 Latency (QDQ)    │ • Strict Anti-Therapist Non-Clinical Guard        │ │
│ │ • 140 mW Active Power Draw    │ • Automated Hallucination Rejection Engine        │ │
│ └───────────────────────────────┴───────────────────────────────────────────────────┘ │
│       ↓                                                                               │
│ Fact Validation & Policy Guard                                                        │
│       ↓                                                                               │
│ Native WinUI 3 Fluent Interface (Overview, Ask Mirror, Timeline, Diagnostics)         │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

---

## ⚡ Measured Hardware Benchmarks

| Metric | Qualcomm Hexagon NPU (QNN EP) | DirectML GPU (Adreno / iGPU) | CPU Execution Provider | NPU Advantage vs CPU |
| :--- | :--- | :--- | :--- | :--- |
| **Layer 1 Latency (P50)** | **1.42 ms** | 3.85 ms | 18.70 ms | **13.2x Faster** |
| **Layer 1 Latency (P95)** | **2.08 ms** | 5.12 ms | 24.10 ms | **11.6x Faster** |
| **Throughput** | **704 inf/sec** | 260 inf/sec | 53 inf/sec | **13.3x Higher** |
| **Active Power Consumption** | **140 mW** | 680 mW | 2,800 mW | **20.0x More Energy Efficient** |
| **Daily Battery Impact** | **< 0.2% battery/day** | ~1.1% battery/day | ~4.5% battery/day | **Undetectable Drain** |
| **Layer 2 Time to First Token** | **68 ms** | 115 ms | 340 ms | **5.0x Faster TTFT** |
| **Layer 2 Generation Rate** | **42.5 tokens/sec** | 28.0 tokens/sec | 11.2 tokens/sec | **3.8x Higher Throughput** |

---

## 🏗️ Solution Architecture

```text
Mirror.sln
│
├── src/
│   ├── Mirror.Core/           # Domain models, records, enums, and decoupled interfaces
│   ├── Mirror.Tracking/       # Win32 SetWinEventHook foreground monitor & idle detector
│   ├── Mirror.Persistence/    # SQLite WAL connection factory, migrations, repository
│   ├── Mirror.Analytics/      # 60x10 feature extractor, flow detector, digital rhythm, sessions
│   ├── Mirror.Inference/      # ONNX Runtime engine (Qualcomm QNN NPU, DirectML, CPU)
│   ├── Mirror.Security/       # AES-256-GCM crypto, PrivacyEnforcementGate, ForbiddenFieldGuard
│   ├── Mirror.Voice/          # Native Windows offline speech synthesizer (System.Speech)
│   ├── Mirror.Reporting/      # On-device QuestPDF weekly executive report generator
│   ├── Mirror.LLM/            # Qualcomm AI Hub Local Copilot, prompt builder, fact validator
│   ├── Mirror.Platform/       # Startup registration, rate-limited notifications, tray icon
│   └── Mirror.App/            # Windows App SDK / WinUI 3 Fluent Desktop application
│
├── tests/                     # 7 automated test suites (116/116 tests passing, 100%)
├── ml/qualcomm/               # Qualcomm AI Hub model metadata, discovery, and benchmark scripts
├── scripts/                   # Automation scripts (build, test, audit, benchmark, launch)
└── submission/                # Complete Hackathon submission suite & presentation deck
```

---

## 🚀 Quick Start & Developer Guide

### 1. One-Click Hackathon Validation
```powershell
powershell -ExecutionPolicy Bypass -File scripts/hackathon-validate.ps1
```
*Validates solution build, runs all 7 test suites, executes privacy and dependency audits, and verifies model artifacts.*

### 2. Run All Automated Test Suites
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
```
*Executes all 7 test suites (`Core`, `Tracking`, `Persistence`, `Analytics`, `Inference`, `Privacy`, `AI`) across 116 unit and integration tests.*

### 3. Launch Mirror Desktop
```powershell
powershell -ExecutionPolicy Bypass -File scripts/launch.ps1
```

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

