# Mirror Developer & Contributing Guide

This guide covers everything required to build, test, and develop Mirror on Windows 11.

---

## 1. Prerequisites

- **Operating System**: Windows 11 (Version 22H2 or later recommended, x64 or ARM64).
- **.NET SDK**: .NET 8.0 SDK (installed globally or portable in `D:\dotnet\`).
- **Python**: Python 3.10+ with `torch`, `onnx`, and `onnxruntime` (only required if modifying the ML model).
- **Windows App SDK**: Version 1.5+ (restored via NuGet).

---

## 2. Directory Structure

```
D:\MIRROR\
├── src\
│   ├── Mirror.Core\          # Domain entities, enums, and interfaces
│   ├── Mirror.Security\      # PrivacyGuard and NetworkPolicy validators
│   ├── Mirror.Persistence\   # SQLite WAL connection factory, migrations, repo
│   ├── Mirror.Tracking\      # Win32 foreground hook, idle detector, sessions
│   ├── Mirror.Analytics\     # 60x10 feature extractor, rules, fusion engine
│   ├── Mirror.Inference\     # ONNX Runtime backends (QNN, DirectML, CPU)
│   ├── Mirror.Platform\      # Windows auto-start, toast notifications, tray
│   └── Mirror.App\           # WinUI 3 Fluent Desktop application & ViewModels
├── ml\
│   ├── generate_dataset.py   # Synthetic training dataset generator
│   ├── train.py              # PyTorch 1D-CNN training script
│   ├── export_onnx.py        # ONNX export script
│   ├── quantize_qdq.py       # Static QDQ INT8 quantization script
│   └── benchmark.py          # Latency and memory benchmark script
├── tests\
│   ├── Mirror.Core.Tests\
│   ├── Mirror.Tracking.Tests\
│   ├── Mirror.Persistence.Tests\
│   ├── Mirror.Analytics.Tests\
│   ├── Mirror.Inference.Tests\
│   └── Mirror.Privacy.Tests\
├── scripts\
│   ├── setup-dev.ps1         # Environment sanity check
│   ├── build.ps1             # Solution build script
│   ├── test.ps1              # Full test runner script
│   ├── privacy-audit.ps1     # Static privacy scanner
│   ├── benchmark.ps1         # ONNX latency benchmark runner
│   ├── package-arm64.ps1     # ARM64 distribution packager
│   └── package-x64.ps1       # x64 distribution packager
└── docs\                     # Full architectural documentation suite
```

---

## 3. Standard Development Workflows

### Setup Environment
```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-dev.ps1
```

### Build Solution
```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1 -Configuration Debug
```

### Run All 56 Unit & Integration Tests
```powershell
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
```

### Run Static Privacy & Offline Audit
```powershell
powershell -ExecutionPolicy Bypass -File scripts/privacy-audit.ps1
```

### Run Hardware Benchmarks
```powershell
powershell -ExecutionPolicy Bypass -File scripts/benchmark.ps1 -Iterations 100
```

### Package Application for Deployment
```powershell
# For Windows on ARM64 (Qualcomm Snapdragon X):
powershell -ExecutionPolicy Bypass -File scripts/package-arm64.ps1

# For Windows x64:
powershell -ExecutionPolicy Bypass -File scripts/package-x64.ps1
```
