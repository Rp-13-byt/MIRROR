"""
Model Inspector for Mirror
Inspects ONNX graph inputs, outputs, nodes, and metadata.
"""

import json
import os
import onnx

def inspect_model(model_path="ml/models/mirror_pattern_v1.onnx"):
    if not os.path.exists(model_path):
        print(f"Model file not found: {model_path}")
        return

    model = onnx.load(model_path)
    print("=" * 60)
    print(f"INSPECTING ONNX MODEL: {model_path}")
    print("=" * 60)
    print(f"IR Version: {model.ir_version}")
    print(f"Producer: {model.producer_name} {model.producer_version}")
    print(f"Opset imports: {[(op.domain, op.version) for op in model.opset_import]}")

    print("\n--- GRAPH INPUTS ---")
    for inp in model.graph.input:
        shape = [dim.dim_value for dim in inp.type.tensor_type.shape.dim]
        print(f"Name: {inp.name}, Type: {inp.type.tensor_type.elem_type}, Shape: {shape}")

    print("\n--- GRAPH OUTPUTS ---")
    for out in model.graph.output:
        shape = [dim.dim_value for dim in out.type.tensor_type.shape.dim]
        print(f"Name: {out.name}, Type: {out.type.tensor_type.elem_type}, Shape: {shape}")

    print(f"\nTotal Nodes: {len(model.graph.node)}")
    op_counts = {}
    for node in model.graph.node:
        op_counts[node.op_type] = op_counts.get(node.op_type, 0) + 1

    print("Operator breakdown:")
    for op, count in sorted(op_counts.items()):
        print(f"  {op}: {count}")

    file_size_kb = os.path.getsize(model_path) / 1024
    print(f"\nModel File Size: {file_size_kb:.2f} KB")
    print("=" * 60)

if __name__ == "__main__":
    inspect_model("ml/models/mirror_pattern_v1.onnx")
    if os.path.exists("ml/models/mirror_pattern_v1.qdq.onnx"):
        inspect_model("ml/models/mirror_pattern_v1.qdq.onnx")
