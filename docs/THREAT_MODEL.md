# Mirror Security & Threat Model

Mirror approaches user behavioral tracking through an adversarial security mindset. The system assumes that any sensitive data collected could potentially be exposed in the event of local workstation compromise. Therefore, the core security control is **data minimization at the point of capture**.

---

## 1. Threat Boundaries

```
+-------------------------------------------------------------------------------+
|                            Workstation Boundary                               |
|                                                                               |
|   +--------------------------+           +--------------------------------+   |
|   | Untrusted Network / Web  | <---X---> | Mirror Process                 |   |
|   | (Cloud APIs, Telemetry)  |           | (Zero sockets, air-gapped)     |   |
|   +--------------------------+           +--------------------------------+   |
|                                                          |                    |
|                                                          v                    |
|                                          +--------------------------------+   |
|                                          | Local SQLite Database          |   |
|                                          | (%LOCALAPPDATA%\Mirror\*.db)   |   |
|                                          +--------------------------------+   |
+-------------------------------------------------------------------------------+
```

---

## 2. Threat Analysis & Mitigations

### 2.1 Threat: Remote Network Exfiltration
- **Risk**: An attacker or third-party SDK sends user application habits to remote servers.
- **Mitigation**: Mirror contains **zero** networking dependencies. The application links against neither `System.Net.Http` nor third-party telemetry packages. Even if the process were injected with hostile instructions, the executable contains no HTTP transport stack.

### 2.2 Threat: Credential / Password Interception
- **Risk**: Screen tracking or keylogging tools capture passwords or private encryption keys typed into password managers or terminals.
- **Mitigation**: Mirror **never** installs keyboard hooks (`WH_KEYBOARD`, `WH_KEYBOARD_LL`). It uses solely `GetLastInputInfo` (which yields only the timestamp of the last global input event, never the key code or character).

### 2.3 Threat: Private Document & Tab Title Exposure
- **Risk**: Window titles reveal sensitive internal documents (e.g. `Q3_Layoff_Plan.docx`) or sensitive browser URLs/tabs (e.g. `medical-records.com`).
- **Mitigation**: Mirror explicitly ignores window titles. The P/Invoke layer resolves the process handle solely to the executable file name (`WINWORD.EXE`, `msedge.exe`). Full paths and titles are discarded immediately in memory.

### 2.4 Threat: Forensic Inspection of Local SQLite File
- **Risk**: A secondary user or forensic tool inspects `%LOCALAPPDATA%\Mirror\mirror.db`.
- **Mitigation**:
  1. The schema contains only executable names and duration integers. No URLs, text, or file names exist in the database.
  2. One-click purge immediately executes an atomic SQL wipe and checkpoint.
  3. Mirror runs entirely in standard user space (no elevated administrator rights required).
