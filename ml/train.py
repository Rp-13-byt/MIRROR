"""
Training Pipeline for Mirror 1D-CNN Temporal Behavioral Model
Trains a compact sequence model optimized for NPU / edge inference.
Input: [batch, 60, 10]
Output: 5 behavioral pattern classes
"""

import json
import os
import random
import numpy as np
import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader, TensorDataset

class MirrorPatternCNN(nn.Module):
    def __init__(self, in_features=10, num_classes=5):
        super().__init__()
        self.conv_block = nn.Sequential(
            nn.Conv1d(in_channels=in_features, out_channels=32, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.Conv1d(in_channels=32, out_channels=32, kernel_size=3, padding=1),
            nn.ReLU(),
            nn.AdaptiveAvgPool1d(1)
        )
        self.fc_block = nn.Sequential(
            nn.Linear(32, 16),
            nn.ReLU(),
            nn.Linear(16, num_classes)
        )

    def forward(self, x):
        # x shape: [batch, timesteps, features] -> permute to [batch, features, timesteps]
        x = x.transpose(1, 2)
        x = self.conv_block(x)
        x = x.squeeze(-1)  # [batch, 32]
        logits = self.fc_block(x)
        return logits

def set_seed(seed=42):
    random.seed(seed)
    np.random.seed(seed)
    torch.manual_seed(seed)
    if torch.cuda.is_available():
        torch.cuda.manual_seed_all(seed)

def train_model(dataset_path="ml/dataset.npz", epochs=25, batch_size=32, lr=0.001, seed=42):
    set_seed(seed)
    data = np.load(dataset_path)
    X = torch.from_numpy(data["X"])
    y = torch.from_numpy(data["y"])
    class_names = [str(c) for c in data["class_names"]]

    n_samples = len(X)
    n_train = int(0.70 * n_samples)
    n_val = int(0.15 * n_samples)
    n_test = n_samples - n_train - n_val

    indices = np.arange(n_samples)
    train_idx = indices[:n_train]
    val_idx = indices[n_train:n_train + n_val]
    test_idx = indices[n_train + n_val:]

    train_loader = DataLoader(TensorDataset(X[train_idx], y[train_idx]), batch_size=batch_size, shuffle=True)
    val_loader = DataLoader(TensorDataset(X[val_idx], y[val_idx]), batch_size=batch_size, shuffle=False)
    test_loader = DataLoader(TensorDataset(X[test_idx], y[test_idx]), batch_size=batch_size, shuffle=False)

    device = torch.device("cpu")
    model = MirrorPatternCNN(in_features=10, num_classes=len(class_names)).to(device)
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(model.parameters(), lr=lr, weight_decay=1e-4)

    best_val_loss = float("inf")
    best_weights = None

    for epoch in range(1, epochs + 1):
        model.train()
        train_loss = 0.0
        for batch_x, batch_y in train_loader:
            batch_x, batch_y = batch_x.to(device), batch_y.to(device)
            optimizer.zero_grad()
            outputs = model(batch_x)
            loss = criterion(outputs, batch_y)
            loss.backward()
            optimizer.step()
            train_loss += loss.item() * len(batch_x)

        train_loss /= len(train_idx)

        model.eval()
        val_loss = 0.0
        val_correct = 0
        with torch.no_grad():
            for batch_x, batch_y in val_loader:
                batch_x, batch_y = batch_x.to(device), batch_y.to(device)
                outputs = model(batch_x)
                loss = criterion(outputs, batch_y)
                val_loss += loss.item() * len(batch_x)
                preds = outputs.argmax(dim=-1)
                val_correct += (preds == batch_y).sum().item()

        val_loss /= len(val_idx)
        val_acc = val_correct / len(val_idx)

        if val_loss < best_val_loss:
            best_val_loss = val_loss
            best_weights = model.state_dict().copy()

    # Load best model
    if best_weights:
        model.load_state_dict(best_weights)

    # Evaluate on Test Set
    model.eval()
    all_preds = []
    all_targets = []
    with torch.no_grad():
        for batch_x, batch_y in test_loader:
            outputs = model(batch_x.to(device))
            preds = outputs.argmax(dim=-1).cpu().numpy()
            all_preds.extend(preds)
            all_targets.extend(batch_y.numpy())

    all_preds = np.array(all_preds)
    all_targets = np.array(all_targets)

    # Confusion matrix
    num_classes = len(class_names)
    conf_matrix = np.zeros((num_classes, num_classes), dtype=int)
    for p, t in zip(all_preds, all_targets):
        conf_matrix[t, p] += 1

    per_class_metrics = {}
    f1_scores = []
    for c in range(num_classes):
        tp = conf_matrix[c, c]
        fp = conf_matrix[:, c].sum() - tp
        fn = conf_matrix[c, :].sum() - tp
        prec = tp / (tp + fp) if (tp + fp) > 0 else 0.0
        rec = tp / (tp + fn) if (tp + fn) > 0 else 0.0
        f1 = (2 * prec * rec) / (prec + rec) if (prec + rec) > 0 else 0.0
        f1_scores.append(f1)
        per_class_metrics[class_names[c]] = {
            "precision": round(float(prec), 4),
            "recall": round(float(rec), 4),
            "f1": round(float(f1), 4),
            "support": int(conf_matrix[c, :].sum())
        }

    macro_f1 = float(np.mean(f1_scores))
    accuracy = float((all_preds == all_targets).mean())

    metrics = {
        "model_name": "mirror_pattern_cnn",
        "accuracy": round(accuracy, 4),
        "macro_f1": round(macro_f1, 4),
        "class_metrics": per_class_metrics,
        "confusion_matrix": conf_matrix.tolist(),
        "classes": class_names
    }

    os.makedirs("ml/models", exist_ok=True)
    torch.save(model.state_dict(), "ml/models/mirror_pattern_model.pt")

    with open("ml/metrics.json", "w") as f:
        json.dump(metrics, f, indent=2)

    with open("ml/training_report.json", "w") as f:
        json.dump({
            "training_summary": "Trained on synthetic behavioral sequences across 5 archetypes",
            "epochs": epochs,
            "best_val_loss": round(best_val_loss, 4),
            "test_accuracy": round(accuracy, 4),
            "macro_f1": round(macro_f1, 4),
            "metrics": metrics
        }, f, indent=2)

    # Save confusion matrix plot if matplotlib available
    try:
        import matplotlib.pyplot as plt
        fig, ax = plt.subplots(figsize=(6, 5))
        cax = ax.matshow(conf_matrix, cmap=plt.cm.Blues, alpha=0.8)
        for i in range(num_classes):
            for j in range(num_classes):
                ax.text(j, i, str(conf_matrix[i, j]), va="center", ha="center")
        fig.colorbar(cax)
        ax.set_xticks(range(num_classes))
        ax.set_yticks(range(num_classes))
        ax.set_xticklabels([c.replace("_", "\n") for c in class_names], fontsize=8)
        ax.set_yticklabels(class_names, fontsize=8)
        plt.xlabel("Predicted")
        plt.ylabel("Ground Truth")
        plt.title("Confusion Matrix — Behavioral Sequence Model")
        plt.tight_layout()
        plt.savefig("ml/confusion_matrix.png", dpi=150)
        plt.close()
    except Exception as e:
        print(f"Could not generate confusion matrix plot: {e}")

    print(f"Training completed successfully! Test Accuracy: {accuracy:.4f}, Macro F1: {macro_f1:.4f}")
    return model

if __name__ == "__main__":
    train_model()
