# Mirror System Boundaries & Limitations

By choosing an uncompromised privacy-first design, Mirror intentionally accepts specific technical trade-offs. This document explicitly outlines those boundaries.

---

## 1. Deliberate Privacy Blind Spots

### 1.1 Web Browser Internal Tabs
- **Observation**: Mirror observes `msedge.exe`, `chrome.exe`, or `firefox.exe`.
- **Limitation**: Mirror cannot distinguish whether the user is viewing educational documentation on GitHub, reading news, or streaming videos.
- **Rationale**: Inspecting specific tabs requires either browser extensions, accessibility tree traversal (UI Automation), or URL monitoring—all of which violate Mirror's zero-PII privacy commitment.

### 1.2 Multi-Monitor Passive Reading
- **Observation**: Win32 foreground window hooks track only the single window currently holding active user input focus.
- **Limitation**: If a user is reading a reference document on Monitor 2 while typing into Visual Studio on Monitor 1, Mirror only attributes active time to Visual Studio.
- **Rationale**: Tracking gaze or passive window visibility would require screen capture or camera eye-tracking, both strictly forbidden.

### 1.3 Background Audio / Video Playback
- **Observation**: Media playback applications (e.g., Spotify, YouTube in a background tab) running without keyboard/mouse interaction past 2 minutes will trigger the idle detector.
- **Limitation**: Passive listening while away from the computer is classified as idle time rather than active media consumption.

---

## 2. Virtualized & Remote Environments

### 2.1 Remote Desktop / Citrix / WSL2 GUI
- When connected to a Remote Desktop (RDP) session, Mirror sees only `mstsc.exe`. The internal applications inside the remote virtual machine are invisible to Mirror.

### 2.2 Full-Screen Video & Presentations
- Video players that suppress the Windows screen saver without generating input events may be flagged as idle if mouse movement ceases for longer than the idle threshold (default: 120 seconds).
