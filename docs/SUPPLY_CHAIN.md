# Mirror — Software Supply Chain & Security Specification

**Version:** 1.0.0  
**Target Platform:** Windows 11 (ARM64 Snapdragon X / x64)  
**Security Architecture:** Zero-Cloud, 100% Local Execution, Air-Gapped Behavioral Telemetry  

---

## 1. Supply Chain Philosophy

Mirror is designed as an uncompromised, privacy-first desktop application. Every dependency is evaluated against three core invariants:
1. **Zero Outbound Telemetry:** No package may initiate HTTP, WebSocket, or socket-based network connections.
2. **Local Machine Learning Sovereignty:** All model inference executes strictly on local hardware (Qualcomm Snapdragon Hexagon NPU via QNN, DirectML GPU, or CPU).
3. **Transparent Build & Provenance:** Every dependency is pinned, verifiable via SHA256 checksums, and traceable in the generated Software Bill of Materials (SBOM).

---

## 2. Dependency Provenance Matrix

| Package | Version | License | Source | Justification |
| :--- | :--- | :--- | :--- | :--- |
| `Microsoft.WindowsAppSDK` | 1.6.241114003 | MIT | nuget.org | Core Windows App SDK runtime & WinUI 3 controls |
| `Microsoft.Windows.SDK.BuildTools` | 10.0.26100.1742 | Proprietary MS | nuget.org | Windows 11 Build Tools & metadata generation |
| `Microsoft.Data.Sqlite` | 8.0.8 | MIT | nuget.org | Embedded local SQLite database engine (WAL mode) |
| `Microsoft.ML.OnnxRuntime.DirectML`| 1.19.2 | MIT | nuget.org | On-device ONNX runtime execution provider |
| `Microsoft.ML.OnnxRuntime.QNN` | 1.19.2 | MIT | nuget.org | Snapdragon X Elite/Plus Hexagon NPU execution provider |
| `CommunityToolkit.Mvvm` | 8.3.2 | MIT | nuget.org | Compile-time source-generated MVVM pattern |
| `QuestPDF` | 2024.3.0 | Community | nuget.org | 100% local PDF document layout & report rendering |
| `xunit` / `xunit.runner.visualstudio`| 2.8.2 | Apache-2.0 | nuget.org | Automated test harness |
| `FluentAssertions` | 6.12.1 | Apache-2.0 | nuget.org | Test assertion library |

---

## 3. Forbidden Dependencies & Automated Enforcement

To prevent accidental inclusion of network or tracking libraries, the following packages and namespaces are strictly banned:

* `System.Net.Http` (unless explicitly gated in tests)
* `System.Net.Sockets`
* `Microsoft.AspNetCore.SignalR`
* `Microsoft.ApplicationInsights`
* `Google.Analytics`
* `Segment.Analytics`
* `Firebase`

### Enforcement Gates:
1. **Static Analysis (`scripts/dependency-audit.ps1`):** Scans all `.csproj` files and build outputs. Fails CI/CD if any banned package is discovered.
2. **Runtime Assembly Scanner (`PrivacyAuditService.cs`):** At application startup, scans all loaded `Mirror.*` assemblies. Throws `InvalidOperationException` if any banned assembly reference or forbidden field (`WindowTitle`, `Url`, `Keystroke`, `Screenshot`, `Clipboard`) is exposed.

---

## 4. Software Bill of Materials (SBOM)

An automated SPDX 2.3 compliant Software Bill of Materials can be generated at any time using:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\generate-sbom.ps1
```

This outputs `artifacts/sbom.spdx.json`, which details:
- Project metadata and creator information
- Package names, exact package versions, licenses, and suppliers
- SHA-256 package hashes for tamper resistance

---

## 5. Build Reproducibility & Integrity

1. **Deterministic Builds:** The solution builds with `.NET 8 SDK (8.0.406)` with deterministic output enabled (`<Deterministic>true</Deterministic>`).
2. **Code Signing:** Windows release packages (`MSIX`) must be signed with a hardware-backed code signing certificate or local development test certificate (`certs/MirrorDevCert.pfx`).
3. **Database Integrity:** The SQLite database utilizes Write-Ahead Logging (`WAL`), `PRAGMA synchronous = NORMAL`, and automated startup verification (`PRAGMA integrity_check`).
