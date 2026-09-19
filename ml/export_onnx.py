"""
ONNX Export Pipeline for Mirror
Exports trained PyTorch sequence model to fixed-shape ONNX format.
Validates operator set, runs ONNX checker, and tests numerical parity with ONNX Runtime.
"""

import hashlib
import json
import os
import sys
import numpy as np
import onnx
import onnxruntime as ort
import torch

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

from train import MirrorPatternCNN

FEATURE_NAMES = [
    "active_seconds",
    "switch_count",
    "unique_apps",
    "reopen_count",
    "longest_session_seconds",
    "top_app_share",
    "app_entropy",
    "late_night_ratio",
    "single_app_ratio",
    "category_diversity"
]

CLASS_NAMES = [
    "NORMAL",
    "HIGH_SWITCHING",
    "EXTENDED_SESSION",
    "RAPID_REOPEN",
    "LATE_NIGHT_OR_SCROLL_LIKE"
]

def export_onnx(model_path="ml/models/mirror_pattern_model.pt", output_onnx="ml/models/mirror_pattern_v1.onnx"):
    model = MirrorPatternCNN(in_features=10, num_classes=len(CLASS_NAMES))
    model.load_state_dict(torch.load(model_path, map_location="cpu"))
    model.eval()

    # Fixed input shape: [1, 60, 10]
    dummy_input = torch.randn(1, 60, 10, dtype=torch.float32)

    os.makedirs(os.path.dirname(output_onnx), exist_ok=True)

    # Use classic TorchScript exporter (dynamo=False) for maximum ONNX / QNN compatibility
    torch.onnx.export(
        model,
        dummy_input,
        output_onnx,
        export_params=True,
        opset_version=17,
        do_constant_folding=True,
        input_names=["behavioral_sequence"],
        output_names=["pattern_logits"],
        dynamic_axes=None,  # Fixed shape [1, 60, 10] for optimal NPU execution
        dynamo=False
    )
    print(f"Exported ONNX model to {output_onnx}")

    # 1. Check with ONNX validator
    onnx_model = onnx.load(output_onnx)
    onnx.checker.check_model(onnx_model)
    print("ONNX model validated successfully by onnx.checker.")

    # 2. Numerical validation with ONNX Runtime
    ort_session = ort.InferenceSession(output_onnx, providers=["CPUExecutionProvider"])
    ort_inputs = {"behavioral_sequence": dummy_input.numpy()}
    ort_outputs = ort_session.run(None, ort_inputs)[0]

    with torch.no_grad():
        pt_outputs = model(dummy_input).numpy()

    diff = float(np.max(np.abs(ort_outputs - pt_outputs)))
    print(f"Maximum numerical divergence between PyTorch and ONNX Runtime: {diff:.6e}")
    assert diff < 1e-4, f"Parity check failed, diff: {diff}"

    # 3. Compute SHA256 checksum
    with open(output_onnx, "rb") as f:
        sha256 = hashlib.sha256(f.read()).hexdigest()

    # 4. Save metadata
    metadata = {
        "model_name": "mirror_pattern",
        "version": "1.0.0",
        "input_shape": [1, 60, 10],
        "quantized": False,
        "training_dataset": "synthetic-v1",
        "feature_schema": 1,
        "sha256": sha256,
        "features": FEATURE_NAMES,
        "classes": CLASS_NAMES
    }

    metadata_path = "ml/models/metadata/mirror_pattern_v1.json"
    os.makedirs(os.path.dirname(metadata_path), exist_ok=True)
    with open(metadata_path, "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"Saved model metadata to {metadata_path}")
    return output_onnx

if __name__ == "__main__":
    export_onnx()
