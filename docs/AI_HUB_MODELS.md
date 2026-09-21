# Qualcomm AI Hub Models in Mirror

**Platform:** Windows 11 ARM64 (Snapdragon X Elite / Snapdragon X Plus)  
**AI Runtime Engine:** ONNX Runtime with Qualcomm QNN Execution Provider  

---

## 1. Primary Model: Qualcomm AI Hub Language Model

* **Model Identifier:** `qwen2.5-0.5b-instruct` / `qwen3-0.6b` (Qualcomm AI Hub Optimized)
* **Provider:** Qualcomm Technologies, Inc. via Qualcomm AI Hub
* **Original Architecture:** Qwen Team (Alibaba Cloud), Apache 2.0 License
* **Target Hardware:** Qualcomm Hexagon NPU (Snapdragon X Elite / Snapdragon X Plus)
* **Optimization Pipeline:** Qualcomm AI Hub Model Converter & Quantizer
* **Quantization Scheme:** Static QDQ INT4/INT8 (weights + activations)
* **Execution Provider:** `QNNExecutionProvider` (Qualcomm Neural Network SDK v2.26+)
* **Fallback Providers:** `DirectMLExecutionProvider` (Windows GPU), `CPUExecutionProvider`
* **Input Schema:** Formatted prompt tokens with system instruction delimiter `<|im_start|>`
* **Context Limit:** 2,048 tokens (tailored for local concise behavioral summaries)
* **Purpose in Mirror:** 
  Powers the **"Ask Mirror" Local Copilot**, translating strictly grounded factual contexts (`MirrorCopilotContext`) into natural, conversational, non-clinical explanations.

---

## 2. Complementary Model: Mirror Behavioral Classifier

* **Model Identifier:** `mirror_pattern_v1.onnx` / `mirror_pattern_v1.qdq.onnx`
* **Architecture:** 2-layer 1D Temporal Convolution + BiLSTM + Dense Classifier
* **Input Tensor:** `[1, 60, 10]` (1 batch, 60 timesteps representing 60-minute sliding window, 10 behavioral features)
* **Output Tensor:** `[1, 5]` (Probabilities across `Normal`, `HighSwitchingBurst`, `ExtendedSingleAppSession`, `RapidReopenPattern`, `CompositeScrollLike`)
* **Quantization:** INT8 QDQ Hexagon NPU Optimized
* **Execution Provider:** Qualcomm Hexagon NPU via QNN
* **Purpose in Mirror:** 
  Continuous, low-power background pattern classification with sub-millisecond inference latency, operating without battery or thermal penalties on Snapdragon PCs.

---

## 3. Truthful Hardware Detection & Fallback Flow

```text
               INFERENCE REQUEST
                      │
        ┌─────────────┴─────────────┐
        ▼                           ▼
[Behavioral Classifier]     [Local Copilot SLM]
        │                           │
  Check QNN EP                Check QNN EP
  (Qualcomm Hexagon NPU)      (Qualcomm Hexagon NPU)
        │                           │
   ┌────┴────┐                 ┌────┴────┐
 [Available] [Unavailable]   [Available] [Unavailable]
   │             │             │             │
   ▼             ▼             ▼             ▼
Run on NPU    Check DirectML  Run on NPU   Check DirectML
                 │                            │
            ┌────┴────┐                  ┌────┴────┐
          [Yes]      [No]              [Yes]      [No]
            │          │                 │          │
            ▼          ▼                 ▼          ▼
         DirectML     CPU             DirectML   Local Grounded
                                                 Template Engine
```

Under no circumstances does Mirror misrepresent CPU execution as NPU execution. The active provider is queried directly from the ONNX Runtime session and displayed truthfully in Diagnostics and Engineering Mode.
