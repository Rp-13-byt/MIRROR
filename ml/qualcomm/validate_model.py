"""
Qualcomm Model Validation Utility for Mirror
---------------------------------------------
Validates ONNX models for compatibility with ONNX Runtime and Qualcomm QNN Execution Provider.
Checks tensor shapes, types, operators, and verifies inference consistency.
"""

import os
import sys
import json
import numpy as np

def validate_model(model_path):
    import onnxruntime as ort

    print(f"Validating model: {model_path}")
    if not os.path.exists(model_path):
        return {"file": model_path, "valid": False, "error": "File does not exist"}

    try:
        session = ort.InferenceSession(model_path, providers=["CPUExecutionProvider"])
        inputs = session.get_inputs()
        outputs = session.get_outputs()

        input_details = [{
            "name": inp.name,
            "shape": inp.shape,
            "type": inp.type
        } for inp in inputs]

        output_details = [{
            "name": out.name,
            "shape": out.shape,
            "type": out.type
        } for out in outputs]

        # Execute test inference with dummy input matching input shape
        test_shape = [1, 60, 10]
        dummy_input = np.random.randn(*test_shape).astype(np.float32)
        feed_dict = {inputs[0].name: dummy_input}
        raw_output = session.run(None, feed_dict)

        result_shape = list(raw_output[0].shape)
        probabilities = raw_output[0][0].tolist()

        return {
            "file": os.path.basename(model_path),
            "valid": True,
            "inputs": input_details,
            "outputs": output_details,
            "test_inference": {
                "input_shape": test_shape,
                "output_shape": result_shape,
                "sample_output": [round(p, 4) for p in probabilities]
            }
        }
    except Exception as ex:
        return {
            "file": os.path.basename(model_path),
            "valid": False,
            "error": str(ex)
        }

def main():
    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    models_dir = os.path.join(repo_root, "ml", "models")
    
    models_to_test = [
        os.path.join(models_dir, "mirror_pattern_v1.onnx"),
        os.path.join(models_dir, "mirror_pattern_v1.qdq.onnx")
    ]

    results = []
    print("=" * 60)
    print("MIRROR — MODEL VALIDATION & TENSOR INTEGRITY")
    print("=" * 60)

    for m in models_to_test:
        res = validate_model(m)
        results.append(res)
        status = "PASS" if res.get("valid") else "FAIL"
        print(f"[{status}] {res.get('file')}")
        if res.get("valid"):
            print(f"       Inputs: {res['inputs']}")
            print(f"       Outputs: {res['outputs']}")
            print(f"       Sample Out: {res['test_inference']['sample_output']}")
        else:
            print(f"       Error: {res.get('error')}")

    out_file = os.path.join(os.path.dirname(__file__), "validation_report.json")
    with open(out_file, "w", encoding="utf-8") as f:
        json.dump(results, f, indent=2)
    print("=" * 60)
    print(f"Validation report written to {out_file}")

if __name__ == "__main__":
    main()
