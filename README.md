# Mirror 🪞

> **A Privacy-First, Entirely Local Digital Wellbeing Companion for Windows 11**  
> *Engineered for Qualcomm Snapdragon X Copilot+ PCs with Qualcomm AI Hub, Hexagon NPU, DirectML, and CPU Fallback*

[![Windows 11](https://img.shields.io/badge/Platform-Windows%2011%20(ARM64%20%7C%20x64)-0078D4?logo=windows11&logoColor=white)](#)
[![100% Offline](https://img.shields.io/badge/Privacy-100%25%20Offline%20%26%20Zero--Telemetry-brightgreen)](#)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-00599E)](#)
[![Qualcomm AI Hub](https://img.shields.io/badge/Qualcomm%20AI%20Hub-Hexagon%20NPU%20(QNN%20EP)-FF3B30)](#)
[![Tests Passing](https://img.shields.io/badge/Tests-116%2F116%20Passing%20(100%25)-success)](#)
[![Readiness Scorecard](https://img.shields.io/badge/Hackathon%20Readiness-8%2F8%20Checks%20Passed-10B981)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](#)

---

## 🏆 Qualcomm Edge-AI Hackathon Championship Package

For Qualcomm Hackathon judges and technical evaluators, complete submission assets and evaluation guides are organized below:

| Submission Asset | Format | Purpose |
| :--- | :--- | :--- |
| [**submission/JUDGE_START_HERE.md**](submission/JUDGE_START_HERE.md) | Markdown | **2-minute evaluation guide to verify on-device AI, NPU benchmarks, and air-gap test** |
| [**submission/Mirror_Pitch_Deck.pptx**](submission/Mirror_Pitch_Deck.pptx) | PowerPoint (16:9) | 11-slide pitch deck with embedded application screenshots, architecture diagrams, and benchmark charts |
| [**submission/Mirror_Pitch_Deck.pdf**](submission/Mirror_Pitch_Deck.pdf) | PDF (16:9) | High-fidelity presentation deck ready for submission review |
| [**submission/Mirror_Brief_Project_Description.docx**](submission/Mirror_Brief_Project_Description.docx) | Word (.docx) | Formatted project brief with figures, benchmark comparison tables, and architectural details |
| [**submission/Mirror_Brief_Project_Description.pdf**](submission/Mirror_Brief_Project_Description.pdf) | PDF Document | Printable executive project description |
| [**submission/DEMO_SCRIPT.md**](submission/DEMO_SCRIPT.md) | Markdown | Timed 3-minute video presentation script |
| [**submission/ARCHITECTURE_ONE_PAGER.md**](submission/ARCHITECTURE_ONE_PAGER.md) | Markdown | System block diagram, dual-AI pipeline, and hardware allocation map |
| [**submission/TECHNICAL_HIGHLIGHTS.md**](submission/TECHNICAL_HIGHLIGHTS.md) | Markdown | Deep-dive on QNN EP, Qualcomm AI Hub, and deterministic fact verification |
| [**submission/BENCHMARK_SUMMARY.md**](submission/BENCHMARK_SUMMARY.md) | Markdown | Measured latency, throughput, and power comparisons across Hexagon NPU, DirectML, and CPU |

---

## 🧭 The Central Architectural Promise

```text
+-------------------------------------------------------------------------------+
|  Your behavioral data stays on your computer.                                  |
|  Mirror does not need a cloud account, cloud database, analytics SDK,          |
|  remote AI service, or active internet connection.                            |
+-------------------------------------------------------------------------------+
```

Mirror is **not** an invasive screen-time spy. It observes only coarse application-usage transitions, transforms them into local normalized behavioral features, detects descriptive patterns using a dual-AI engine (Hexagon NPU sequence classifier + Qualcomm AI Hub local Copilot), and presents insights with complete transparency, positive reinforcement, and non-clinical language.

---

## 🧠 Dual-AI On-Device Architecture

Mirror establishes a multi-tier edge-AI pipeline separating fast, continuous background monitoring from on-demand conversational intelligence:

```text
                                WINDOWS 11 SNAPDRAGON PC
┌───────────────────────────────────────────────────────────────────────────────────────┐
│ User Windows Activity (WinEventHooks / Idle Detectors)                                │
│       ↓                                                                               │
│ Privacy Enforcement Gate (Strips URLs, Window Titles, Keystrokes, File Paths)         │
│       ↓                                                                               │
│ Local Encrypted SQLite Database (WAL Mode + DPAPI Key Derivation)                     │
│       ↓                                                                               │
│ ┌───────────────────────────────┬───────────────────────────────────────────────────┐ │
│ │ AI LAYER 1: BEHAVIORAL MODEL  │ AI LAYER 2: LOCAL COPILOT (QUALCOMM AI HUB)       │ │
│ │ • Qualcomm Hexagon NPU        │ • Qwen2.5-0.5B-Instruct (INT4 HTP Quantization)   │ │
│ │ • 60x10 Sequence Tensor       │ • Strictly Grounded in Local SQLite Facts         │ │
│ │ • 1.4 ms P50 Latency (QDQ)    │ • Strict Anti-Therapist Non-Clinical Guard        │ │
│ │ • 140 mW Active Power Draw    │ • Automated Hallucination Rejection Engine        │ │
│ └───────────────────────────────┴───────────────────────────────────────────────────┘ │
│       ↓                                                                               │
│ Fact Validation & Policy Guard (Rejects ungrounded claims & clinical speculation)     │
│       ↓                                                                               │
│ Native WinUI 3 Fluent Interface (Overview, Ask Mirror, Timeline, Diagnostics)         │
└───────────────────────────────────────────────────────────────────────────────────────┘
```

1. **AI Layer 1 — Continuous Behavioral Classifier (Qualcomm Hexagon NPU)**:
   - Processes a 60-minute sliding window of normalized interaction telemetry (active duration, idle intervals, switch velocity, category diversity, rapid reopens) represented as a `[1, 60, 10]` tensor.
   - Quantized with static INT8 QDQ (`QuantizeLinear` / `DequantizeLinear`) operators to execute directly on the Hexagon Tensor Processor (HTP) via the Qualcomm QNN Execution Provider (`QNN EP`).
   - Runs in **1.42 ms steady-state latency** at **140 mW active power** (a **13.2x latency reduction and 20x energy efficiency gain** compared to traditional CPU inference).
2. **AI Layer 2 — Grounded Local Copilot (Qualcomm AI Hub)**:
   - An on-device conversational intelligence engine powered by a Qualcomm AI Hub quantized small language model (`Qwen2.5-0.5B-Instruct` INT4/HTP).
   - Generates natural language activity summaries, trend explanations, and workflow comparisons in under 100 ms without sending a single byte over the network.

---

## 📸 Application Showcase

### 1. Today's Observability & Flow-State Recognition
*Continuous second-by-second focus tracking, live category breakdown, and deep work celebration.*
![Mirror Overview Dashboard](docs/images/overview_flow_state.png)

### 2. "Ask Mirror" Natural Language Copilot (Qualcomm AI Hub)
*Conversational Q&A grounded in verified local facts with one-click quick chips, raw evidence drawer, and strict non-clinical safety boundaries.*
![Mirror Copilot Architecture](submission/assets/behavioral_patterns_slm.png)

### 3. Personalization Without Profiling: Adaptive Baseline & Digital Rhythm
*Rolling 14-day statistical envelope (median + variance) with strict Day 7 learning gate, >2σ anomaly alerting, and 24-hour rhythm segmentation.*
![Mirror Longitudinal Trends & Baseline](docs/images/trends_adaptive_baseline.png)

### 4. Real-Time Activity Timeline & Session Structure Archetypes
*Sub-second chronological transition tracking that archives sessions on window switch, categorizing blocks into Focused, Fragmented, Switch-Heavy, or Reopen-Heavy without moralizing language.*
![Mirror Activity Timeline](docs/images/activity_timeline.png)

---

## ✨ Standout Innovations

1. **🤖 Qualcomm AI Hub Local Copilot ("Ask Mirror")**: Natural language reasoning running 100% on-device. Users can ask *"Summarize today"*, *"What changed this week?"*, or *"Show longest session"*. Mediated through `LocalQueryPlanner` to assemble approved local SQLite aggregates without free-form SQL.
2. **🎯 Deterministic Grounding & Anti-Hallucination Engine**: `CopilotFactChecker` cross-references every numeric assertion in model outputs against verified local facts in `MirrorCopilotContext`. If discrepancies are detected, output is rejected and replaced by deterministic ground truth.
3. **🛡️ Strict Non-Clinical Ethical Safety Guard**: `CopilotPolicyGuard` deterministically intercepts psychiatric and clinical terms (`burnout`, `depression`, `anxiety`, `ADHD`, `addiction`, `dopamine`). Mirror delivers objective, neutral behavioral metrics without ever attempting clinical diagnosis.
4. **🧠 Adaptive Personal Baseline**: Replaces arbitrary static limits with a dynamic rolling 14-day median ($\tilde{x}$) and standard deviation ($\sigma$). Anomaly alerts trigger only when activity exceeds two standard deviations ($x > \tilde{x} + 2\sigma$) with a strict Day 7 learning phase gate.
5. **🌅 24-Hour Digital Rhythm Mapping**: `DigitalRhythmService` segments daily activity into Morning (06:00–12:00), Afternoon (12:00–18:00), Evening (18:00–23:00), and Late-Night (23:00–06:00), comparing each block against personal baselines.
6. **🌊 Flow-State Deep Work Recognition**: Actively celebrates deep work by detecting $\ge 60$ minutes of uninterrupted focus in a productivity or development application with zero context switches and zero idle intervals.
7. **🔄 Active Recalibration Feedback Loop**: Users can click *"This isn't accurate"* on any flagged pattern to immediately bump detection sensitivity thresholds by $+15\%$, teaching Mirror personal habits without cloud retraining.
8. **🎙️ Voice-Narrated Daily Summary**: Native Windows offline speech synthesis (`System.Speech`) reading warm, observational daily audio briefings with interactive play/stop controls.
9. **🔐 Encrypted "Wellbeing Wallet"**: Complete data sovereignty via password-protected **AES-256-GCM** archives (`.mirrorwallet`) derived via PBKDF2 HMAC-SHA256 (100,000 iterations) with lossless one-click export and restore.
10. **📄 Weekly PDF Report Card**: Executive vector PDF report cards generated 100% locally via QuestPDF with Windows 11 Fluent aesthetic and a verifiable Local Privacy Seal.

---

## ⚡ Measured Hardware Benchmarks

Mirror was empirically benchmarked across Qualcomm Snapdragon X Elite (Hexagon NPU), DirectML GPU, and standard x86 CPU:

| Metric | Qualcomm Hexagon NPU (QNN EP) | DirectML GPU (Adreno / iGPU) | CPU Execution Provider | NPU Advantage vs CPU |
| :--- | :--- | :--- | :--- | :--- |
| **Layer 1 Latency (P50)** | **1.42 ms** | 3.85 ms | 18.70 ms | **13.2x Faster** |
| **Layer 1 Latency (P95)** | **2.08 ms** | 5.12 ms | 24.10 ms | **11.6x Faster** |
| **Throughput** | **704 inf/sec** | 260 inf/sec | 53 inf/sec | **13.3x Higher** |
| **Active Power Consumption** | **140 mW** | 680 mW | 2,800 mW | **20.0x More Energy Efficient** |
| **Daily Battery Impact** | **< 0.2% battery/day** | ~1.1% battery/day | ~4.5% battery/day | **Undetectable Drain** |
| **Layer 2 Time to First Token** | **68 ms** | 115 ms | 340 ms | **5.0x Faster TTFT** |
| **Layer 2 Generation Rate** | **42.5 tokens/sec** | 28.0 tokens/sec | 11.2 tokens/sec | **3.8x Higher Throughput** |

### Why the Hexagon NPU is Essential for Continuous Observability
Running continuous background neural inference on standard laptop CPUs draws 2.8W to 5W of power, causing thermal throttling, fan noise, and rapid battery depletion. On the **Qualcomm Hexagon NPU**, Mirror executes continuous inference at **140 milliwatts** with **1.42 ms latency**—delivering an imperceptible **<0.2% battery impact per day**.

---

## 🛡️ The 5 Architectural Privacy Commandments

1. **Zero Content Capture**: Mirror never inspects window titles, browser URLs, document paths, keystrokes, clipboard data, webcam, or microphone.
2. **Zero Remote Networking**: Static, reflection, and AST policy auditors enforce zero networking namespaces (`System.Net`, `HttpClient`, `Sockets`).
3. **Single-User Local Storage**: All data resides in an encrypted or user-owned SQLite database with Write-Ahead Logging (`mirror.db`) and DPAPI key derivation.
4. **Non-Clinical Ethics**: The system never uses medical, psychiatric, or diagnostic labels (`burnout`, `depression`, `addiction`, `adhd`).
5. **Zero-Residue Purge**: One-click complete deletion wipes all database tables, WAL files, and cached metrics instantaneously (<50 ms).

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
│   ├── Mirror.Core.Tests/
│   ├── Mirror.Tracking.Tests/
│   ├── Mirror.Persistence.Tests/
│   ├── Mirror.Analytics.Tests/
│   ├── Mirror.Inference.Tests/
│   ├── Mirror.Privacy.Tests/
│   └── Mirror.AI.Tests/       # Grounding, refusal, prompt injection, and session shape tests
│
├── ml/qualcomm/               # Qualcomm AI Hub model metadata, discovery, and benchmark scripts
│   ├── model_metadata.json
│   ├── discover_model.py
│   ├── validate_model.py
│   ├── benchmark_model.py
│   └── deployment_notes.md
│
├── scripts/                   # Automation scripts (build, test, audit, benchmark, launch)
│   ├── build.ps1
│   ├── test.ps1
│   ├── launch.ps1
│   ├── privacy-audit.ps1
│   ├── dependency-audit.ps1
│   └── hackathon-validate.ps1 # Automated 8-point readiness checker
│
└── submission/                # Complete Hackathon submission suite & presentation deck
    ├── Mirror_Pitch_Deck.pptx
    ├── Mirror_Pitch_Deck.pdf
    ├── Mirror_Brief_Project_Description.docx
    ├── Mirror_Brief_Project_Description.pdf
    ├── JUDGE_START_HERE.md
    ├── DEMO_SCRIPT.md
    ├── ARCHITECTURE_ONE_PAGER.md
    ├── TECHNICAL_HIGHLIGHTS.md
    └── BENCHMARK_SUMMARY.md
```

---

## 🚀 Quick Start & Developer Guide

### Prerequisites
- Windows 11 (Version 22H2+ / Build 22621+)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows Desktop runtime)
- Python 3.10+ (for Qualcomm model tooling and benchmarks)

### 1. One-Click Hackathon Validation
```powershell
powershell -ExecutionPolicy Bypass -File scripts/hackathon-validate.ps1
```
*Validates solution build, runs all 7 test suites, executes privacy and dependency audits, verifies model artifacts, and checks documentation.*

### 2. Run All Automated Test Suites
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
```
*Executes all 7 test suites across 116 unit and integration tests with 100% pass rate.*

### 3. Run Privacy & Offline Architecture Audit
```powershell
powershell -ExecutionPolicy Bypass -File scripts/privacy-audit.ps1
```

### 4. Launch Mirror Desktop Application
```powershell
powershell -ExecutionPolicy Bypass -File scripts/launch.ps1
```

---

## 📄 License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.
