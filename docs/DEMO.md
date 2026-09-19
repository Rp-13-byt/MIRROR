# Mirror 7-Day Realistic Demo Mode

Mirror includes a built-in, fully isolated **7-Day Demo Mode** designed for instant evaluation, product demonstrations, and visual inspection without requiring days of background data collection.

---

## 1. Complete Dataset Isolation

```
                  +----------------------------------------------+
                  |                 WinUI 3 View                 |
                  +----------------------------------------------+
                                         |
                                         v
                         +--------------------------------+
                         | IsDemoModeActive Check (Toggle)|
                         +--------------------------------+
                                /                  \
                      [True]   /                    \  [False]
                              v                      v
        +----------------------------+     +----------------------------+
        | DemoDataService (In-Memory)|     | MirrorRepository (SQLite)  |
        |  - 7 Days of Synthetic Data|     |  - Actual User Workstation |
        |  - Zero Disk Footprint     |     |  - Local File: mirror.db   |
        +----------------------------+     +----------------------------+
```

### Safety Guarantees:
- **No Database Mutation**: Enabling Demo Mode does **not** insert or overwrite records in the user's real `mirror.db`.
- **In-Memory Generation**: All synthetic data is maintained strictly in memory via `DemoDataService.cs`.
- **Instant Toggle**: Toggling Demo Mode off immediately restores the user's actual local activity data.

---

## 2. Visual Indicators

When Demo Mode is activated:
1. **Header Toggle**: The top-right Demo toggle switch highlights in an active orange accent.
2. **Global Warning Banner**: An amber warning banner appears across the top of the interface:
   > *"DEMO MODE ACTIVE — Viewing simulated 7-day realistic behavioral data. Real data tracking is preserved."*
3. **Hardware Acceleration Status**: The hardware acceleration badge dynamically simulates active Qualcomm Snapdragon NPU inference for realistic demonstration.

---

## 3. Simulated Realistic Archetypes

The 7-day synthetic dataset models rich, diverse behavioral dynamics across a typical week:
- **Monday–Wednesday (Productive Focus & Context Switching)**:
  - Long software development blocks in Visual Studio Code (2.5 – 3.5 hrs).
  - High switching bursts between Slack, Microsoft Teams, and Microsoft Edge during mid-day standups.
- **Thursday–Friday (Collaborative & Evening Gaming)**:
  - Document editing in Microsoft Word and Notion.
  - Evening gaming sessions in Steam and simulation games.
- **Weekend (Browsing & Late-Night Spike)**:
  - Media streaming and browsing in Edge and Spotify.
  - Simulated late-night activity spike (23:30 – 02:00) triggering the late-night usage pattern.
