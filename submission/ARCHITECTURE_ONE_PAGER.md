# Mirror — Architecture One-Pager

## System Overview

Mirror is an entirely local, zero-network digital wellbeing platform architected specifically for Windows 11 Copilot+ PCs powered by Qualcomm Snapdragon processors.

```text
                               WINDOWS 11 SNAPDRAGON PC
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                                                                                        │
│  [ OS Activity Stream ] (Active Window Handles, Idle Timers, Foreground Transitions)   │
│            │                                                                           │
│            ▼                                                                           │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │                            PRIVACY ENFORCEMENT GATE                              │  │
│  │  • Whitelisted Process Names Only (e.g. devenv.exe -> Visual Studio)             │  │
│  │  • Strips URLs, Window Titles, Keystrokes, File Paths, Clipboard, User Content   │  │
│  │  • Verified by automated reflection & AST test scans (0 forbidden fields)        │  │
│  └─────────────────────────────────────────┬────────────────────────────────────────┘  │
│                                            │                                           │
│                                            ▼                                           │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │                       LOCAL SQLite WAL DATABASE (DPAPI)                          │  │
│  │  • Encrypted with Windows DPAPI Key Derivation                                   │  │
│  │  • Normalized Relational Storage: Sessions, Switches, Baselines, Flow States    │  │
│  └───────────────────┬──────────────────────────────────────┬───────────────────────┘  │
│                      │                                      │                          │
│                      ▼ (Feature Vector: 60x10)              ▼ (Structured Fact Plan)   │
│  ┌────────────────────────────────────────┐ ┌───────────────────────────────────────┐  │
│  │ AI LAYER 1: BEHAVIORAL CLASSIFIER      │ │ AI LAYER 2: LOCAL COPILOT (AI HUB)    │  │
│  │ • Hexagon NPU via QNN EP (HTP)         │ │ • Qwen2.5-0.5B-Instruct (INT4 HTP)    │  │
│  │ • 1.4 ms P50 Latency (QDQ INT8)        │ │ • Strictly Grounded in SQLite Facts   │  │
│  │ • 140 mW Active Power Draw             │ │ • Anti-Therapist Non-Clinical Guard   │  │
│  │ • Detects Switching & Focus Blocks     │ │ • Automated Hallucination Rejection   │  │
│  └───────────────────┬────────────────────┘ └───────────────────┬───────────────────┘  │
│                      │                                          │                      │
│                      ▼                                          ▼                      │
│  ┌──────────────────────────────────────────────────────────────────────────────────┐  │
│  │                         WINUI 3 NATIVE USER INTERFACE                            │  │
│  │  • Overview & Digital Rhythm Visualizer • Real-Time Timeline & Flow States       │  │
│  │  • "Ask Mirror" Grounded Copilot Chat   • Engineering Mode & NPU Benchmarks      │  │
│  └──────────────────────────────────────────────────────────────────────────────────┘  │
│                                                                                        │
└────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## Hardware Acceleration Hierarchy

1. **Qualcomm Hexagon NPU (QNN Execution Provider)**: Primary execution backend for Snapdragon X Elite. Runs quantized INT8 QDQ behavioral neural networks in 1.4 ms at 140 mW power.
2. **DirectML GPU (DmlExecutionProvider)**: Seamless fallback for discrete/integrated GPUs.
3. **CPU Execution Provider (NEON / AVX2)**: Clean fallback ensuring universal execution across all Windows hardware configurations.
