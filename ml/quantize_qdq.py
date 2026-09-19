"""
QDQ Quantization Pipeline for Mirror
Performs static INT8 Quantize-Dequantize (QDQ) quantization for Qualcomm Hexagon NPU / QNN deployment.
"""

import hashlib
import json
import os
import sys
import numpy as np
import onnx
import onnxruntime as ort
from onnxruntime.quantization import (
    CalibrationDataReader,
    QuantFormat,
    QuantType,
    quantize_static
)

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")
if hasattr(sys.stderr, "reconfigure"):
    sys.stderr.reconfigure(encoding="utf-8")

class BehavioralCalibrationDataReader(CalibrationDataReader):
    def __init__(self, dataset_path="ml/dataset.npz", num_samples=50):
        super().__init__()
        data = np.load(dataset_path)
        X = data["X"][:num_samples]  # [num_samples, 60, 10]
        self.data_iter = iter([{"behavioral_sequence": np.expand_dims(x, axis=0)} for x in X])

    def get_next(self):
        return next(self.data_iter, None)

def quantize_qdq(
    input_onnx="ml/models/mirror_pattern_v1.onnx",
    output_qdq_onnx="ml/models/mirror_pattern_v1.qdq.onnx",
    dataset_path="ml/dataset.npz"
):
    print("Beginning QDQ quantization...")
    calib_reader = BehavioralCalibrationDataReader(dataset_path=dataset_path, num_samples=100)

    # Static QDQ quantization for NPU / QNN compatibility
    quantize_static(
        model_input=input_onnx,
        model_output=output_qdq_onnx,
        calibration_data_reader=calib_reader,
        quant_format=QuantFormat.QDQ,
        activation_type=QuantType.QInt8,
        weight_type=QuantType.QInt8,
        per_channel=False,
        reduce_range=False
    )
    print(f"Quantized model generated: {output_qdq_onnx}")

    # Validate with onnx.checker
    qdq_model = onnx.load(output_qdq_onnx)
    onnx.checker.check_model(qdq_model)
    print("QDQ model validated successfully with onnx.checker.")

    # Validate inference execution with ONNX Runtime
    session = ort.InferenceSession(output_qdq_onnx, providers=["CPUExecutionProvider"])
    dummy_input = np.random.randn(1, 60, 10).astype(np.float32)
    output = session.run(None, {"behavioral_sequence": dummy_input})[0]
    print(f"QDQ inference test succeeded. Output shape: {output.shape}")

    # Compute checksum
    with open(output_qdq_onnx, "rb") as f:
        sha256 = hashlib.sha256(f.read()).hexdigest()

    # Save quantized metadata
    metadata = {
        "model_name": "mirror_pattern_qdq",
        "version": "1.0.0",
        "input_shape": [1, 60, 10],
        "quantized": True,
        "quantization_format": "QDQ_INT8",
        "target_execution_provider": "QNNExecutionProvider",
        "training_dataset": "synthetic-v1",
        "feature_schema": 1,
        "sha256": sha256
    }

    metadata_path = "ml/models/metadata/mirror_pattern_v1_qdq.json"
    with open(metadata_path, "w") as f:
        json.dump(metadata, f, indent=2)

    print(f"Saved QDQ metadata to {metadata_path}")
    return output_qdq_onnx

if __name__ == "__main__":
    quantize_qdq()
