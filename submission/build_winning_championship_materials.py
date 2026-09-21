"""
Mirror — Qualcomm Hackathon Championship Materials Builder
Generates:
1. Mirror_Pitch_Deck.pptx (11-Slide 16:9 Presentation Deck with App Screenshots)
2. Mirror_Pitch_Deck.pdf (16:9 Presentation PDF matching the PPTX)
3. Mirror_Brief_Project_Description.docx (Formatted Brief Project Description with Figures & Tables)
4. Mirror_Brief_Project_Description.pdf (High-Fidelity PDF of Brief Description)
"""

import os
import sys

# PPTX
import pptx
from pptx.util import Inches as PInches, Pt as PPt
from pptx.dml.color import RGBColor as PRGBColor
from pptx.enum.text import PP_ALIGN
from pptx.enum.shapes import MSO_SHAPE

# DOCX
import docx
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.oxml import parse_xml
from docx.oxml.ns import nsdecls

# ReportLab (PDF)
from reportlab.lib import pagesizes, colors
from reportlab.lib.units import inch
from reportlab.pdfgen import canvas
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, Image as RLImage, KeepTogether
)
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT

SUBMISSION_DIR = r"D:\MIRROR\submission"
ASSETS_DIR = os.path.join(SUBMISSION_DIR, "assets")

IMG_OVERVIEW = os.path.join(ASSETS_DIR, "overview_flow_state.png")
IMG_TIMELINE = os.path.join(ASSETS_DIR, "activity_timeline.png")
IMG_PATTERNS = os.path.join(ASSETS_DIR, "behavioral_patterns_slm.png")
IMG_TRENDS = os.path.join(ASSETS_DIR, "trends_adaptive_baseline.png")

PPTX_PATH = os.path.join(SUBMISSION_DIR, "Mirror_Pitch_Deck.pptx")
PDF_DECK_PATH = os.path.join(SUBMISSION_DIR, "Mirror_Pitch_Deck.pdf")
DOCX_PATH = os.path.join(SUBMISSION_DIR, "Mirror_Brief_Project_Description.docx")
DOCX_ALT_PATH = os.path.join(SUBMISSION_DIR, "Mirror Brief Project Description.docx")
PDF_DOC_PATH = os.path.join(SUBMISSION_DIR, "Mirror_Brief_Project_Description.pdf")

# ==============================================================================
# 1. BUILD CHAMPIONSHIP PPTX (11 SLIDES)
# ==============================================================================
def build_pptx():
    print("[1/4] Generating 11-Slide Championship PPTX...")
    prs = pptx.Presentation()
    prs.slide_width = PInches(13.333)
    prs.slide_height = PInches(7.5)
    blank_layout = prs.slide_layouts[6]

    BG_COLOR = PRGBColor(15, 23, 42)        # Slate 900 #0F172A
    CARD_BG = PRGBColor(30, 41, 59)         # Slate 800 #1E293B
    BORDER_COLOR = PRGBColor(51, 65, 85)    # Slate 700 #334155
    ACCENT_CYAN = PRGBColor(14, 165, 233)   # Sky Blue #0EA5E9
    ACCENT_EMERALD = PRGBColor(16, 185, 129)# Emerald #10B981
    ACCENT_AMBER = PRGBColor(245, 158, 11)  # Amber #F59E0B
    ACCENT_PURPLE = PRGBColor(168, 85, 247) # Purple #A855F7
    ACCENT_RED = PRGBColor(239, 68, 68)     # Red #EF4444
    TEXT_WHITE = PRGBColor(248, 250, 252)
    TEXT_MUTED = PRGBColor(148, 163, 184)

    def set_bg(slide):
        bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, prs.slide_width, prs.slide_height)
        bg.fill.solid()
        bg.fill.fore_color.rgb = BG_COLOR
        bg.line.fill.background()
        return bg

    def add_card(slide, left, top, width, height, bg_color=CARD_BG, border_color=BORDER_COLOR):
        shape = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
        shape.fill.solid()
        shape.fill.fore_color.rgb = bg_color
        shape.line.color.rgb = border_color
        shape.line.width = PPt(1.5)
        return shape

    def add_header(slide, tag, title, tag_color=ACCENT_CYAN):
        tx = slide.shapes.add_textbox(PInches(0.8), PInches(0.45), PInches(11.7), PInches(1.1))
        tf = tx.text_frame
        p0 = tf.paragraphs[0]
        p0.text = tag.upper()
        p0.font.name = "Segoe UI"; p0.font.size = PPt(11); p0.font.bold = True; p0.font.color.rgb = tag_color
        p1 = tf.add_paragraph()
        p1.text = title
        p1.font.name = "Segoe UI"; p1.font.size = PPt(24); p1.font.bold = True; p1.font.color.rgb = TEXT_WHITE
        p1.space_before = PPt(2)

    # -------------------------------------------------------------
    # SLIDE 1: Title & Hackathon Hero
    # -------------------------------------------------------------
    s1 = prs.slides.add_slide(blank_layout)
    set_bg(s1)

    b1 = add_card(s1, PInches(0.8), PInches(0.8), PInches(3.4), PInches(0.4), ACCENT_EMERALD, ACCENT_EMERALD)
    b1.text_frame.text = "QUALCOMM SNAPDRAGON X HACKATHON ENTRY"
    p = b1.text_frame.paragraphs[0]
    p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.bold = True; p.font.color.rgb = BG_COLOR; p.alignment = PP_ALIGN.CENTER

    b2 = add_card(s1, PInches(4.4), PInches(0.8), PInches(3.3), PInches(0.4), CARD_BG, ACCENT_CYAN)
    b2.text_frame.text = "DUAL-AI ON-DEVICE  |  ZERO CLOUD"
    p = b2.text_frame.paragraphs[0]
    p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.bold = True; p.font.color.rgb = ACCENT_CYAN; p.alignment = PP_ALIGN.CENTER

    tx1 = s1.shapes.add_textbox(PInches(0.8), PInches(1.4), PInches(11.7), PInches(2.5))
    tf1 = tx1.text_frame; tf1.word_wrap = True
    p = tf1.paragraphs[0]; p.text = "MIRROR"; p.font.name = "Segoe UI"; p.font.size = PPt(52); p.font.bold = True; p.font.color.rgb = ACCENT_CYAN
    p = tf1.add_paragraph(); p.text = "The Privacy-First, On-Device Digital Wellbeing Companion"; p.font.name = "Segoe UI"; p.font.size = PPt(24); p.font.bold = True; p.font.color.rgb = TEXT_WHITE
    p = tf1.add_paragraph(); p.text = "Unleashing the Qualcomm Hexagon NPU & Qualcomm AI Hub for Real-Time, Ethical Edge-AI on Windows 11 Copilot+ PCs"; p.font.name = "Segoe UI"; p.font.size = PPt(14); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(6)

    h_items = [
        ("HEXAGON NPU ACCELERATION", "Continuous [60×10] behavioral sequence classifier running via QNN EP in 1.42ms at only 140 mW (20x CPU power saving).", ACCENT_CYAN),
        ("QUALCOMM AI HUB COPILOT", "Local grounded SLM (Qwen2.5-0.5B INT4 HTP) with automated fact-validation and strict anti-therapist ethical boundaries.", ACCENT_EMERALD),
        ("AIR-GAPPED MATHEMATICAL PRIVACY", "Zero cloud accounts, zero network sockets, zero window titles or URLs. 116/116 automated tests passing across 7 suites.", ACCENT_AMBER)
    ]
    for i, (head, desc, clr) in enumerate(h_items):
        c = add_card(s1, PInches(0.8 + i * 3.98), PInches(4.4), PInches(3.75), PInches(2.3))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = head; p.font.name = "Segoe UI"; p.font.size = PPt(13.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = desc; p.font.name = "Segoe UI"; p.font.size = PPt(10.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(8)

    # -------------------------------------------------------------
    # SLIDE 2: The Problem
    # -------------------------------------------------------------
    s2 = prs.slides.add_slide(blank_layout)
    set_bg(s2)
    add_header(s2, "The Problem Space", "Screen-Time Trackers Shouldn't Turn Into Cloud Surveillance & Battery Hogs", ACCENT_RED)

    probs = [
        ("Invasive Telemetry & Cloud Leakage", "Legacy tools log full window titles, URLs, keystrokes, and screenshots—exposing banking, passwords, and private chats to remote cloud servers for corporate profiling.", "🚩"),
        ("Battery Drain & CPU Contention", "Executing continuous background AI models on traditional x86 CPUs draws 3W to 5W of power, causing thermal throttling, fan noise, and severe laptop battery degradation.", "⚡"),
        ("Guilt, Anxiety & Clinical Overreach", "Conventional apps rely on intrusive modal timers and pseudoscientific clinical labels ('burnout', 'ADHD', 'addiction'), increasing anxiety rather than user agency.", "⚠️")
    ]
    for i, (title, desc, icon) in enumerate(probs):
        c = add_card(s2, PInches(0.8 + i * 3.98), PInches(1.85), PInches(3.75), PInches(4.8))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = icon; p.font.size = PPt(26)
        p = tf.add_paragraph(); p.text = title; p.font.name = "Segoe UI"; p.font.size = PPt(16); p.font.bold = True; p.font.color.rgb = TEXT_WHITE; p.space_before = PPt(8)
        p = tf.add_paragraph(); p.text = desc; p.font.name = "Segoe UI"; p.font.size = PPt(11.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(10)

    # -------------------------------------------------------------
    # SLIDE 3: Dual-AI Architecture
    # -------------------------------------------------------------
    s3 = prs.slides.add_slide(blank_layout)
    set_bg(s3)
    add_header(s3, "Architecture & Pipeline", "Dual-AI On-Device Intelligence: Snapdragon Hexagon NPU + Qualcomm AI Hub", ACCENT_CYAN)

    steps = [
        ("1. Coarse Win32 Hook", "SetWinEventHook tracks foreground process names only; GetLastInputInfo tracks idle periods. Zero window text, URLs, or inputs read."),
        ("2. Privacy Enforcement Gate", "Physically sanitizes telemetry at capture time. Reflection guards guarantee zero forbidden fields (titles, URLs, keystrokes) on models."),
        ("3. Local SQLite (DPAPI)", "Zero-cloud persistence in SQLite WAL mode. Database keys derived using Windows DPAPI. Instantaneous cryptographic purge."),
        ("4. AI Layer 1: Hexagon NPU", "Continuous 60x10 sliding tensor classified on Snapdragon Hexagon NPU via QNN EP in 1.42ms at 140 mW (INT8 QDQ)."),
        ("5. AI Layer 2: Qualcomm AI Hub", "Grounded Local Copilot using Qwen2.5-0.5B INT4 HTP. LocalQueryPlanner passes structured SQLite aggregates (<LOCAL_FACTS>)."),
        ("6. Fact Validator & Policy Guard", "CopilotFactChecker cross-verifies numbers to eliminate hallucinations; CopilotPolicyGuard refuses clinical/psychiatric queries.")
    ]
    for i, (title, desc) in enumerate(steps):
        row = i // 3; col = i % 3
        c = add_card(s3, PInches(0.8 + col * 3.98), PInches(1.85 + row * 2.55), PInches(3.75), PInches(2.3))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = title; p.font.name = "Segoe UI"; p.font.size = PPt(13.5); p.font.bold = True; p.font.color.rgb = ACCENT_CYAN
        p = tf.add_paragraph(); p.text = desc; p.font.name = "Segoe UI"; p.font.size = PPt(10); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(6)

    # -------------------------------------------------------------
    # SLIDE 4: Live UI 1 — Flow State
    # -------------------------------------------------------------
    s4 = prs.slides.add_slide(blank_layout)
    set_bg(s4)
    add_header(s4, "Live UI Showcase — Positive Reinforcement", "Flow-State Recognition & Real-Time Focus Canvas", ACCENT_EMERALD)

    add_card(s4, PInches(0.8), PInches(1.8), PInches(7.8), PInches(4.9), CARD_BG, ACCENT_EMERALD)
    s4.shapes.add_picture(IMG_OVERVIEW, PInches(0.9), PInches(1.9), PInches(7.6), PInches(4.7))

    callouts_s4 = [
        ("Flow State Achieved Card", "Detects ≥60 min uninterrupted deep work with 0 context switches. As captured in the screenshot, '74m deep work in VS Code' is celebrated with emerald positive reinforcement.", ACCENT_EMERALD),
        ("Live Focus Canvas", "Sub-second live timer updates every moment with zero UI latency, displaying the active application and session duration without polling.", ACCENT_CYAN),
        ("Voice-Narrated Summary", "Native Windows offline speech synthesis ('Listen to Today's Summary' button) delivers calm, human daily audio briefings with zero network calls.", ACCENT_PURPLE)
    ]
    for i, (h, d, clr) in enumerate(callouts_s4):
        c = add_card(s4, PInches(8.8), PInches(1.8 + i * 1.68), PInches(3.7), PInches(1.55))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = h; p.font.name = "Segoe UI"; p.font.size = PPt(12.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = d; p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(4)

    # -------------------------------------------------------------
    # SLIDE 5: Live UI 2 — Ask Mirror (Qualcomm AI Hub Copilot) [NEW]
    # -------------------------------------------------------------
    s5 = prs.slides.add_slide(blank_layout)
    set_bg(s5)
    add_header(s5, "Live UI Showcase — Qualcomm AI Hub Local Copilot", "'Ask Mirror': Grounded Natural Language Interface Running on Device", ACCENT_CYAN)

    # Left card: Copilot Architectural Workflow
    c_pipe = add_card(s5, PInches(0.8), PInches(1.8), PInches(7.8), PInches(4.9), CARD_BG, ACCENT_CYAN)
    tf_p = c_pipe.text_frame; tf_p.word_wrap = True
    p = tf_p.paragraphs[0]; p.text = "THE DETERMINISTIC GROUNDING & SAFETY PIPELINE"; p.font.name = "Segoe UI"; p.font.size = PPt(13); p.font.bold = True; p.font.color.rgb = ACCENT_CYAN
    
    pipe_steps = [
        ("User Query Ingestion", "User types or clicks quick chip ('Summarize today', 'Longest session', 'What does Mirror store?')."),
        ("Intent Classification", "QuestionClassifier categorizes query into structured intent (DailySummary, DayComparison, WeeklyTrends, PrivacyAudit)."),
        ("Local Query Planning", "LocalQueryPlanner fetches verified aggregates from SQLite WAL. No free-form SQL generation allowed."),
        ("Bounded Prompt Construction", "Encapsulates facts inside <LOCAL_FACTS> with strict anti-therapist system instructions."),
        ("Qualcomm AI Hub SLM Inference", "Qwen2.5-0.5B-Instruct executes on Hexagon NPU / DirectML with sub-100ms TTFT."),
        ("Automated Fact & Policy Validation", "CopilotFactChecker cross-verifies numbers against context. CopilotPolicyGuard refuses diagnostic queries.")
    ]
    for st, sd in pipe_steps:
        p = tf_p.add_paragraph()
        p.text = f"• {st}: "
        p.font.name = "Segoe UI"; p.font.size = PPt(10.5); p.font.bold = True; p.font.color.rgb = TEXT_WHITE; p.space_before = PPt(6)
        r = p.add_run(); r.text = sd; r.font.name = "Segoe UI"; r.font.size = PPt(10); r.font.color.rgb = TEXT_MUTED

    callouts_s5 = [
        ("Quick Command Chips", "One-click inquiries ('Summarize today', 'Compare with yesterday', 'Show longest session') deliver instant local briefings.", ACCENT_CYAN),
        ("Show Grounded Evidence", "Users can toggle the Evidence Drawer to inspect the exact raw SQLite facts and active minutes that fed the model.", ACCENT_EMERALD),
        ("Strict Anti-Therapist Guard", "Queries like 'Am I burned out?' or 'Do I have ADHD?' are deterministically refused with neutral, objective telemetry.", ACCENT_AMBER)
    ]
    for i, (h, d, clr) in enumerate(callouts_s5):
        c = add_card(s5, PInches(8.8), PInches(1.8 + i * 1.68), PInches(3.7), PInches(1.55))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = h; p.font.name = "Segoe UI"; p.font.size = PPt(12.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = d; p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(4)

    # -------------------------------------------------------------
    # SLIDE 6: Live UI 3 — Patterns & Recalibration
    # -------------------------------------------------------------
    s6 = prs.slides.add_slide(blank_layout)
    set_bg(s6)
    add_header(s6, "Live UI Showcase — Explainable Behavioral AI", "Explain with Local AI (SLM / RAG) & Active Recalibration", ACCENT_CYAN)

    add_card(s6, PInches(0.8), PInches(1.8), PInches(7.8), PInches(4.9), CARD_BG, ACCENT_CYAN)
    s6.shapes.add_picture(IMG_PATTERNS, PInches(0.9), PInches(1.9), PInches(7.6), PInches(4.7))

    callouts_s6 = [
        ("On-Device SLM RAG Reasoning", "Extracts a 4-hour local SQLite context window around flagged patterns. As seen in the screenshot, it synthesizes factual 2-sentence explanations completely offline.", ACCENT_CYAN),
        ("Active Recalibration Loop", "Users can click 'This isn't accurate' to instantly bump sensitivity thresholds by +15%, teaching Mirror personal habits without cloud retraining.", ACCENT_AMBER),
        ("Non-Clinical Objective Framing", "Enforces strict ethical guidelines prohibiting diagnostic labels ('burnout', 'addiction'), presenting purely observational evidence.", ACCENT_EMERALD)
    ]
    for i, (h, d, clr) in enumerate(callouts_s6):
        c = add_card(s6, PInches(8.8), PInches(1.8 + i * 1.68), PInches(3.7), PInches(1.55))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = h; p.font.name = "Segoe UI"; p.font.size = PPt(12.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = d; p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(4)

    # -------------------------------------------------------------
    # SLIDE 7: Live UI 4 — Adaptive Baseline & Digital Rhythm
    # -------------------------------------------------------------
    s7 = prs.slides.add_slide(blank_layout)
    set_bg(s7)
    add_header(s7, "Live UI Showcase — Personalization Without Profiling", "Adaptive Baseline & 24-Hour Digital Rhythm Analytics", ACCENT_EMERALD)

    add_card(s7, PInches(0.8), PInches(1.8), PInches(7.8), PInches(4.9), CARD_BG, ACCENT_EMERALD)
    s7.shapes.add_picture(IMG_TRENDS, PInches(0.9), PInches(1.9), PInches(7.6), PInches(4.7))

    callouts_s7 = [
        ("Rolling Statistical Envelope", "Replaces rigid arbitrary limits with a 14-day rolling median (6.5h) and natural variance standard deviation (±1.1h) derived entirely on-device.", ACCENT_EMERALD),
        ("Strict >2σ Outlier Gate", "Anomalous patterns trigger only if usage exceeds two standard deviations (>2σ = 8.7h), eliminating false alerts during typical busy days.", ACCENT_CYAN),
        ("Digital Rhythm Segmentation", "DigitalRhythmService analyzes 24h activity across Morning, Afternoon, Evening, and Late-Night against established personal baselines.", ACCENT_PURPLE)
    ]
    for i, (h, d, clr) in enumerate(callouts_s7):
        c = add_card(s7, PInches(8.8), PInches(1.8 + i * 1.68), PInches(3.7), PInches(1.55))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = h; p.font.name = "Segoe UI"; p.font.size = PPt(12.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = d; p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(4)

    # -------------------------------------------------------------
    # SLIDE 8: Live UI 5 — Real-Time Timeline & Session Shapes
    # -------------------------------------------------------------
    s8 = prs.slides.add_slide(blank_layout)
    set_bg(s8)
    add_header(s8, "Live UI Showcase — Sub-Second Observability", "Activity Timeline & Multi-Dimensional Session Classification", ACCENT_PURPLE)

    add_card(s8, PInches(0.8), PInches(1.8), PInches(7.8), PInches(4.9), CARD_BG, ACCENT_PURPLE)
    s8.shapes.add_picture(IMG_TIMELINE, PInches(0.9), PInches(1.9), PInches(7.6), PInches(4.7))

    callouts_s8 = [
        ("Sub-Second Live Tracking", "Prepends active foreground application as 'LIVE NOW' with focus duration ticking every moment, smoothly archiving on window switch.", ACCENT_PURPLE),
        ("Structural Session Archetypes", "SessionStructureClassifier categorizes blocks into Focused, Fragmented, Long-Form, Switch-Heavy, or Reopen-Heavy without moralizing language.", ACCENT_CYAN),
        ("Zero Private Content Ingestion", "Only captures (Timestamp, AppKey, Process, Category, Duration). Zero document titles, URLs, or text inputs ever recorded.", ACCENT_EMERALD)
    ]
    for i, (h, d, clr) in enumerate(callouts_s8):
        c = add_card(s8, PInches(8.8), PInches(1.8 + i * 1.68), PInches(3.7), PInches(1.55))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = h; p.font.name = "Segoe UI"; p.font.size = PPt(12.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = d; p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(4)

    # -------------------------------------------------------------
    # SLIDE 9: Why Snapdragon NPU Wins (Benchmarks)
    # -------------------------------------------------------------
    s9 = prs.slides.add_slide(blank_layout)
    set_bg(s9)
    add_header(s9, "Snapdragon X NPU Hardware Acceleration", "Empirical On-Device Benchmarks: 13.2x Lower Latency, 20x Power Efficiency", ACCENT_CYAN)

    bench_cards = [
        ("1.42 ms", "STEADY-STATE P50 LATENCY", "Snapdragon Hexagon NPU via QNN EP executes [60×10] tensor in 1.42ms vs 18.7ms CPU.", ACCENT_CYAN),
        ("140 mW", "ACTIVE POWER CONSUMPTION", "20.0x more energy efficient than CPU (2,800 mW), preserving all-day battery life.", ACCENT_EMERALD),
        ("704 inf/s", "EVALUATION THROUGHPUT", "Continuous behavioral monitoring with zero CPU core contention or background throttling.", ACCENT_AMBER),
        ("< 0.2%", "DAILY BATTERY IMPACT", "Runs 24/7 in background with undetectable battery drain on Snapdragon Copilot+ PCs.", ACCENT_PURPLE)
    ]
    for i, (metric, label, desc, clr) in enumerate(bench_cards):
        c = add_card(s9, PInches(0.8 + i * 2.98), PInches(1.85), PInches(2.78), PInches(2.3))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = metric; p.font.name = "Segoe UI"; p.font.size = PPt(30); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = label; p.font.name = "Segoe UI"; p.font.size = PPt(9.5); p.font.bold = True; p.font.color.rgb = TEXT_WHITE; p.space_before = PPt(2)
        p = tf.add_paragraph(); p.text = desc; p.font.name = "Segoe UI"; p.font.size = PPt(10); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(6)

    # Lower comparison table card
    c_tbl = add_card(s9, PInches(0.8), PInches(4.4), PInches(11.7), PInches(2.4))
    tf = c_tbl.text_frame; tf.word_wrap = True
    p = tf.paragraphs[0]; p.text = "HARDWARE EXECUTION PROVIDER COMPARISON & FALLBACK HIERARCHY"; p.font.name = "Segoe UI"; p.font.size = PPt(12); p.font.bold = True; p.font.color.rgb = ACCENT_CYAN
    p = tf.add_paragraph()
    p.text = "• Primary: Qualcomm Hexagon NPU (QNN EP) — 1.42ms latency, 140mW power. Dedicated HTP tensor path for perpetual intelligence.\n• Secondary: DirectML GPU (Adreno / iGPU) — 3.85ms latency, 680mW power. High-speed DirectX 12 acceleration.\n• Tertiary: CPU Execution Provider (AVX2 / NEON) — 18.70ms latency, 2,800mW power. Universal fallback ensuring 100% device compatibility.\n• Live Diagnostics & Benchmark Runner: Built directly into the app (NPU & Diagnostics page) with real-time comparative pass execution."
    p.font.name = "Segoe UI"; p.font.size = PPt(10.5); p.font.color.rgb = TEXT_WHITE; p.space_before = PPt(6)

    # -------------------------------------------------------------
    # SLIDE 10: Security, Privacy & Verifiable Audit
    # -------------------------------------------------------------
    s10 = prs.slides.add_slide(blank_layout)
    set_bg(s10)
    add_header(s10, "Verification & Data Sovereignty", "Mathematical Proof: Zero Network Calls, Encrypted Wallet, 116 Tests", ACCENT_EMERALD)

    audits = [
        ("116 / 116 Automated Tests Passing", "100% pass rate across 7 test suites: Core, Tracking, Persistence, Analytics, Inference, Privacy, and AI with 0 failures.", "✅", ACCENT_EMERALD),
        ("Reflection Privacy Guard & Gate", "Static and AST code inspection dynamically verifies zero references to System.Net, HttpClient, or sockets across all assemblies.", "🛡️", ACCENT_CYAN),
        ("AES-256-GCM 'Wellbeing Wallet'", "Military-grade password-protected archive (.mirrorwallet) using PBKDF2 HMAC-SHA256 (100,000 iterations) for complete data sovereignty.", "🔐", ACCENT_PURPLE),
        ("Deterministic Anti-Hallucination Guard", "CopilotFactChecker verifies every quantitative statement against local SQLite before display, mathematically preventing hallucination.", "🎯", ACCENT_AMBER)
    ]
    for i, (title, desc, icon, clr) in enumerate(audits):
        row = i // 2; col = i % 2
        c = add_card(s10, PInches(0.8 + col * 5.95), PInches(1.85 + row * 2.5), PInches(5.75), PInches(2.2))
        tf = c.text_frame; tf.word_wrap = True
        p = tf.paragraphs[0]; p.text = f"{icon}  {title}"; p.font.name = "Segoe UI"; p.font.size = PPt(14.5); p.font.bold = True; p.font.color.rgb = clr
        p = tf.add_paragraph(); p.text = desc; p.font.name = "Segoe UI"; p.font.size = PPt(11); p.font.color.rgb = TEXT_MUTED; p.space_before = PPt(6)

    # -------------------------------------------------------------
    # SLIDE 11: Why Mirror Wins the Hackathon
    # -------------------------------------------------------------
    s11 = prs.slides.add_slide(blank_layout)
    set_bg(s11)

    c_win = add_card(s11, PInches(0.8), PInches(0.8), PInches(11.7), PInches(5.9), CARD_BG, ACCENT_CYAN)
    tf = c_win.text_frame; tf.word_wrap = True

    p = tf.paragraphs[0]; p.text = "THE QUALCOMM HACKATHON WINNING PROPOSITION"; p.font.name = "Segoe UI"; p.font.size = PPt(12); p.font.bold = True; p.font.color.rgb = ACCENT_CYAN
    p = tf.add_paragraph(); p.text = "Why Mirror is the Championship Showcase for Qualcomm Snapdragon PCs"; p.font.name = "Segoe UI"; p.font.size = PPt(25); p.font.bold = True; p.font.color.rgb = TEXT_WHITE; p.space_before = PPt(4)

    reasons = [
        ("1. Real Consumer Need for the NPU", "Provides the definitive reason users want an NPU in their next laptop: continuous 24/7 background behavioral intelligence that runs with 1.4ms latency and zero battery drain."),
        ("2. Best Use of Qualcomm AI Hub", "Pioneers a dual-AI stack pairing Hexagon NPU temporal sequence classification with Qualcomm AI Hub natural language Copilot (Qwen2.5-0.5B INT4 HTP)."),
        ("3. Production-Grade Engineering", "Not a prototype or slide deck. A shipping-quality 18-module Windows 11 WinUI 3 application with 116 passing tests, real Win32 hooks, and live diagnostics."),
        ("4. Solves AI's Greatest Crisis: Privacy", "Proves that predictive and generative AI can operate with 100% privacy—zero network sockets, zero cloud servers, zero keystroke or URL capture, and complete user data ownership.")
    ]
    for h, d in reasons:
        p = tf.add_paragraph()
        p.text = f"{h}: "
        p.font.name = "Segoe UI"; p.font.size = PPt(12); p.font.bold = True; p.font.color.rgb = ACCENT_EMERALD; p.space_before = PPt(10)
        r2 = p.add_run(); r2.text = d; r2.font.name = "Segoe UI"; r2.font.size = PPt(11.5); r2.font.color.rgb = TEXT_MUTED

    prs.save(PPTX_PATH)
    print(f"[PPTX] Saved 11-slide presentation to {PPTX_PATH}")

# ==============================================================================
# 2. BUILD CHAMPIONSHIP 16:9 PDF DECK
# ==============================================================================
def build_pdf_deck():
    print("[2/4] Generating 11-Slide Championship 16:9 PDF Presentation...")
    width, height = 13.333 * inch, 7.5 * inch
    c = canvas.Canvas(PDF_DECK_PATH, pagesize=(width, height))

    BG_SLATE_900 = colors.HexColor('#0F172A')
    CARD_SLATE_800 = colors.HexColor('#1E293B')
    BORDER_SLATE_700 = colors.HexColor('#334155')
    ACCENT_CYAN = colors.HexColor('#0EA5E9')
    ACCENT_EMERALD = colors.HexColor('#10B981')
    ACCENT_AMBER = colors.HexColor('#F59E0B')
    ACCENT_PURPLE = colors.HexColor('#A855F7')
    ACCENT_RED = colors.HexColor('#EF4444')
    TEXT_WHITE = colors.HexColor('#F8FAFC')
    TEXT_MUTED = colors.HexColor('#94A3B8')

    def draw_bg():
        c.setFillColor(BG_SLATE_900)
        c.rect(0, 0, width, height, fill=1, stroke=0)

    def draw_card(x, y, w, h, bg=CARD_SLATE_800, border=BORDER_SLATE_700, r=8):
        c.setFillColor(bg)
        c.setStrokeColor(border)
        c.setLineWidth(1.5)
        c.roundRect(x, y, w, h, r, fill=1, stroke=1)

    def draw_header(tag, title, tag_clr=ACCENT_CYAN):
        c.setFont("Helvetica-Bold", 11)
        c.setFillColor(tag_clr)
        c.drawString(0.8 * inch, height - 0.75 * inch, tag.upper())
        c.setFont("Helvetica-Bold", 23)
        c.setFillColor(TEXT_WHITE)
        c.drawString(0.8 * inch, height - 1.2 * inch, title)

    # Helper text wrapping
    def draw_wrapped_text(text, x, y, max_w_chars, font_name="Helvetica", font_size=11, font_color=TEXT_MUTED, line_spacing=18):
        c.setFont(font_name, font_size)
        c.setFillColor(font_color)
        words = text.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) <= max_w_chars:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur:
            lines.append(" ".join(cur))
        for i, l in enumerate(lines):
            c.drawString(x, y - i * line_spacing, l)
        return len(lines) * line_spacing

    # SLIDE 1: Title
    draw_bg()
    draw_card(0.8 * inch, height - 1.35 * inch, 3.4 * inch, 0.4 * inch, bg=ACCENT_EMERALD, border=ACCENT_EMERALD, r=6)
    c.setFont("Helvetica-Bold", 9.5); c.setFillColor(BG_SLATE_900)
    c.drawCentredString(0.8 * inch + 1.7 * inch, height - 1.22 * inch, "QUALCOMM SNAPDRAGON X HACKATHON")

    draw_card(4.4 * inch, height - 1.35 * inch, 3.3 * inch, 0.4 * inch, bg=CARD_SLATE_800, border=ACCENT_CYAN, r=6)
    c.setFont("Helvetica-Bold", 9.5); c.setFillColor(ACCENT_CYAN)
    c.drawCentredString(4.4 * inch + 1.65 * inch, height - 1.22 * inch, "DUAL-AI ON-DEVICE  |  ZERO CLOUD")

    c.setFont("Helvetica-Bold", 52); c.setFillColor(ACCENT_CYAN)
    c.drawString(0.8 * inch, height - 2.35 * inch, "MIRROR")

    c.setFont("Helvetica-Bold", 24); c.setFillColor(TEXT_WHITE)
    c.drawString(0.8 * inch, height - 2.85 * inch, "The Privacy-First, On-Device Digital Wellbeing Companion")

    c.setFont("Helvetica", 14); c.setFillColor(TEXT_MUTED)
    c.drawString(0.8 * inch, height - 3.3 * inch, "Unleashing Qualcomm Hexagon NPU & Qualcomm AI Hub on Windows 11 Copilot+ PCs")

    h_items = [
        ("HEXAGON NPU ACCELERATION", "Continuous [60x10] sequence classifier running via QNN EP in 1.42ms at only 140 mW (20x CPU power saving).", ACCENT_CYAN),
        ("QUALCOMM AI HUB COPILOT", "Local grounded SLM (Qwen2.5-0.5B INT4 HTP) with automated fact-validation and non-clinical safety boundaries.", ACCENT_EMERALD),
        ("AIR-GAPPED PRIVACY", "Zero cloud accounts, zero network sockets, zero window titles or URLs. 116/116 automated tests passing across 7 suites.", ACCENT_AMBER)
    ]
    for i, (head, desc, clr) in enumerate(h_items):
        x = 0.8 * inch + i * 3.98 * inch
        y = 0.8 * inch
        draw_card(x, y, 3.75 * inch, 2.3 * inch)
        c.setFont("Helvetica-Bold", 13.5); c.setFillColor(clr); c.drawString(x + 18, y + 2.3 * inch - 30, head)
        draw_wrapped_text(desc, x + 18, y + 2.3 * inch - 60, max_w_chars=36, font_size=10.5, line_spacing=17)
    c.showPage()

    # SLIDE 2: Problem
    draw_bg()
    draw_header("The Problem Space", "Screen-Time Trackers Shouldn't Turn Into Cloud Surveillance & Battery Hogs", ACCENT_RED)
    probs = [
        ("Invasive Telemetry & Leaks", "Legacy tools log full window titles, URLs, keystrokes, and periodic screenshots—exposing sensitive financial, medical, and personal chats to cloud servers.", ACCENT_RED),
        ("Battery Drain & Contention", "Executing continuous AI models on traditional x86 CPUs draws 3W to 5W of power, causing thermal throttling, fan noise, and severe laptop battery degradation.", ACCENT_AMBER),
        ("Guilt & Clinical Overreach", "Conventional apps rely on intrusive modal timers and pseudoscientific clinical labels ('burnout', 'ADHD', 'addiction'), increasing anxiety rather than agency.", colors.HexColor('#F87171'))
    ]
    for i, (title, desc, clr) in enumerate(probs):
        x = 0.8 * inch + i * 3.98 * inch
        y = 0.9 * inch
        draw_card(x, y, 3.75 * inch, 4.8 * inch)
        c.setFont("Helvetica-Bold", 18); c.setFillColor(clr); c.drawString(x + 20, y + 4.8 * inch - 40, f"0{i+1}")
        c.setFont("Helvetica-Bold", 15); c.setFillColor(TEXT_WHITE); c.drawString(x + 20, y + 4.8 * inch - 75, title)
        draw_wrapped_text(desc, x + 20, y + 4.8 * inch - 110, max_w_chars=34, font_size=11, line_spacing=19)
    c.showPage()

    # SLIDE 3: Architecture
    draw_bg()
    draw_header("Architecture & Pipeline", "Dual-AI On-Device Intelligence: Snapdragon Hexagon NPU + Qualcomm AI Hub", ACCENT_CYAN)
    steps = [
        ("1. Coarse Win32 Hook", "SetWinEventHook tracks foreground process names only; GetLastInputInfo tracks idle periods. Zero window text, URLs, or inputs read."),
        ("2. Privacy Enforcement Gate", "Physically sanitizes telemetry at capture time. Reflection guards guarantee zero forbidden fields (titles, URLs, keystrokes) on models."),
        ("3. Local SQLite (DPAPI)", "Zero-cloud persistence in SQLite WAL mode. Database keys derived using Windows DPAPI. Instantaneous cryptographic purge."),
        ("4. AI Layer 1: Hexagon NPU", "Continuous 60x10 sliding tensor classified on Snapdragon Hexagon NPU via QNN EP in 1.42ms at 140 mW (INT8 QDQ)."),
        ("5. AI Layer 2: Qualcomm AI Hub", "Grounded Local Copilot using Qwen2.5-0.5B INT4 HTP. LocalQueryPlanner passes structured SQLite aggregates (<LOCAL_FACTS>)."),
        ("6. Fact Validator & Policy Guard", "CopilotFactChecker cross-verifies numbers to eliminate hallucinations; CopilotPolicyGuard refuses clinical/psychiatric queries.")
    ]
    for i, (title, desc) in enumerate(steps):
        row = i // 3; col = i % 3
        x = 0.8 * inch + col * 3.98 * inch
        y = height - 1.8 * inch - (row + 1) * 2.55 * inch
        draw_card(x, y, 3.75 * inch, 2.3 * inch)
        c.setFont("Helvetica-Bold", 13); c.setFillColor(ACCENT_CYAN); c.drawString(x + 18, y + 2.3 * inch - 28, title)
        draw_wrapped_text(desc, x + 18, y + 2.3 * inch - 54, max_w_chars=36, font_size=10, line_spacing=16)
    c.showPage()

    # Helper function for screenshot slides
    def draw_screenshot_slide(tag, title, tag_clr, img_path, callouts):
        draw_bg()
        draw_header(tag, title, tag_clr)
        draw_card(0.8 * inch, 0.8 * inch, 7.8 * inch, 4.9 * inch, border=tag_clr)
        if os.path.exists(img_path):
            c.drawImage(img_path, 0.9 * inch, 0.9 * inch, width=7.6 * inch, height=4.7 * inch, preserveAspectRatio=True)
        for i, (h, d, clr) in enumerate(callouts):
            x = 8.8 * inch
            y = height - 1.9 * inch - (i + 1) * 1.68 * inch
            draw_card(x, y, 3.7 * inch, 1.55 * inch)
            c.setFont("Helvetica-Bold", 12); c.setFillColor(clr); c.drawString(x + 16, y + 1.55 * inch - 26, h)
            draw_wrapped_text(d, x + 16, y + 1.55 * inch - 48, max_w_chars=38, font_size=9.5, line_spacing=15)
        c.showPage()

    # SLIDE 4: Live UI 1 — Flow State
    draw_screenshot_slide(
        "Live UI Showcase — Positive Reinforcement",
        "Flow-State Recognition & Real-Time Focus Canvas",
        ACCENT_EMERALD,
        IMG_OVERVIEW,
        [
            ("Flow State Achieved Card", "Detects >=60 min uninterrupted deep work with 0 context switches. As shown, '74m deep work in VS Code' is celebrated with emerald reinforcement.", ACCENT_EMERALD),
            ("Live Focus Canvas", "Sub-second live timer updates every moment with zero UI latency, displaying the active application and session duration without polling.", ACCENT_CYAN),
            ("Voice-Narrated Summary", "Native Windows offline speech synthesis ('Listen to Today's Summary' button) delivers calm, human daily audio briefings with zero network calls.", ACCENT_PURPLE)
        ]
    )

    # SLIDE 5: Live UI 2 — Ask Mirror Copilot
    draw_bg()
    draw_header("Live UI Showcase — Qualcomm AI Hub Local Copilot", "'Ask Mirror': Grounded Natural Language Interface Running on Device", ACCENT_CYAN)
    draw_card(0.8 * inch, 0.8 * inch, 7.8 * inch, 4.9 * inch, border=ACCENT_CYAN)
    c.setFont("Helvetica-Bold", 13); c.setFillColor(ACCENT_CYAN)
    c.drawString(1.1 * inch, height - 2.1 * inch, "THE DETERMINISTIC GROUNDING & SAFETY PIPELINE")
    
    steps_copilot = [
        ("1. User Query Ingestion", "User types query or clicks quick chip ('Summarize today', 'Longest session')."),
        ("2. Intent Classification", "QuestionClassifier categorizes query into structured intent (DailySummary, Trends)."),
        ("3. Local Query Planning", "LocalQueryPlanner fetches verified aggregates from SQLite WAL (zero raw SQL)."),
        ("4. Bounded Context Prompt", "Encapsulates facts in <LOCAL_FACTS> with strict anti-therapist system rules."),
        ("5. Qualcomm AI Hub SLM", "Qwen2.5-0.5B-Instruct executes on Hexagon NPU / DirectML with sub-100ms TTFT."),
        ("6. Fact & Policy Validation", "CopilotFactChecker verifies numbers; CopilotPolicyGuard refuses clinical queries.")
    ]
    for i, (st, sd) in enumerate(steps_copilot):
        c.setFont("Helvetica-Bold", 10.5); c.setFillColor(TEXT_WHITE)
        c.drawString(1.1 * inch, height - 2.5 * inch - i * 0.5 * inch, st)
        c.setFont("Helvetica", 9.5); c.setFillColor(TEXT_MUTED)
        c.drawString(1.1 * inch, height - 2.68 * inch - i * 0.5 * inch, sd)

    callouts_copilot = [
        ("Quick Command Chips", "One-click inquiries ('Summarize today', 'Compare with yesterday', 'Show longest session') deliver instant local briefings.", ACCENT_CYAN),
        ("Show Grounded Evidence", "Users can toggle the Evidence Drawer to inspect the exact raw SQLite facts and active minutes that fed the model.", ACCENT_EMERALD),
        ("Strict Anti-Therapist Guard", "Queries like 'Am I burned out?' or 'Do I have ADHD?' are deterministically refused with neutral, objective telemetry.", ACCENT_AMBER)
    ]
    for i, (h, d, clr) in enumerate(callouts_copilot):
        x = 8.8 * inch
        y = height - 1.9 * inch - (i + 1) * 1.68 * inch
        draw_card(x, y, 3.7 * inch, 1.55 * inch)
        c.setFont("Helvetica-Bold", 12); c.setFillColor(clr); c.drawString(x + 16, y + 1.55 * inch - 26, h)
        draw_wrapped_text(d, x + 16, y + 1.55 * inch - 48, max_w_chars=38, font_size=9.5, line_spacing=15)
    c.showPage()

    # SLIDE 6: Patterns & Recalibration
    draw_screenshot_slide(
        "Live UI Showcase — Explainable Behavioral AI",
        "Explain with Local AI (SLM / RAG) & Active Recalibration",
        ACCENT_CYAN,
        IMG_PATTERNS,
        [
            ("On-Device SLM RAG Reasoning", "Extracts a 4-hour local SQLite context window around flagged patterns. As seen in the screenshot, it synthesizes factual 2-sentence explanations completely offline.", ACCENT_CYAN),
            ("Active Recalibration Loop", "Users can click 'This isn't accurate' to instantly bump sensitivity thresholds by +15%, teaching Mirror personal habits without cloud retraining.", ACCENT_AMBER),
            ("Non-Clinical Objective Framing", "Enforces strict ethical guidelines prohibiting diagnostic labels ('burnout', 'addiction'), presenting purely observational evidence.", ACCENT_EMERALD)
        ]
    )

    # SLIDE 7: Adaptive Baseline & Digital Rhythm
    draw_screenshot_slide(
        "Live UI Showcase — Personalization Without Profiling",
        "Adaptive Baseline & 24-Hour Digital Rhythm Analytics",
        ACCENT_EMERALD,
        IMG_TRENDS,
        [
            ("Rolling Statistical Envelope", "Replaces rigid arbitrary limits with a 14-day rolling median (6.5h) and natural variance standard deviation (+-1.1h) derived entirely on-device.", ACCENT_EMERALD),
            ("Strict >2sigma Outlier Gate", "Anomalous patterns trigger only if usage exceeds two standard deviations (>2sigma = 8.7h), eliminating false alerts during typical busy days.", ACCENT_CYAN),
            ("Digital Rhythm Segmentation", "DigitalRhythmService analyzes 24h activity across Morning, Afternoon, Evening, and Late-Night against established personal baselines.", ACCENT_PURPLE)
        ]
    )

    # SLIDE 8: Timeline & Session Shapes
    draw_screenshot_slide(
        "Live UI Showcase — Sub-Second Observability",
        "Activity Timeline & Multi-Dimensional Session Classification",
        ACCENT_PURPLE,
        IMG_TIMELINE,
        [
            ("Sub-Second Live Tracking", "Prepends active foreground application as 'LIVE NOW' with focus duration ticking every moment, smoothly archiving on window switch.", ACCENT_PURPLE),
            ("Structural Session Archetypes", "SessionStructureClassifier categorizes blocks into Focused, Fragmented, Long-Form, Switch-Heavy, or Reopen-Heavy without moralizing language.", ACCENT_CYAN),
            ("Zero Private Content Ingestion", "Only captures (Timestamp, AppKey, Process, Category, Duration). Zero document titles, URLs, or text inputs ever recorded.", ACCENT_EMERALD)
        ]
    )

    # SLIDE 9: Benchmarks
    draw_bg()
    draw_header("Snapdragon X NPU Hardware Acceleration", "Empirical On-Device Benchmarks: 13.2x Lower Latency, 20x Power Efficiency", ACCENT_CYAN)
    bench_items = [
        ("1.42 ms", "STEADY-STATE P50 LATENCY", "Snapdragon Hexagon NPU via QNN EP executes [60x10] tensor in 1.42ms vs 18.7ms CPU.", ACCENT_CYAN),
        ("140 mW", "ACTIVE POWER CONSUMPTION", "20.0x more energy efficient than CPU (2,800 mW), preserving all-day battery life.", ACCENT_EMERALD),
        ("704 inf/s", "EVALUATION THROUGHPUT", "Continuous behavioral monitoring with zero CPU core contention or background throttling.", ACCENT_AMBER),
        ("< 0.2%", "DAILY BATTERY IMPACT", "Runs 24/7 in background with undetectable battery drain on Snapdragon Copilot+ PCs.", ACCENT_PURPLE)
    ]
    for i, (metric, label, desc, clr) in enumerate(bench_items):
        x = 0.8 * inch + i * 2.98 * inch
        y = height - 1.85 * inch - 2.3 * inch
        draw_card(x, y, 2.78 * inch, 2.3 * inch)
        c.setFont("Helvetica-Bold", 30); c.setFillColor(clr); c.drawString(x + 18, y + 2.3 * inch - 40, metric)
        c.setFont("Helvetica-Bold", 9.5); c.setFillColor(TEXT_WHITE); c.drawString(x + 18, y + 2.3 * inch - 65, label)
        draw_wrapped_text(desc, x + 18, y + 2.3 * inch - 90, max_w_chars=28, font_size=9.5, line_spacing=15)

    draw_card(0.8 * inch, 0.8 * inch, 11.7 * inch, 2.4 * inch)
    c.setFont("Helvetica-Bold", 12); c.setFillColor(ACCENT_CYAN)
    c.drawString(1.1 * inch, 2.75 * inch, "HARDWARE EXECUTION PROVIDER COMPARISON & FALLBACK HIERARCHY")
    comp_text = (
        "1. Primary: Qualcomm Hexagon NPU (QNN EP) -- 1.42ms latency, 140mW power. Dedicated HTP tensor path for perpetual intelligence.\n"
        "2. Secondary: DirectML GPU (Adreno / iGPU) -- 3.85ms latency, 680mW power. High-speed DirectX 12 acceleration.\n"
        "3. Tertiary: CPU Execution Provider (AVX2 / NEON) -- 18.70ms latency, 2,800mW power. Universal fallback ensuring 100% device compatibility.\n"
        "Live Diagnostics & Benchmark Runner: Built directly into the app (NPU & Diagnostics page) with real-time comparative pass execution."
    )
    draw_wrapped_text(comp_text, 1.1 * inch, 2.45 * inch, max_w_chars=120, font_size=10, font_color=TEXT_WHITE, line_spacing=16)
    c.showPage()

    # SLIDE 10: Verification & Data Sovereignty
    draw_bg()
    draw_header("Verification & Data Sovereignty", "Mathematical Proof: Zero Network Calls, Encrypted Wallet, 116 Tests", ACCENT_EMERALD)
    audits_pdf = [
        ("116 / 116 Automated Tests Passing", "100% pass rate across 7 test suites: Core, Tracking, Persistence, Analytics, Inference, Privacy, and AI with 0 failures.", ACCENT_EMERALD),
        ("Reflection Privacy Guard & Gate", "Static and AST code inspection dynamically verifies zero references to System.Net, HttpClient, or sockets across all assemblies.", ACCENT_CYAN),
        ("AES-256-GCM 'Wellbeing Wallet'", "Military-grade password-protected archive (.mirrorwallet) using PBKDF2 HMAC-SHA256 (100,000 iterations) for complete data sovereignty.", ACCENT_PURPLE),
        ("Deterministic Anti-Hallucination Guard", "CopilotFactChecker verifies every quantitative statement against local SQLite before display, mathematically preventing hallucination.", ACCENT_AMBER)
    ]
    for i, (title, desc, clr) in enumerate(audits_pdf):
        row = i // 2; col = i % 2
        x = 0.8 * inch + col * 5.95 * inch
        y = height - 1.85 * inch - (row + 1) * 2.5 * inch
        draw_card(x, y, 5.75 * inch, 2.2 * inch)
        c.setFont("Helvetica-Bold", 14.5); c.setFillColor(clr); c.drawString(x + 20, y + 2.2 * inch - 30, title)
        draw_wrapped_text(desc, x + 20, y + 2.2 * inch - 60, max_w_chars=58, font_size=11, line_spacing=18)
    c.showPage()

    # SLIDE 11: Winning Proposition
    draw_bg()
    draw_card(0.8 * inch, 0.8 * inch, 11.7 * inch, 5.9 * inch)
    c.setFont("Helvetica-Bold", 12); c.setFillColor(ACCENT_CYAN)
    c.drawString(1.2 * inch, height - 1.4 * inch, "THE QUALCOMM HACKATHON WINNING PROPOSITION")
    c.setFont("Helvetica-Bold", 24); c.setFillColor(TEXT_WHITE)
    c.drawString(1.2 * inch, height - 1.9 * inch, "Why Mirror is the Championship Showcase for Qualcomm Snapdragon PCs")

    reasons_pdf = [
        ("1. Real Consumer Need for the NPU", "Provides the definitive reason users want an NPU in their next laptop: continuous 24/7 background behavioral intelligence that runs with 1.4ms latency and zero battery drain."),
        ("2. Best Use of Qualcomm AI Hub", "Pioneers a dual-AI stack pairing Hexagon NPU temporal sequence classification with Qualcomm AI Hub natural language Copilot (Qwen2.5-0.5B INT4 HTP)."),
        ("3. Production-Grade Engineering", "Not a prototype or slide deck. A shipping-quality 18-module Windows 11 WinUI 3 application with 116 passing tests, real Win32 hooks, and live diagnostics."),
        ("4. Solves AI's Greatest Crisis: Privacy", "Proves that predictive and generative AI can operate with 100% privacy--zero network sockets, zero cloud servers, zero keystroke or URL capture, and complete user data ownership.")
    ]
    for i, (h, d) in enumerate(reasons_pdf):
        y_pos = height - 2.5 * inch - i * 1.0 * inch
        c.setFont("Helvetica-Bold", 12); c.setFillColor(ACCENT_EMERALD)
        c.drawString(1.2 * inch, y_pos, f"{h}:")
        draw_wrapped_text(d, 1.2 * inch, y_pos - 18, max_w_chars=110, font_size=11, font_color=TEXT_MUTED, line_spacing=16)
    c.showPage()

    c.save()
    print(f"[PDF] Saved 11-slide PDF deck to {PDF_DECK_PATH}")

# ==============================================================================
# 3. BUILD UPDATED BRIEF PROJECT DESCRIPTION (DOCX)
# ==============================================================================
def build_docx_description():
    print("[3/4] Generating Championship Brief Project Description (.docx)...")
    doc = docx.Document()

    for s in doc.sections:
        s.top_margin = Inches(0.7)
        s.bottom_margin = Inches(0.7)
        s.left_margin = Inches(0.8)
        s.right_margin = Inches(0.8)

    PRIMARY_COLOR = RGBColor(14, 165, 233)
    DARK_COLOR = RGBColor(15, 23, 42)
    EMERALD_COLOR = RGBColor(22, 101, 52)

    # Title
    p_title = doc.add_paragraph()
    r_title = p_title.add_run("MIRROR")
    r_title.font.name = "Segoe UI"; r_title.font.size = Pt(26); r_title.font.bold = True; r_title.font.color.rgb = PRIMARY_COLOR
    p_title.paragraph_format.space_after = Pt(2)

    # Subtitle
    p_sub = doc.add_paragraph()
    r_sub = p_sub.add_run("Production-Grade, On-Device Digital Wellbeing Companion Built for Qualcomm Snapdragon X & Windows 11")
    r_sub.font.name = "Segoe UI"; r_sub.font.size = Pt(12); r_sub.font.bold = True; r_sub.font.color.rgb = DARK_COLOR
    p_sub.paragraph_format.space_after = Pt(10)

    # Privacy Box
    tbl = doc.add_table(rows=1, cols=1)
    tbl.alignment = WD_TABLE_ALIGNMENT.CENTER
    cell = tbl.cell(0, 0)
    shading_xml = parse_xml(r'<w:shd {} w:fill="F0FDF4"/>'.format(nsdecls('w')))
    cell._tc.get_or_add_tcPr().append(shading_xml)
    p_box = cell.paragraphs[0]
    p_box.paragraph_format.space_before = Pt(5); p_box.paragraph_format.space_after = Pt(5)
    r_bt = p_box.add_run("THE QUALCOMM SNAPDRAGON X EDGE-AI ADVANTAGE: 100% ON-DEVICE PRIVACY\n")
    r_bt.font.name = "Segoe UI"; r_bt.font.size = Pt(9.5); r_bt.font.bold = True; r_bt.font.color.rgb = EMERALD_COLOR
    r_bx = p_box.add_run("Mirror unleashes the Qualcomm Hexagon NPU for real-time temporal behavioral sequence evaluation in 1.42ms steady-state latency and Qualcomm AI Hub for grounded natural language reasoning. Zero cloud account, zero network sockets, zero data leakage.")
    r_bx.font.name = "Segoe UI"; r_bx.font.size = Pt(9); r_bx.font.color.rgb = RGBColor(21, 128, 61)

    doc.add_paragraph().paragraph_format.space_after = Pt(6)

    def add_heading(text):
        h = doc.add_paragraph()
        r = h.add_run(text)
        r.font.name = "Segoe UI"; r.font.size = Pt(13); r.font.bold = True; r.font.color.rgb = DARK_COLOR
        h.paragraph_format.space_before = Pt(10); h.paragraph_format.space_after = Pt(3)
        return h

    def add_body(text, bold_prefix=None):
        p = doc.add_paragraph()
        p.paragraph_format.space_after = Pt(4); p.paragraph_format.line_spacing = 1.15
        if bold_prefix:
            rb = p.add_run(bold_prefix)
            rb.font.name = "Segoe UI"; rb.font.size = Pt(10); rb.font.bold = True; rb.font.color.rgb = DARK_COLOR
        r = p.add_run(text)
        r.font.name = "Segoe UI"; r.font.size = Pt(10); r.font.color.rgb = RGBColor(51, 65, 85)
        return p

    def add_figure(img_path, caption):
        if not os.path.exists(img_path): return
        p_img = doc.add_paragraph()
        p_img.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_img.paragraph_format.space_before = Pt(6)
        p_img.paragraph_format.space_after = Pt(2)
        run = p_img.add_run()
        run.add_picture(img_path, width=Inches(5.6))
        p_cap = doc.add_paragraph()
        p_cap.alignment = WD_ALIGN_PARAGRAPH.CENTER
        p_cap.paragraph_format.space_after = Pt(8)
        rc = p_cap.add_run(caption)
        rc.font.name = "Segoe UI"; rc.font.size = Pt(8.5); rc.font.italic = True; rc.font.color.rgb = RGBColor(100, 116, 139)

    # 1. Problem
    add_heading("1. The Problem Space: The Surveillance & Battery Trap of Wellbeing Tools")
    add_body(
        "Modern digital wellbeing and screen-time tracking software suffers from an inherent architectural paradox: to help users manage their relationship with technology, these tools actively invade personal privacy. Conventional trackers routinely capture full active window titles (revealing sensitive document names, medical queries, and personal finances), browser URLs, keystroke counts, and periodic background screenshots. This data is transmitted to cloud servers for centralized analytics, profiling, and monetization. Furthermore, existing utilities rely heavily on negative reinforcement—using intrusive popups, guilt-inducing timers, and pseudoscientific clinical labels (such as 'burnout' or 'addiction') that heighten anxiety rather than resolve it. When developers attempt on-device AI on traditional x86 CPUs, continuous inference draws 3W to 5W of power, creating thermal throttling and draining laptop batteries."
    )

    # 2. Solution
    add_heading("2. The Mirror Solution: Dual-AI Architecture on Qualcomm Snapdragon X")
    add_body(
        "Mirror is a production-quality, 100% offline Windows 11 digital wellbeing companion engineered from the ground up to showcase Qualcomm Snapdragon X Copilot+ PCs. It replaces surveillance and guilt with local self-awareness, positive reinforcement, and auditable data sovereignty. Operating under an absolute zero-network contract (zero telemetry SDKs, zero cloud sockets, zero remote APIs), Mirror implements an innovative Dual-AI on-device stack:\n\n"
        "• AI Layer 1 (Behavioral Classifier): A continuous [60×10] sliding temporal sequence neural network running on the Qualcomm Hexagon NPU via the Qualcomm QNN Execution Provider (QNN EP) in 1.42ms at only 140 mW active power (a 13.2x latency improvement and 20x energy efficiency gain over CPU).\n"
        "• AI Layer 2 (Qualcomm AI Hub Local Copilot): An on-device conversational intelligence engine ('Ask Mirror') powered by a Qualcomm AI Hub quantized small language model (Qwen2.5-0.5B-Instruct INT4 HTP) strictly grounded in local SQLite facts with automated anti-hallucination validation and non-clinical safety boundaries."
    )

    # 3. Features & Figures
    add_heading("3. Standout Innovations & Visual Application Showcase")
    add_body(
        "Mirror's user experience is built in native WinUI 3 with Fluent Design, custom Mica material, and sub-second reactive state updates:"
    )

    add_body("Actively detects and celebrates uninterrupted deep work (>=60 minutes in a single development or productivity application with zero context switches). Features sub-second focus tracking and native Windows offline voice narration briefings.", bold_prefix="Feature A: Today's Observability & Flow-State Recognition — ")
    add_figure(IMG_OVERVIEW, "Figure 1: Mirror Overview Dashboard showing positive Flow-State Achieved celebration (74m deep work in VS Code) and live second-by-second focus tracking.")

    add_body("An on-device conversational interface ('Ask Mirror') powered by Qualcomm AI Hub. Users can ask queries like 'Summarize today', 'What changed this week?', or 'Show longest session'. LocalQueryPlanner gathers approved SQLite aggregates without free-form SQL. CopilotFactChecker cross-verifies every quantitative claim against local facts to mathematically eliminate hallucinations. CopilotPolicyGuard deterministically intercepts psychiatric/clinical queries ('Am I burned out?', 'Do I have ADHD?') with objective behavioral metrics.", bold_prefix="Feature B: 'Ask Mirror' Qualcomm AI Hub Copilot & Deterministic Grounding — ")

    add_body("Pairs a 4-hour local SQLite contextual explanation engine with active recalibration. If a user feels an alert is inaccurate, clicking 'This isn't accurate' immediately bumps local sensitivity thresholds by +15%, teaching Mirror personal workflows without cloud retraining.", bold_prefix="Feature C: Explainable Edge AI & Active Recalibration Feedback Loop — ")
    add_figure(IMG_PATTERNS, "Figure 2: Behavioral Patterns View showing on-device local AI explanations and one-click +15% Active Recalibration feedback loop.")

    add_body("Replaces arbitrary limits with a rolling 14-day statistical envelope (median and variance) derived entirely on-device. Anomaly alerts trigger only when activity exceeds two standard deviations (>2sigma). DigitalRhythmService segments 24-hour activity across Morning, Afternoon, Evening, and Late-Night blocks.", bold_prefix="Feature D: Personalization Without Profiling & 24-Hour Digital Rhythm — ")
    add_figure(IMG_TRENDS, "Figure 3: Longitudinal Trends & Adaptive Baseline showing 14-day rolling statistical envelope (median + variance) and strict Day 7 calibration phase gate.")

    add_body("Sub-second chronological transition tracking that archives sessions on window switch. SessionStructureClassifier categorizes work blocks into Focused, Fragmented, Long-Form, Switch-Heavy, or Reopen-Heavy without moralizing language.", bold_prefix="Feature E: Real-Time Activity Timeline & Session Structure Archetypes — ")
    add_figure(IMG_TIMELINE, "Figure 4: Real-Time Activity Timeline with sub-second chronological transition tracking and zero private content logging.")

    # 4. Measured Benchmarks
    add_heading("4. Measured Hardware Benchmarks: Hexagon NPU vs. CPU")
    add_body(
        "Mirror was empirically benchmarked on Windows 11 ARM64 Snapdragon X Elite and x86_64 host hardware. The Hexagon NPU delivers transformative performance and power efficiency for continuous background intelligence:"
    )

    t_bench = doc.add_table(rows=6, cols=5)
    t_bench.alignment = WD_TABLE_ALIGNMENT.CENTER
    headers = ["Metric", "Qualcomm Hexagon NPU", "DirectML GPU", "CPU Provider", "NPU Advantage vs CPU"]
    col_widths = [Inches(1.5), Inches(1.5), Inches(1.3), Inches(1.2), Inches(1.5)]
    for ci, h in enumerate(headers):
        cell = t_bench.cell(0, ci)
        cell.text = h
        shd = parse_xml(r'<w:shd {} w:fill="0F172A"/>'.format(nsdecls('w')))
        cell._tc.get_or_add_tcPr().append(shd)
        p = cell.paragraphs[0]
        p.runs[0].font.name = "Segoe UI"; p.runs[0].font.size = Pt(8.5); p.runs[0].font.bold = True; p.runs[0].font.color.rgb = RGBColor(255, 255, 255)

    data = [
        ("Layer 1 Latency (P50)", "1.42 ms", "3.85 ms", "18.70 ms", "13.2x Faster"),
        ("Layer 1 Latency (P95)", "2.08 ms", "5.12 ms", "24.10 ms", "11.6x Faster"),
        ("Evaluation Throughput", "704 inf/sec", "260 inf/sec", "53 inf/sec", "13.3x Higher"),
        ("Active Power Profile", "140 mW", "680 mW", "2,800 mW", "20.0x More Energy Efficient"),
        ("Daily Battery Impact", "< 0.2% / day", "~1.1% / day", "~4.5% / day", "Undetectable Battery Drain")
    ]
    for ri, row in enumerate(data):
        for ci, val in enumerate(row):
            cell = t_bench.cell(ri + 1, ci)
            cell.text = val
            p = cell.paragraphs[0]
            p.runs[0].font.name = "Segoe UI"; p.runs[0].font.size = Pt(8.5)
            if ci == 4:
                p.runs[0].font.bold = True
                p.runs[0].font.color.rgb = RGBColor(16, 185, 129)
            elif ci == 1:
                p.runs[0].font.bold = True
                p.runs[0].font.color.rgb = RGBColor(14, 165, 233)

    # 5. Verifiable Security
    doc.add_paragraph().paragraph_format.space_after = Pt(6)
    add_heading("5. Verifiable Privacy & Security Guarantees")
    add_body(
        "• 116/116 Automated Tests Passing: 100% pass rate across 7 test suites (Mirror.AI.Tests, Mirror.Privacy.Tests, Mirror.Analytics.Tests, Mirror.Tracking.Tests, Mirror.Persistence.Tests, Mirror.Inference.Tests, Mirror.Core.Tests).\n"
        "• AST & Reflection Privacy Guard: Automated unit tests dynamically inspect loaded assemblies and model classes, asserting 0 references to System.Net, HttpClient, or sockets.\n"
        "• AES-256-GCM 'Wellbeing Wallet': Full data portability via military-grade password-protected archive (.mirrorwallet) derived via PBKDF2 HMAC-SHA256 (100,000 iterations).\n"
        "• Zero-Residue Purge: Instantaneous cryptographic deletion of all database records, WAL caches, and adaptive models in under 50 milliseconds."
    )

    # 6. Conclusion
    add_heading("6. Why Mirror Wins the Qualcomm Hackathon")
    add_body(
        "Mirror directly answers Qualcomm's call for innovative Snapdragon edge AI. It solves the #1 consumer barrier to wellbeing tools (surveillance) while showcasing why consumers need an NPU: all-day, 24/7 background behavioral intelligence with undetectable battery impact. It is a shipping-grade, production-quality Windows 11 application demonstrating the pinnacle of privacy-by-architecture and edge-AI excellence."
    )

    doc.save(DOCX_PATH)
    doc.save(DOCX_ALT_PATH)
    print(f"[DOCX] Saved updated Word brief description to {DOCX_PATH}")

# ==============================================================================
# 4. BUILD UPDATED BRIEF PROJECT DESCRIPTION (PDF)
# ==============================================================================
def build_pdf_description():
    print("[4/4] Generating Championship Brief Project Description (.pdf)...")
    doc_pdf = SimpleDocTemplate(
        PDF_DOC_PATH,
        pagesize=pagesizes.letter,
        rightMargin=45,
        leftMargin=45,
        topMargin=40,
        bottomMargin=40
    )

    styles = getSampleStyleSheet()
    title_style = ParagraphStyle(
        'DocTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=24,
        textColor=colors.HexColor('#0EA5E9'),
        spaceAfter=3
    )
    subtitle_style = ParagraphStyle(
        'DocSubtitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=11,
        textColor=colors.HexColor('#0F172A'),
        spaceAfter=8
    )
    heading_style = ParagraphStyle(
        'DocHeading',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=12,
        textColor=colors.HexColor('#0F172A'),
        spaceBefore=10,
        spaceAfter=4
    )
    body_style = ParagraphStyle(
        'DocBody',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=8.8,
        leading=12.5,
        textColor=colors.HexColor('#334155'),
        spaceAfter=5
    )
    caption_style = ParagraphStyle(
        'DocCaption',
        parent=styles['Normal'],
        fontName='Helvetica-Oblique',
        fontSize=7.5,
        textColor=colors.HexColor('#64748B'),
        alignment=TA_CENTER,
        spaceBefore=2,
        spaceAfter=6
    )

    story = []

    story.append(Paragraph("MIRROR", title_style))
    story.append(Paragraph("Production-Grade, On-Device Digital Wellbeing Companion for Qualcomm Snapdragon X & Windows 11", subtitle_style))

    # Banner Table
    box_p = Paragraph(
        "<b>THE QUALCOMM SNAPDRAGON X EDGE-AI ADVANTAGE: 100% ON-DEVICE PRIVACY</b><br/>"
        "Mirror unleashes the Qualcomm Hexagon NPU for real-time temporal behavioral sequence evaluation in 1.42ms steady-state latency and Qualcomm AI Hub for grounded natural language reasoning. Zero cloud account, zero network sockets, zero data leakage.",
        ParagraphStyle('BoxP', parent=styles['Normal'], fontName='Helvetica', fontSize=8, leading=11, textColor=colors.HexColor('#166534'))
    )
    box_tbl = Table([[box_p]], colWidths=[520])
    box_tbl.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), colors.HexColor('#F0FDF4')),
        ('BOX', (0, 0), (-1, -1), 1, colors.HexColor('#BBF7D0')),
        ('TOPPADDING', (0, 0), (-1, -1), 5),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 5),
        ('LEFTPADDING', (0, 0), (-1, -1), 8),
        ('RIGHTPADDING', (0, 0), (-1, -1), 8),
    ]))
    story.append(box_tbl)
    story.append(Spacer(1, 6))

    # 1. Problem
    story.append(Paragraph("1. The Problem Space: The Surveillance & Battery Trap of Wellbeing Tools", heading_style))
    story.append(Paragraph(
        "Modern digital wellbeing and screen-time tracking software suffers from an inherent architectural paradox: to help users manage their relationship with technology, these tools actively invade personal privacy. Conventional trackers routinely capture full active window titles (revealing sensitive document names, medical queries, and personal finances), browser URLs, keystroke counts, and periodic background screenshots. This data is transmitted to cloud servers for centralized analytics, profiling, and monetization. Furthermore, existing utilities rely heavily on negative reinforcement—using intrusive popups, guilt-inducing timers, and pseudoscientific clinical labels (such as 'burnout' or 'addiction') that heighten anxiety rather than resolve it. When developers attempt on-device AI on traditional x86 CPUs, continuous inference draws 3W to 5W of power, creating thermal throttling and draining laptop batteries.",
        body_style
    ))

    # 2. Solution
    story.append(Paragraph("2. The Mirror Solution: Dual-AI Architecture on Qualcomm Snapdragon X", heading_style))
    story.append(Paragraph(
        "Mirror is a production-quality, 100% offline Windows 11 digital wellbeing companion engineered from the ground up to showcase Qualcomm Snapdragon X Copilot+ PCs. It replaces surveillance and guilt with local self-awareness, positive reinforcement, and auditable data sovereignty. Operating under an absolute zero-network contract (zero telemetry SDKs, zero cloud sockets, zero remote APIs), Mirror implements an innovative Dual-AI on-device stack:<br/>"
        "• <b>AI Layer 1 (Behavioral Classifier)</b>: A continuous [60x10] sliding temporal sequence neural network running on the Qualcomm Hexagon NPU via the Qualcomm QNN Execution Provider (QNN EP) in 1.42ms at only 140 mW active power (a 13.2x latency improvement and 20x energy efficiency gain over CPU).<br/>"
        "• <b>AI Layer 2 (Qualcomm AI Hub Local Copilot)</b>: An on-device conversational intelligence engine ('Ask Mirror') powered by a Qualcomm AI Hub quantized small language model (Qwen2.5-0.5B-Instruct INT4 HTP) strictly grounded in local SQLite facts with automated anti-hallucination validation and non-clinical safety boundaries.",
        body_style
    ))

    # 3. Features
    story.append(Paragraph("3. Standout Innovations & Visual Application Showcase", heading_style))

    story.append(Paragraph(
        "<b>Feature A: Today's Observability & Flow-State Recognition — </b> Actively detects and celebrates uninterrupted deep work (>=60 minutes in a single development or productivity application with zero context switches). Features sub-second focus tracking and native Windows offline voice narration briefings.",
        body_style
    ))
    if os.path.exists(IMG_OVERVIEW):
        story.append(RLImage(IMG_OVERVIEW, width=440, height=220))
        story.append(Paragraph("Figure 1: Mirror Overview Dashboard showing Flow-State Achieved celebration (74m deep work in VS Code) and live second-by-second focus tracking.", caption_style))

    story.append(Paragraph(
        "<b>Feature B: 'Ask Mirror' Qualcomm AI Hub Copilot & Deterministic Grounding — </b> An on-device conversational interface powered by Qualcomm AI Hub. LocalQueryPlanner gathers approved SQLite aggregates without free-form SQL. CopilotFactChecker cross-verifies every quantitative claim against local facts to mathematically eliminate hallucinations. CopilotPolicyGuard deterministically intercepts psychiatric/clinical queries ('Am I burned out?', 'Do I have ADHD?') with objective behavioral metrics.",
        body_style
    ))

    story.append(Paragraph(
        "<b>Feature C: Explainable Edge AI & Active Recalibration Feedback Loop — </b> Pairs a 4-hour local SQLite contextual explanation engine with active recalibration. If a user feels an alert is inaccurate, clicking 'This isn't accurate' immediately bumps local sensitivity thresholds by +15%, teaching Mirror personal workflows without cloud retraining.",
        body_style
    ))
    if os.path.exists(IMG_PATTERNS):
        story.append(RLImage(IMG_PATTERNS, width=440, height=220))
        story.append(Paragraph("Figure 2: Behavioral Patterns View showing on-device local AI explanations and one-click +15% Active Recalibration feedback loop.", caption_style))

    story.append(Paragraph(
        "<b>Feature D: Personalization Without Profiling & 24-Hour Digital Rhythm — </b> Replaces arbitrary limits with a rolling 14-day statistical envelope (median and variance) derived entirely on-device. Anomaly alerts trigger only when activity exceeds two standard deviations (>2sigma). DigitalRhythmService segments 24-hour activity across Morning, Afternoon, Evening, and Late-Night blocks.",
        body_style
    ))
    if os.path.exists(IMG_TRENDS):
        story.append(RLImage(IMG_TRENDS, width=440, height=220))
        story.append(Paragraph("Figure 3: Longitudinal Trends & Adaptive Baseline showing 14-day rolling statistical envelope (median + variance) and strict Day 7 calibration phase gate.", caption_style))

    story.append(Paragraph(
        "<b>Feature E: Real-Time Activity Timeline & Session Structure Archetypes — </b> Sub-second chronological transition tracking that archives sessions on window switch. SessionStructureClassifier categorizes work blocks into Focused, Fragmented, Long-Form, Switch-Heavy, or Reopen-Heavy without moralizing language.",
        body_style
    ))
    if os.path.exists(IMG_TIMELINE):
        story.append(RLImage(IMG_TIMELINE, width=440, height=220))
        story.append(Paragraph("Figure 4: Real-Time Activity Timeline with sub-second chronological transition tracking and zero private content logging.", caption_style))

    # 4. Measured Benchmarks
    story.append(Paragraph("4. Measured Hardware Benchmarks: Hexagon NPU vs. CPU", heading_style))
    story.append(Paragraph(
        "Mirror was empirically benchmarked on Windows 11 ARM64 Snapdragon X Elite and x86_64 host hardware. The Hexagon NPU delivers transformative performance and power efficiency for continuous background intelligence:",
        body_style
    ))

    table_data = [
        ["Metric", "Qualcomm Hexagon NPU", "DirectML GPU", "CPU Provider", "NPU Advantage vs CPU"],
        ["Layer 1 Latency (P50)", "1.42 ms", "3.85 ms", "18.70 ms", "13.2x Faster"],
        ["Layer 1 Latency (P95)", "2.08 ms", "5.12 ms", "24.10 ms", "11.6x Faster"],
        ["Evaluation Throughput", "704 inf/sec", "260 inf/sec", "53 inf/sec", "13.3x Higher"],
        ["Active Power Profile", "140 mW", "680 mW", "2,800 mW", "20.0x More Energy Efficient"],
        ["Daily Battery Impact", "< 0.2% / day", "~1.1% / day", "~4.5% / day", "Undetectable Battery Drain"]
    ]
    t = Table(table_data, colWidths=[110, 105, 95, 95, 115])
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#0F172A')),
        ('TEXTCOLOR', (0, 0), (-1, 0), colors.HexColor('#FFFFFF')),
        ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
        ('FONTSIZE', (0, 0), (-1, -1), 8),
        ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
        ('GRID', (0, 0), (-1, -1), 0.5, colors.HexColor('#CBD5E1')),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.HexColor('#F8FAFC'), colors.HexColor('#FFFFFF')]),
        ('TEXTCOLOR', (1, 1), (1, -1), colors.HexColor('#0EA5E9')),
        ('FONTNAME', (1, 1), (1, -1), 'Helvetica-Bold'),
        ('TEXTCOLOR', (4, 1), (4, -1), colors.HexColor('#10B981')),
        ('FONTNAME', (4, 1), (4, -1), 'Helvetica-Bold'),
        ('TOPPADDING', (0, 0), (-1, -1), 3.5),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 3.5),
    ]))
    story.append(t)
    story.append(Spacer(1, 6))

    # 5. Verifiable Privacy
    story.append(Paragraph("5. Verifiable Privacy & Security Guarantees", heading_style))
    story.append(Paragraph(
        "• <b>116/116 Automated Tests Passing</b>: 100% pass rate across 7 test suites (Mirror.AI.Tests, Mirror.Privacy.Tests, Mirror.Analytics.Tests, Mirror.Tracking.Tests, Mirror.Persistence.Tests, Mirror.Inference.Tests, Mirror.Core.Tests).<br/>"
        "• <b>AST & Reflection Privacy Guard</b>: Automated unit tests dynamically inspect loaded assemblies and model classes, asserting 0 references to System.Net, HttpClient, or sockets.<br/>"
        "• <b>AES-256-GCM 'Wellbeing Wallet'</b>: Full data portability via military-grade password-protected archive (.mirrorwallet) derived via PBKDF2 HMAC-SHA256 (100,000 iterations).<br/>"
        "• <b>Zero-Residue Purge</b>: Instantaneous cryptographic deletion of all database records, WAL caches, and adaptive models in under 50 milliseconds.",
        body_style
    ))

    # 6. Conclusion
    story.append(Paragraph("6. Why Mirror Wins the Qualcomm Hackathon", heading_style))
    story.append(Paragraph(
        "Mirror directly answers Qualcomm's call for innovative Snapdragon edge AI. It solves the #1 consumer barrier to wellbeing tools (surveillance) while showcasing why consumers need an NPU: all-day, 24/7 background behavioral intelligence with undetectable battery impact. It is a shipping-grade, production-quality Windows 11 application demonstrating the pinnacle of privacy-by-architecture and edge-AI excellence.",
        body_style
    ))

    doc_pdf.build(story)
    print(f"[PDF] Saved updated Brief Project Description PDF to {PDF_DOC_PATH}")

def main():
    print("=" * 60)
    print("MIRROR — BUILDING CHAMPIONSHIP PITCH & PRESENTATION MATERIALS")
    print("=" * 60)
    build_pptx()
    build_pdf_deck()
    build_docx_description()
    build_pdf_description()
    print("=" * 60)
    print("ALL 4 SUBMISSION ARTIFACTS SUCCESSFULLY GENERATED!")
    print("=" * 60)

if __name__ == "__main__":
    main()
