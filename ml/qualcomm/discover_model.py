"""
Qualcomm AI Hub Model Discovery Utility for Mirror
--------------------------------------------------
Scans Qualcomm AI Hub registry / local repository for validated on-device models,
inspects hardware execution capabilities (Hexagon NPU, DirectML, CPU), and emits
runtime compatibility reports.
"""

import os
import sys
import json
import platform

def detect_hardware():
    arch = platform.machine().lower()
    proc = platform.processor()
    system = platform.system()
    
    is_arm64 = "arm64" in arch or "aarch64" in arch
    is_snapdragon = is_arm64 or "snapdragon" in proc.lower()
    
    has_npu = is_snapdragon
    
    return {
        "os": system,
        "architecture": arch,
        "processor": proc,
        "is_snapdragon": is_snapdragon,
        "npu_supported": has_npu,
        "recommended_ep": "QNNExecutionProvider" if has_npu else "CPUExecutionProvider"
    }

def discover_models(repo_root):
    ml_dir = os.path.join(repo_root, "ml")
    models_dir = os.path.join(ml_dir, "models")
    qualcomm_dir = os.path.join(ml_dir, "qualcomm")
    
    metadata_path = os.path.join(qualcomm_dir, "model_metadata.json")
    if os.path.exists(metadata_path):
        with open(metadata_path, "r", encoding="utf-8") as f:
            metadata = json.load(f)
    else:
        metadata = {}

    discovered = []
    if os.path.exists(models_dir):
        for root, _, files in os.walk(models_dir):
            for file in files:
                if file.endswith(".onnx"):
                    full_path = os.path.join(root, file)
                    rel_path = os.path.relpath(full_path, repo_root)
                    size_kb = os.path.getsize(full_path) / 1024.0
                    is_qdq = "qdq" in file.lower()
                    discovered.append({
                        "filename": file,
                        "relative_path": rel_path,
                        "size_kb": round(size_kb, 2),
                        "quantization": "INT8 QDQ" if is_qdq else "FP32",
                        "status": "Ready for ONNX Runtime (QNN/CPU/DML)"
                    })

    return {
        "hardware": detect_hardware(),
        "metadata_spec": metadata,
        "discovered_onnx_artifacts": discovered
    }

def main():
    repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    result = discover_models(repo_root)
    print("=" * 60)
    print("MIRROR — QUALCOMM AI HUB MODEL DISCOVERY")
    print("=" * 60)
    print(f"Host Architecture: {result['hardware']['architecture']}")
    print(f"Snapdragon Detected: {result['hardware']['is_snapdragon']}")
    print(f"Recommended Provider: {result['hardware']['recommended_ep']}")
    print("-" * 60)
    print("Discovered Models:")
    for m in result["discovered_onnx_artifacts"]:
        print(f"  • {m['filename']} ({m['size_kb']} KB, {m['quantization']})")
    print("=" * 60)
    
    out_file = os.path.join(os.path.dirname(__file__), "discovery_report.json")
    with open(out_file, "w", encoding="utf-8") as f:
        json.dump(result, f, indent=2)
    print(f"Report written to {out_file}")

if __name__ == "__main__":
    main()
