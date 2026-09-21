# Mirror — Technical Highlights & Engineering Innovations

This document details the major software engineering, machine learning, and security accomplishments within Mirror.

---

## 1. Dual-AI Edge Architecture
- **Layer 1 (Continuous Behavioral Classifier)**:
  - Fixed-size sliding tensor `[1, 60, 10]` capturing 60 minutes of normalized activity (active time, idle time, switch count, category diversity, rapid reopens).
  - Quantized with static INT8 QDQ (`QuantizeLinear` / `DequantizeLinear`) operators to compile directly onto the Qualcomm Hexagon Tensor Processor (HTP).
  - Runs in **1.42 ms** P50 latency with **140 mW** active power draw.
- **Layer 2 (Qualcomm AI Hub Local Copilot)**:
  - Integrates `Qwen2.5-0.5B-Instruct` optimized for Qualcomm Snapdragon NPU.
  - Generates conversational summaries, trend explanations, and workflow comparisons in under 100 ms.

---

## 2. Deterministic Grounding & Anti-Hallucination Pipeline
- **Zero Free-Form SQL**: The language model is never allowed to execute arbitrary SQL statements. All data access is mediated through `LocalQueryPlanner`, which queries pre-approved aggregates.
- **Context Encapsulation**: Local metrics are enclosed in strongly bounded `<LOCAL_FACTS>` blocks.
- **Automated Fact Checker (`CopilotFactChecker.cs`)**:
  - Scans model output tokens for numeric assertions.
  - Cross-checks every number against the verified `MirrorCopilotContext`.
  - If hallucinated numbers are discovered, the output is rejected and replaced by a verified deterministic template.

---

## 3. Strict Non-Clinical Safety Guard (`CopilotPolicyGuard.cs`)
- Intercepts all psychiatric and clinical terms (`burnout`, `depression`, `anxiety`, `ADHD`, `addiction`, `dopamine`).
- Enforces an ethical boundary: Mirror provides **descriptive computer telemetry, not psychological evaluation**.
- Delivers empathetic, objective metrics while advising users to speak with healthcare professionals if fatigued.

---

## 4. Privacy-by-Architecture & Zero Telemetry
- `ForbiddenFieldGuard`: Scans entities via reflection to assert that `WindowTitle`, `Url`, `Keystrokes`, and document paths are physically non-existent on domain models.
- **Zero Network Dependency**: Pinned NuGet packages contain no HTTP or analytics SDKs. Verified by automated audit scripts.
- **Data Ownership**: SQLite WAL mode with optional DPAPI encryption. One-click data inventory, export, and permanent cryptographic wipe.

---

## 5. Automated Verification & Test Coverage
- **115 passing tests** across 7 automated test suites:
  - `Mirror.AI.Tests`: Grounding, refusal, prompt injection, and session shape tests.
  - `Mirror.Privacy.Tests`: Privacy enforcement gate and forbidden field scans.
  - `Mirror.Analytics.Tests`: Baselines, pattern fusion, and flow state detection.
  - `Mirror.Inference.Tests`: ONNX tensors and execution provider fallbacks.
  - `Mirror.Tracking.Tests`: Process resolution and idle detection.
  - `Mirror.Persistence.Tests`: SQLite migrations and relational queries.
  - `Mirror.Core.Tests`: Domain logic and time handling.
