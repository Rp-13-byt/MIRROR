# Mirror — Security & Threat Model Review

This document provides a formal threat model and security review for the Mirror desktop client following the STRIDE methodology.

---

## 1. STRIDE Threat Analysis

| Threat Category | Potential Threat | Mitigation in Mirror | Verification |
| :--- | :--- | :--- | :--- |
| **Spoofing** | Attacker impersonates the local application to inject fake activity events. | SQLite database is protected under user profile ACLs; Windows AppContainer isolation when packaged; IPC is local in-process only. | Unit test verifying DB path permissions. |
| **Tampering** | Malware modifies database records or baseline thresholds. | SQLite WAL mode with optional DPAPI database encryption key derivation. SHA-256 integrity checks in `EventReplayEngine`. | Cryptographic hash audit script. |
| **Repudiation** | Unauthorized activity deletion without audit trail. | `PrivacyAuditService` logs all purge/deletion events to an immutable local text log. | `PrivacyAuditServiceTests`. |
| **Information Disclosure** | Sensitive URLs, passwords, or document names leak to logs, AI models, or network. | `PrivacyEnforcementGate` and `ForbiddenFieldGuard` strip all URLs, window titles, and keystrokes at capture time. Zero network sockets. | `privacy-audit.ps1` and static analysis tests. |
| **Denial of Service** | Endless loop or memory exhaustion during continuous inference. | CancellationToken on all async repository/inference methods; fixed 60-second sliding inference window; hardware timeouts. | Load testing in `benchmark_model.py`. |
| **Elevation of Privilege** | App attempts to gain Administrator privileges or bypass UAC. | Runs strictly as Standard User. Zero elevated Windows capabilities requested in `Package.appxmanifest`. | Manifest audit script. |

---

## 2. Zero-Network Air-Gap Assurance

Mirror has **zero network dependencies**:
- No `HttpClient` instances communicating with external endpoints.
- No third-party crash reporting SDKs (Sentry, AppCenter, Firebase are strictly absent).
- No analytics or telemetry telemetry beacons.
- Verified by automated dependency audit (`scripts/dependency-audit.ps1`).

---

## 3. Supply Chain Security

- All NuGet dependencies are pinned to specific versions in `.csproj` files.
- SPDX 2.3 Software Bill of Materials (SBOM) generated via `scripts/generate-sbom.ps1`.
- Model binaries (`.onnx`) are validated via SHA-256 signatures in `models/metadata/mirror_pattern_v1.json`.
