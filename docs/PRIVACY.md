# Mirror Privacy Architecture

Mirror is built around a single uncompromised promise: **Your behavioral data stays on your computer.**

---

## 1. The 5 Architectural Privacy Commandments

Unlike conventional digital wellbeing and screen-time trackers, Mirror is designed with hardware-enforced and code-enforced boundaries preventing surveillance:

| Privacy Commandment | Implementation Guarantee |
| :--- | :--- |
| **1. Zero Window & Tab Titles** | Win32 hooks record **only** the process ID and executable name (e.g., `code.exe`). `GetWindowText` and accessibility inspection are strictly forbidden. Mirror never knows which file, tab, or chat you are reading. |
| **2. Zero Keystrokes or Text Input** | Mirror installs no keyboard hooks (`WH_KEYBOARD` / `WH_KEYBOARD_LL`). Zero keystroke counters, zero keyloggers, zero typed text buffers. |
| **3. Zero URLs or Web Payloads** | Browser activity is logged exclusively as the browser application itself (e.g., `msedge.exe`). Mirror does not inspect HTTP traffic, browser histories, bookmarks, or web addresses. |
| **4. Zero Screenshots or Screen Capture** | Mirror contains no GDI screen-scraping (`BitBlt`, `CreateCompatibleBitmap`), no DirectX screen duplication, and no webcam/microphone drivers. |
| **5. Zero Remote Telemetry or Cloud Sync** | Mirror operates 100% offline. The codebase contains zero HTTP clients, zero WebSockets, zero telemetry endpoints, and zero cloud identity dependencies. |

---

## 2. Automated Privacy Validation

Mirror enforces its privacy guarantees through multiple layers of automated testing and static analysis:

### 2.1 Reflection-Based Property Scanning (`PrivacyGuard.cs`)
Before any domain entity or session record is passed to the persistence layer, `PrivacyGuard.ValidateSafeEntity<T>` inspects all public properties of the object using reflection. If any property matches forbidden names—such as `WindowTitle`, `Url`, `Keystroke`, `Clipboard`, or `Screenshot`—execution immediately terminates with a `PrivacyViolationException`.

### 2.2 Assembly-Level Network Auditing (`NetworkPolicy.cs`)
The test suite inspects all loaded production assemblies to ensure zero referenced fields or dependencies of type:
- `System.Net.Http.HttpClient`
- `System.Net.WebClient`
- `System.Net.Sockets.TcpClient`
- `System.Net.Sockets.Socket`
- `System.Net.WebSockets.ClientWebSocket`

### 2.3 Automated Script Verification (`scripts/privacy-audit.ps1`)
The CI/CD build scripts include `scripts/privacy-audit.ps1`, which parses all `.cs` files and SQLite migrations to verify that forbidden Win32 surveillance APIs (`GetWindowText`, `GetAsyncKeyState`, `BitBlt`) and sensitive storage columns are completely absent.

---

## 3. Data Storage & Lifecycle Control

- **Local Storage Location**: `%LOCALAPPDATA%\Mirror\mirror.db`
- **Zero Cloud Replicas**: Data exists solely on the local drive.
- **Configurable Retention**: Users can select 30-day, 60-day, or 90-day retention policies. Data exceeding the retention window is pruned automatically during startup.
- **Instant Data Purge**: A single button in the Privacy Center (`Purge All Data`) issues an atomic SQLite delete command and clears all session, switch, pattern, and metric tables.
- **Transparent Human-Readable Exports**: Users can export their entire local dataset at any time in standard CSV or formatted JSON.
