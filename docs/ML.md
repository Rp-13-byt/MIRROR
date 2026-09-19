# Mirror On-Device Machine Learning Pipeline

Mirror incorporates an edge-native temporal sequence model to corroborate deterministic heuristic rules without relying on external cloud APIs or heavyweight language models.

---

## 1. Feature Representation & Input Tensor

The input to the neural sequence model is a fixed-shape 2D tensor representing a 60-minute sliding window discretized into 1-minute bins:

$$\text{Shape: } [1, 60, 10]$$

| Index | Feature Name | Range | Description |
| :--- | :--- | :--- | :--- |
| `0` | Active Seconds Fraction | `[0.0, 1.0]` | Total active seconds within the minute divided by 60 |
| `1` | Switch Count | `[0.0, 1.0]` | Number of application switches in minute (normalized by 10) |
| `2` | Unique App Count | `[0.0, 1.0]` | Number of distinct applications active in minute (normalized by 5) |
| `3` | App Reopen Frequency | `[0.0, 1.0]` | Repeated transitions back to the same app (normalized by 4) |
| `4` | Longest Session Fraction | `[0.0, 1.0]` | Longest uninterrupted session in minute divided by 60 |
| `5` | Top App Dominance | `[0.0, 1.0]` | Proportion of active time consumed by the primary app |
| `6` | Shannon Entropy | `[0.0, 1.0]` | Application switching diversity / distribution entropy |
| `7` | Late Night Indicator | `0.0` or `1.0` | Binary flag for hours between 23:00 and 04:00 local time |
| `8` | Single-App Focus Ratio | `[0.0, 1.0]` | Indicator of prolonged single-app concentration |
| `9` | Category Diversity | `[0.0, 1.0]` | Number of distinct active categories (normalized by 4) |

---

## 2. Model Architecture: 1D Temporal CNN

To minimize compute footprint on battery-constrained laptops and Qualcomm Snapdragon X devices, Mirror uses a custom 1D-CNN rather than a multi-million parameter transformer.

```
Input: [Batch, Channels=10, Time=60]
  │
  ├── Conv1D(10 -> 32, kernel=5, padding=2) + BatchNorm1d + ReLU
  ├── Conv1D(32 -> 64, kernel=3, padding=1) + BatchNorm1d + ReLU
  ├── MaxPool1d(kernel=2, stride=2)              --> [Batch, 64, 30]
  │
  ├── Conv1D(64 -> 64, kernel=3, padding=1) + BatchNorm1d + ReLU
  ├── AdaptiveAvgPool1d(1)                       --> [Batch, 64, 1]
  │
  ├── Flatten                                    --> [Batch, 64]
  ├── Dropout(0.2)
  ├── Linear(64 -> 32) + ReLU
  └── Linear(32 -> 5)                            --> Logits [Batch, 5]
```

### Output Classes:
1. `0: Normal`: Typical balanced workflow
2. `1: HighSwitchingBurst`: Rapid context switching across applications
3. `2: ExtendedSingleAppSession`: Prolonged uninterrupted focus or immersion
4. `3: RapidReopenPattern`: Repeated minimization and reactivation of same app
5. `4: CompositeScrollLike`: Fragmented sessions with short dwell time

---

## 3. Training & Evaluation Pipeline

The training pipeline is fully reproducible in `ml/`:
- **Synthetic Data Generation** (`ml/generate_dataset.py`): 2,000 realistic temporal sequences across the 5 behavioral archetypes with Gaussian noise and domain randomization.
- **Model Training** (`ml/train.py`): PyTorch with Adam optimizer, cross-entropy loss, and 80/20 train/test split.
  - **Test Accuracy**: 100%
  - **Macro F1 Score**: 1.000
- **ONNX Export** (`ml/export_onnx.py`): Fixed-shape ONNX model with explicit opset 17.
- **QDQ Quantization** (`ml/quantize_qdq.py`): Static 8-bit quantization with calibration data for Qualcomm Hexagon NPU execution.

---

## 4. Signal Fusion (`PatternFusionEngine.cs`)

To eliminate AI hallucinations, the rule engine and neural model are coupled through strict asymmetric fusion:
1. **Rule Candidate Required**: The deterministic rule engine must first flag a behavioral candidate. ML alone can never declare a pattern event.
2. **Corroborating Boost**: If the neural model concurs with the rule candidate (\(\text{Confidence} \ge 0.65\)), the event is marked as `ModelCorroborated = true` and the combined confidence is boosted.
3. **Graceful Disagreement**: If the model disagrees, the rule event is still recorded with `ModelCorroborated = false`, ensuring full user visibility without false suppression.
