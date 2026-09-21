# Qualcomm AI Deployment & Snapdragon NPU Optimization Notes

This document provides technical instructions for compiling, deploying, and verifying ONNX models with the **Qualcomm QNN Execution Provider (QNN EP)** on Snapdragon X Elite and Snapdragon 8cx PCs.

---

## 1. Runtime Requirements

| Component | Version / Requirement | Purpose |
| :--- | :--- | :--- |
| **OS** | Windows 11 on ARM64 (Build 26100+ recommended) | Windows Copilot+ PC environment |
| **SoC** | Snapdragon X Elite / X Plus | Qualcomm Hexagon NPU (45 TOPS) |
| **Driver** | Qualcomm NPU Driver (v2.18+) | Low-level Hexagon DSP runtime |
| **ONNX Runtime** | `Microsoft.ML.OnnxRuntime.QNN` (v1.19+) | Direct execution provider bindings |
| **QNN SDK** | Qualcomm AI Engine Direct SDK v2.28+ | Model compilation and HTP backend |

---

## 2. Compilation and Quantization (Hexagon NPU Targeting)

Hexagon NPU requires INT8 QDQ (QuantizeLinear / DequantizeLinear) or INT16/INT4 weights for optimal HTP (Hexagon Tensor Processor) execution.

### Quantization Pipeline:
1. Train model in PyTorch (`ml/train.py`).
2. Export to FP32 ONNX (`ml/export_onnx.py`).
3. Apply static QDQ quantization using representative calibration dataset (`ml/quantize_qdq.py`):
   ```bash
   python ml/quantize_qdq.py
   ```
4. Verify graph and operators:
   ```bash
   python ml/qualcomm/validate_model.py
   ```

### Generated Artifacts:
- `ml/models/mirror_pattern_v1.onnx` (FP32 Graph)
- `ml/models/mirror_pattern_v1.qdq.onnx` (INT8 QDQ Graph — Hexagon NPU target)

---

## 3. C# / WinUI 3 Runtime Execution (Dual Fallback)

In `src/Mirror.Inference/OnnxPatternClassifier.cs`, session initialization strictly attempts QNN first:

```csharp
var sessionOptions = new SessionOptions();
try
{
    // Try QNN Execution Provider on Snapdragon Hexagon NPU
    sessionOptions.AppendExecutionProvider("QNN", new Dictionary<string, string>
    {
        { "backend_path", "QnnHtp.dll" },
        { "htp_performance_mode", "burst" },
        { "htp_graph_finalization_optimization_mode", "3" }
    });
    _activeProvider = ExecutionProviderType.SnapdragonNpu;
}
catch
{
    try
    {
        // Fallback to DirectML GPU
        sessionOptions.AppendExecutionProvider_DML(0);
        _activeProvider = ExecutionProviderType.DirectML;
    }
    catch
    {
        // Fallback to CPU
        sessionOptions.AppendExecutionProvider_CPU();
        _activeProvider = ExecutionProviderType.Cpu;
    }
}
```

---

## 4. Qualcomm AI Hub Integration for Mirror Copilot

For natural language explanations and conversational grounding:
- **Model**: `Qwen2.5-0.5B-Instruct`
- **Source**: Qualcomm AI Hub (`qai_hub`)
- **Optimization**: INT4 weight-only quantization with FP16 activation targeting HTP.
- **Fallback**: High-speed deterministic template engine ensuring zero latency penalty if model weights are pending download.

---

## 5. Verification Commands

Run the discovery and benchmark suite directly:
```bash
python ml/qualcomm/discover_model.py
python ml/qualcomm/validate_model.py
python ml/qualcomm/benchmark_model.py
```
