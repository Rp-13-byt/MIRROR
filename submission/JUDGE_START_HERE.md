# Mirror — Judge Quickstart Guide (2-Minute Evaluation)

Welcome to the **Mirror** Qualcomm Hackathon evaluation! Follow this step-by-step 2-minute walkthrough to verify on-device AI, hardware truthfulness, privacy guarantees, and local intelligence.

---

## Step 1: Run One-Click Validation (30 Seconds)

Run our automated validation script to verify the entire build, all 7 unit test suites, zero-telemetry audits, and model files:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/hackathon-validate.ps1
```
*Expected Result: All 8 checks show `[PASS]` and all 7 test suites pass 100%.*

---

## Step 2: Launch Application (10 Seconds)

```powershell
powershell -ExecutionPolicy Bypass -File scripts/launch.ps1
```

The native Windows 11 WinUI 3 desktop interface will open immediately.

---

## Step 3: Verify Hardware Truthfulness & NPU Benchmarks (30 Seconds)

1. In the left navigation pane, click **"NPU & Diagnostics"**.
2. Notice the **Dual-AI Pipeline Architecture Inspector**:
   - **Layer 1**: Shows `[1, 60, 10]` tensor geometry running on Snapdragon NPU via QNN EP (or active fallback on non-ARM PCs).
   - **Layer 2**: Shows `Qwen2.5-0.5B-Instruct` model specs from Qualcomm AI Hub.
   - **Air-Gap Card**: Truthfully reports `0 Sockets Open, Zero Telemetry`.
3. Click **"Run Comparative Hardware Benchmark"**:
   - Watch the app execute 100 on-device inference passes in real time.
   - Observe the measured sub-millisecond P50 latency and throughput.

---

## Step 4: Test "Ask Mirror" Natural Language Copilot (40 Seconds)

1. In the left navigation pane, click **"Ask Mirror"**.
2. Click the quick chip **"Summarize today"**:
   - Notice the instantaneous local response grounded in your active session data.
3. Click **"Show Grounded Evidence (Local Facts)"**:
   - Inspect the exact raw local facts passed to the model from local SQLite.
4. Test the **Strict Non-Clinical Safety Guard**:
   - Click the orange chip **"Am I burned out?"** (or type *"Do I have ADHD?"*).
   - Notice that Mirror immediately refuses speculative psychological diagnosis and instead delivers neutral, objective activity facts.

---

## Step 5: The "Air-Gap Test" (Disconnect Network)

To prove that Mirror is 100% on-device and zero-cloud:
1. Turn off your Wi-Fi or unplug your Ethernet cable.
2. Ask any question in **"Ask Mirror"** or run the **NPU Benchmark**.
3. Mirror operates with zero degradation and zero network latency.
