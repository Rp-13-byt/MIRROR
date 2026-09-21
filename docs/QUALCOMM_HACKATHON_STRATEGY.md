# Mirror — Qualcomm Hackathon Championship Strategy & Evaluation Matrix

**Project:** Mirror — Privacy-First Local Behavioral Intelligence Platform  
**Target Platform:** Windows 11 on Snapdragon X Elite / ARM64 (with x64 portable execution)  
**Hardware Accelerator:** Qualcomm Hexagon NPU via Qualcomm AI Runtime (QNN) Execution Provider  
**Local Language Model:** Qualcomm AI Hub Optimized Qwen2.5-0.5B-Instruct (INT4/INT8 QDQ)  

---

## 1. Hackathon Evaluation Criteria Mapping

| Evaluation Dimension | Weight | Mirror Championship Solution | Hard Technical Evidence & Benchmarks |
| :--- | :--- | :--- | :--- |
| **1. Technological Implementation** | 25% | **Dual-AI Architecture on Snapdragon:**<br>1. *Behavioral Model:* 60×10 feature sequence classifier running on Qualcomm Hexagon NPU via QNN EP.<br>2. *Local Copilot:* Qualcomm AI Hub Language Model with structured-grounding, fact-checking, and anti-therapist policy guard.<br>3. *Privacy Enforcement Gate:* Hard architectural barrier preventing raw OS data from reaching persistence or AI. | • Automated test suite (7 projects, 100% pass rate)<br>• Real QNN Execution Provider integration<br>• Measured P50/P95 latencies and cold-start timings<br>• Zero-telemetry assembly and dependency verification |
| **2. Design & Native UX** | 20% | **Windows 11 Fluent WinUI 3 Application:**<br>• Real-time reactive dashboard with Mica backdrop and high-DPI scaling.<br>• "Ask Mirror" conversational interface with one-click chips, evidence drilldowns, and action cards.<br>• Engineering Mode exposing live architecture visualizer and AI pipeline monitor.<br>• Accessible high-contrast typography, zero gamification, zero guilt-inducing notifications. | • Tested across window resizing, light/dark themes, multi-monitor setups<br>• Textual table alternatives for all charts<br>• Non-blocking background worker pipelines |
| **3. Potential Impact** | 20% | **Digital Sovereignty & Observability:**<br>• Solves the digital surveillance dilemma: users gain deep behavioral clarity without sending keystrokes, URLs, or browsing data to cloud data-brokers.<br>• Broad application: developers, enterprise privacy environments, students, researchers, remote workers. | • Encrypted exportable Wellbeing Wallet (`.mirrorwallet`)<br>• Full zero-residue data purge and deletion preview<br>• Local weekly executive PDF reports generated on-device |
| **4. Quality of the Idea** | 15% | **"Less Data, Local Intelligence":**<br>• Displaces invasive trackers that record keystrokes/screenshots.<br>• Replaces arbitrary clinical labeling (e.g. "ADHD", "burnout") with objective, non-clinical behavioral telemetry (session shapes, switch rates, digital rhythm).<br>• Statistical personalization without cloud retraining. | • Deterministic rule authoritativeness prevents AI hallucinations<br>• Formal fusion engine with explicit rule/ML agreement matrix<br>• Model abstention (`UNCERTAIN` state) when confidence is low |
| **5. Best Use of Qualcomm AI Hub** | 20% | **Deep, User-Visible Hardware Acceleration:**<br>• Real Qualcomm AI Hub model deployment (`ml/qualcomm/`).<br>• Intelligently routed computation: Hexagon NPU handles continuous inference without battery penalties; CPU handles storage & simple analytics.<br>• Live NPU vs. CPU benchmarking lab with truthful hardware capability detection. | • Verifiable model metadata pinned to Qualcomm AI Hub<br>• Reproducible benchmarking scripts (`benchmark_model.py`)<br>• Truthful fallback detection: QNN -> DirectML -> CPU |

---

## 2. Product Differentiation Matrix

| Dimension | Standard Screen Time Trackers | Cloud-Connected AI Tools | Mirror (Snapdragon Edge-AI) |
| :--- | :--- | :--- | :--- |
| **Data Storage** | Local SQLite or Cloud | Cloud Server / Remote DB | **100% Local Encrypted SQLite (WAL mode)** |
| **Network Dependency** | Periodic sync / telemetry | Mandatory high-bandwidth API | **Zero Network Calls (Operates fully air-gapped)** |
| **Surveillance Scope** | Often logs window titles / URLs | Full screen OCR / keystrokes | **Sanitized Process Keys Only (Titles/URLs Rejected)** |
| **AI Inference** | None or heuristic rules | Remote Cloud LLMs (OpenAI/Anthropic) | **Snapdragon Hexagon NPU + Qualcomm AI Hub** |
| **Explainability** | Black-box graphs | Generated narrative without evidence | **Deterministic Grounding + "Show Evidence" Drilldown** |
| **Clinical Stance** | Pejorative ("Addicted", "Unproductive") | Speculative pseudo-therapy | **Strict Non-Clinical Behavioral Vocabulary** |
| **User Ownership** | Proprietary lock-in | Cloud profile deletion requests | **AES-256-GCM Encrypted Wallet + Deletion Preview** |

---

## 3. Demo Storyboard for Evaluators

1. **Scene 1 (The Problem & The Guarantee):** Launch Mirror. Show Privacy Center: 0 outbound connections, 0 HTTP sockets, zero window titles logged.
2. **Scene 2 (Real Activity & Sessionization):** Demonstrate live tracking switching between VS Code, Edge, and Windows Terminal. Sessionizer aggregates transitions locally.
3. **Scene 3 (AI Layer 1: Behavioral Detection on NPU):** Rapid switching triggers `HighSwitchingBurst`. Insight Explorer displays rule metrics, NPU corroboration, and confidence.
4. **Scene 4 (AI Layer 2: Ask Mirror Local Copilot):** Ask *"How did I use my computer today?"*. Qualcomm AI Hub model answers using strictly grounded facts. Click *"Show Evidence"* to inspect exact numbers.
5. **Scene 5 (The Air-Gap Proof):** Disconnect Wi-Fi/Ethernet. Ask *"What changed compared to my baseline?"*. Instant, validated response generated 100% offline.
6. **Scene 6 (Hardware Benchmarking Lab):** Open Diagnostics. Run live NPU vs CPU inference benchmark. Observe low-latency Hexagon NPU acceleration.
7. **Scene 7 (Data Sovereignty):** Open Data Inventory, review table row counts, generate `.mirrorwallet` backup, and inspect Deletion Preview.
