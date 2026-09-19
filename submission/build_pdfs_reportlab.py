import os
from reportlab.lib import pagesizes, colors
from reportlab.lib.units import inch
from reportlab.pdfgen import canvas
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, KeepTogether
)
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT, TA_JUSTIFY

SUBMISSION_DIR = r"D:\MIRROR\submission"
DOCX_PDF_PATH = os.path.join(SUBMISSION_DIR, "Mirror_Brief_Project_Description.pdf")
PPTX_PDF_PATH = os.path.join(SUBMISSION_DIR, "Mirror_Pitch_Deck.pdf")

# ==============================================================================
# 1. BRIEF PROJECT DESCRIPTION (PDF via ReportLab)
# ==============================================================================
def create_brief_description_pdf():
    print("[PDF 1/2] Generating Mirror_Brief_Project_Description.pdf...")
    doc = SimpleDocTemplate(
        DOCX_PDF_PATH,
        pagesize=pagesizes.letter,
        leftMargin=54,
        rightMargin=54,
        topMargin=54,
        bottomMargin=54
    )

    styles = getSampleStyleSheet()
    
    # Custom styles
    title_style = ParagraphStyle(
        'DocTitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=24,
        leading=28,
        textColor=colors.HexColor('#0EA5E9'),
        spaceAfter=4
    )

    sub_style = ParagraphStyle(
        'DocSubtitle',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=12,
        leading=16,
        textColor=colors.HexColor('#0F172A'),
        spaceAfter=12
    )

    h1_style = ParagraphStyle(
        'DocH1',
        parent=styles['Normal'],
        fontName='Helvetica-Bold',
        fontSize=13,
        leading=17,
        textColor=colors.HexColor('#0F172A'),
        spaceBefore=12,
        spaceAfter=5
    )

    body_style = ParagraphStyle(
        'DocBody',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=9.5,
        leading=14,
        textColor=colors.HexColor('#334155'),
        spaceAfter=6,
        alignment=TA_JUSTIFY
    )

    bullet_style = ParagraphStyle(
        'DocBullet',
        parent=styles['Normal'],
        fontName='Helvetica',
        fontSize=9.5,
        leading=13.5,
        textColor=colors.HexColor('#334155'),
        spaceAfter=4
    )

    story = []

    # Title & Subtitle
    story.append(Paragraph("MIRROR", title_style))
    story.append(Paragraph("Production-Grade, On-Device Digital Wellbeing Companion for Windows 11 & Snapdragon X", sub_style))

    # Privacy Box
    box_content = [
        Paragraph("<b>THE CORE ARCHITECTURAL PROMISE: 100% ON-DEVICE PRIVACY</b>", ParagraphStyle('B1', fontName='Helvetica-Bold', fontSize=9.5, leading=13, textColor=colors.HexColor('#166534'))),
        Spacer(1, 3),
        Paragraph("Your behavioral data stays on your computer. Mirror does not need a cloud account, cloud database, analytics SDK, remote AI service, or internet connection.", ParagraphStyle('B2', fontName='Helvetica', fontSize=8.5, leading=12, textColor=colors.HexColor('#15803D')))
    ]
    t = Table([[box_content]], colWidths=[504])
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, -1), colors.HexColor('#F0FDF4')),
        ('BOX', (0, 0), (-1, -1), 1, colors.HexColor('#86EFAC')),
        ('TOPPADDING', (0, 0), (-1, -1), 8),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 8),
        ('LEFTPADDING', (0, 0), (-1, -1), 12),
        ('RIGHTPADDING', (0, 0), (-1, -1), 12),
    ]))
    story.append(t)
    story.append(Spacer(1, 10))

    # Section 1
    story.append(Paragraph("1. The Problem Space: The Surveillance Trap of Wellbeing Tools", h1_style))
    story.append(Paragraph(
        "Modern digital wellbeing and screen-time tracking software suffers from an inherent architectural paradox: to help users manage their relationship with technology, these tools actively invade personal privacy. Conventional trackers routinely capture full active window titles (revealing sensitive document names, medical queries, and personal finances), browser URLs, keystroke counts, and periodic background screenshots. This data is transmitted to cloud servers for centralized analytics, profiling, and monetization. Furthermore, existing utilities rely heavily on negative reinforcement—using intrusive popups, guilt-inducing timers, and pseudoscientific clinical labels (such as 'burnout' or 'addiction') that heighten anxiety rather than resolve it.",
        body_style
    ))

    # Section 2
    story.append(Paragraph("2. The Mirror Solution: Intelligence Without Surveillance", h1_style))
    story.append(Paragraph(
        "Mirror is an entirely on-device, privacy-preserving digital wellbeing companion built specifically for Windows 11 and optimized for Qualcomm Snapdragon X (ARM64) Copilot+ PCs, with seamless DirectML (GPU) and CPU fallback. Mirror operates on a simple principle: observability without surveillance. It monitors only coarse application-level foreground transitions via Win32 OS event hooks, extracts 60×10 temporal behavioral matrices, executes an on-device 1D-CNN temporal sequence model via ONNX Runtime, and provides calm, constructive behavioral feedback.",
        body_style
    ))
    story.append(Paragraph(
        "<b>Zero Content Inspection:</b> Mirror never inspects window titles, URLs, file paths, keystrokes, clipboard data, webcam feeds, or screen contents. Its SQLite database schema intentionally contains zero columns capable of storing user-generated text.",
        body_style
    ))

    # Section 3
    story.append(Paragraph("3. Seven Standout Innovations & Differentiators", h1_style))
    inno_list = [
        ("1. Adaptive Personal Baseline", "Replaces arbitrary static thresholds with a statistical rolling median and standard deviation envelope over 7/14 days. Enforces a strict Day 7 Learning Phase gate where alerts are completely muted while Mirror learns typical user behavior. Behavioral patterns are surfaced only when activity exceeds 2 standard deviations (>2σ) beyond personal baseline."),
        ("2. Explain with Local AI (SLM / RAG)", "A 100% offline retrieval-augmented generation (RAG) pipeline that fetches a 4-hour chronological context window from SQLite when a pattern is flagged. Generates concise, objective, 2-sentence situational explanations without hallucinations or medical terminology."),
        ("3. Flow-State Recognition", "Actively celebrates sustained productivity by detecting ≥60 minutes of uninterrupted focus in a development or productivity application with zero distractions, zero app switches, and continuous activity. Presented via an emerald/cyan hero card on the dashboard."),
        ("4. Active Recalibration Feedback Loop", "Empowers users with a 'This isn't accurate' feedback button on every flagged pattern. Clicking it instantly increases detection sensitivity by +15% for that rule and application combination, preventing repetitive false positives. All recalibrations can be audited and reset in Settings."),
        ("5. Voice-Narrated Daily Summary", "Leverages native Windows offline speech synthesis (System.Speech) to produce a calm, human-crafted daily briefing of active focus hours, top applications, and focus periods—without cloud APIs or external downloads."),
        ("6. Encrypted 'Wellbeing Wallet'", "Gives users absolute data sovereignty via military-grade AES-256-GCM password-protected encryption (PBKDF2 HMAC-SHA256, 100,000 iterations). Exports the complete SQLite database and JSON histories into a portable .mirrorwallet archive with lossless one-click restore."),
        ("7. Weekly PDF Report Card", "Generates executive-level vector PDF report cards locally via QuestPDF. Features Windows 11 Fluent styling, metric distribution cards, category breakdowns, and a verifiable 100% On-Device Local Privacy Seal saved directly to Documents\\Mirror.")
    ]
    for name, desc in inno_list:
        story.append(Paragraph(f"• <b>{name}:</b> {desc}", bullet_style))

    # Section 4
    story.append(Paragraph("4. Technical Specifications & Verified Performance", h1_style))
    tech_table_data = [
        [Paragraph("<b>Component</b>", ParagraphStyle('TH1', fontName='Helvetica-Bold', fontSize=8.5, textColor=colors.HexColor('#0F172A'))),
         Paragraph("<b>Implementation & Verification</b>", ParagraphStyle('TH2', fontName='Helvetica-Bold', fontSize=8.5, textColor=colors.HexColor('#0F172A')))],
        [Paragraph("<b>UI Layer</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("C# 12, .NET 8.0, Windows App SDK 1.6, WinUI 3 (Fluent Design System, Mica backdrop)", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))],
        [Paragraph("<b>Tracking</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("Win32 SetWinEventHook (foreground app switch), GetLastInputInfo (hardware idle detection)", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))],
        [Paragraph("<b>Persistence</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("SQLite 3 Write-Ahead Logging (WAL) mode, zero-residue data purge, AES-256-GCM cryptography", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))],
        [Paragraph("<b>Edge AI</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("ONNX Runtime 1.20 with Qualcomm QNN EP (Snapdragon NPU), DirectML (GPU), and CPU fallback", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))],
        [Paragraph("<b>Performance</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("Steady-state P50 latency of 0.02 ms (20 µs), warmup < 2.6 ms, memory footprint < 67 MB RAM", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))],
        [Paragraph("<b>Testing</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("65 / 65 unit, integration, and security tests passing across 6 test suites with 100% pass rate", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))],
        [Paragraph("<b>Privacy Audit</b>", ParagraphStyle('TD1', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155'))),
         Paragraph("Reflection scanner confirms 0 networking namespaces (System.Net, HttpClient) and 0 forbidden APIs", ParagraphStyle('TD2', fontName='Helvetica', fontSize=8, textColor=colors.HexColor('#334155')))]
    ]
    tt = Table(tech_table_data, colWidths=[100, 404])
    tt.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#F1F5F9')),
        ('GRID', (0, 0), (-1, -1), 0.5, colors.HexColor('#CBD5E1')),
        ('TOPPADDING', (0, 0), (-1, -1), 3),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 3),
        ('LEFTPADDING', (0, 0), (-1, -1), 6),
        ('RIGHTPADDING', (0, 0), (-1, -1), 6),
    ]))
    story.append(tt)

    doc.build(story)
    print(f"Generated {DOCX_PDF_PATH} successfully!")


# ==============================================================================
# 2. SHORT PITCH PRESENTATION (PDF via ReportLab)
# ==============================================================================
def create_pitch_presentation_pdf():
    print("[PDF 2/2] Generating Mirror_Pitch_Deck.pdf (16:9 Widescreen)...")
    width, height = 13.333 * inch, 7.5 * inch
    c = canvas.Canvas(PPTX_PDF_PATH, pagesize=(width, height))

    # Color definitions
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

    def draw_background():
        c.setFillColor(BG_SLATE_900)
        c.rect(0, 0, width, height, fill=1, stroke=0)

    def draw_card(x, y, w, h, bg_color=CARD_SLATE_800, border_color=BORDER_SLATE_700, r=8):
        c.setFillColor(bg_color)
        c.setStrokeColor(border_color)
        c.setLineWidth(1.5)
        c.roundRect(x, y, w, h, r, fill=1, stroke=1)

    def draw_header(tag, title, tag_color=ACCENT_CYAN):
        c.setFont("Helvetica-Bold", 11)
        c.setFillColor(tag_color)
        c.drawString(1.0 * inch, height - 0.9 * inch, tag.upper())

        c.setFont("Helvetica-Bold", 24)
        c.setFillColor(TEXT_WHITE)
        c.drawString(1.0 * inch, height - 1.35 * inch, title)

    # -------------------------------------------------------------
    # SLIDE 1: Title Slide
    # -------------------------------------------------------------
    draw_background()
    # Top badge
    draw_card(1.0 * inch, height - 1.5 * inch, 3.8 * inch, 0.4 * inch, bg_color=ACCENT_EMERALD, border_color=ACCENT_EMERALD, r=6)
    c.setFont("Helvetica-Bold", 9)
    c.setFillColor(BG_SLATE_900)
    c.drawCentredString(1.0 * inch + 1.9 * inch, height - 1.35 * inch, "100% ON-DEVICE  |  WINDOWS 11 & SNAPDRAGON X")

    c.setFont("Helvetica-Bold", 52)
    c.setFillColor(ACCENT_CYAN)
    c.drawString(1.0 * inch, height - 2.5 * inch, "MIRROR")

    c.setFont("Helvetica-Bold", 24)
    c.setFillColor(TEXT_WHITE)
    c.drawString(1.0 * inch, height - 3.1 * inch, "The Privacy-First, On-Device Digital Wellbeing Companion")

    c.setFont("Helvetica", 14)
    c.setFillColor(TEXT_MUTED)
    c.drawString(1.0 * inch, height - 3.55 * inch, "Edge-AI Behavioral Intelligence Without Cloud Surveillance")

    # 3 Highlight Cards
    h_data = [
        ("ZERO CLOUD", "No account, no servers, no telemetry. 100% local SQLite & ONNX Runtime.", ACCENT_CYAN),
        ("SNAPDRAGON X NPU", "Hardware-accelerated QNN inference: 0.02ms P50 latency at <67 MB RAM.", ACCENT_EMERALD),
        ("NON-CLINICAL & ETHICAL", "No medical diagnosis, no shaming. Respectful, objective behavioral feedback.", ACCENT_AMBER)
    ]
    card_w = 3.5 * inch
    card_h = 2.0 * inch
    for i, (head, desc, clr) in enumerate(h_data):
        x = 1.0 * inch + i * 3.9 * inch
        y = 0.9 * inch
        draw_card(x, y, card_w, card_h)
        c.setFont("Helvetica-Bold", 14)
        c.setFillColor(clr)
        c.drawString(x + 18, y + card_h - 32, head)
        c.setFont("Helvetica", 11)
        c.setFillColor(TEXT_MUTED)
        
        # Word wrap desc
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 34:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        
        for li, line in enumerate(lines):
            c.drawString(x + 18, y + card_h - 60 - li * 17, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 2: The Problem
    # -------------------------------------------------------------
    draw_background()
    draw_header("THE PROBLEM", "Current Screen-Time Trackers Compromise User Privacy", ACCENT_RED)

    prob_data = [
        ("Invasive Surveillance", "Existing utilities capture full active window titles, visited browser URLs, keystrokes, and periodic screenshots, exposing banking, medical, and private chats.", ACCENT_RED),
        ("Cloud Dependency & Leaks", "Data is synced to remote cloud databases for centralized analytics and profiling, creating persistent corporate surveillance vectors and breach risks.", ACCENT_AMBER),
        ("Negative Reinforcement & Shaming", "Conventional apps focus on guilt, punitive timers, and pseudo-clinical labels ('burnout', 'addiction'), increasing anxiety rather than cultivating genuine agency.", colors.HexColor('#F87171'))
    ]
    card_w = 3.5 * inch
    card_h = 4.4 * inch
    for i, (head, desc, clr) in enumerate(prob_data):
        x = 1.0 * inch + i * 3.9 * inch
        y = 1.0 * inch
        draw_card(x, y, card_w, card_h)
        c.setFont("Helvetica-Bold", 18)
        c.setFillColor(clr)
        c.drawString(x + 20, y + card_h - 40, f"0{i+1}")
        c.setFont("Helvetica-Bold", 15)
        c.setFillColor(TEXT_WHITE)
        c.drawString(x + 20, y + card_h - 75, head)

        # Body
        c.setFont("Helvetica", 11.5)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 32:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 20, y + card_h - 110 - li * 20, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 3: The Solution
    # -------------------------------------------------------------
    draw_background()
    draw_header("THE MIRROR SOLUTION", "Observability Without Surveillance: 100% On-Device AI", ACCENT_EMERALD)

    sol_data = [
        ("Coarse Tracking Only", "Captures only coarse OS foreground app switches and system idle time. Zero window titles, zero URLs, zero keystrokes, zero screenshots.", ACCENT_CYAN),
        ("Local Edge-AI Engine", "Runs ONNX models directly on the Qualcomm Hexagon NPU / DirectML GPU / CPU. Insights are computed locally without transmitting a single byte.", ACCENT_EMERALD),
        ("Positive & Non-Clinical", "Focuses on flow-state recognition, adaptive personal baselines, and objective observations without medical pseudo-diagnoses.", ACCENT_AMBER),
        ("Absolute Data Sovereignty", "Data is stored in local SQLite WAL files. Export military-grade AES-256-GCM encrypted 'Wellbeing Wallets' that you control completely.", ACCENT_PURPLE)
    ]
    w_sol = 5.45 * inch
    h_sol = 2.1 * inch
    for i, (title, desc, clr) in enumerate(sol_data):
        row = i // 2
        col = i % 2
        x = 1.0 * inch + col * 5.85 * inch
        y = height - 2.2 * inch - row * 2.45 * inch - h_sol
        draw_card(x, y, w_sol, h_sol)
        c.setFont("Helvetica-Bold", 15)
        c.setFillColor(clr)
        c.drawString(x + 20, y + h_sol - 32, title)

        c.setFont("Helvetica", 11)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 50:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 20, y + h_sol - 62 - li * 18, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 4: Architecture Pipeline
    # -------------------------------------------------------------
    draw_background()
    draw_header("TECHNICAL ARCHITECTURE", "End-to-End On-Device Pipeline", ACCENT_CYAN)

    pipe_steps = [
        ("1. Win32 Hook", "SetWinEventHook & GetLastInputInfo detect app focus & idle state without reading titles or inputs."),
        ("2. Coarse Aggregator", "Normalizes sessions into (AppKey, Category, Duration, IdleSeconds). Zero content columns."),
        ("3. 60×10 Feature Tensor", "Encodes temporal sequences into sliding normalized vectors of app switches and durations."),
        ("4. Edge-AI Inference", "ONNX Runtime with Qualcomm QNN NPU / DirectML executes 1D-CNN model in 0.02ms P50 latency."),
        ("5. Signal Fusion", "Blends deterministic rules with ML probabilities to eliminate false positives."),
        ("6. Fluent WinUI 3 UI", "Renders real-time live cards, timelines, flow badges, and interactive recalibration controls.")
    ]
    w_pipe = 3.55 * inch
    h_pipe = 2.1 * inch
    for i, (title, desc) in enumerate(pipe_steps):
        row = i // 3
        col = i % 3
        x = 1.0 * inch + col * 3.88 * inch
        y = height - 2.1 * inch - row * 2.35 * inch - h_pipe
        draw_card(x, y, w_pipe, h_pipe)
        c.setFont("Helvetica-Bold", 14)
        c.setFillColor(ACCENT_CYAN)
        c.drawString(x + 18, y + h_pipe - 30, title)

        c.setFont("Helvetica", 10.5)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 35:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 18, y + h_pipe - 56 - li * 17, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 5: 7 Innovations
    # -------------------------------------------------------------
    draw_background()
    draw_header("INNOVATION & DIFFERENTIATION", "Seven Standout Features That Redefine Wellbeing", ACCENT_EMERALD)

    inno_cards = [
        ("Adaptive Personal Baseline", "Rolling 7/14-day median & standard deviation with Day 7 learning gate. Alerts only trigger if activity exceeds >2σ.", ACCENT_CYAN),
        ("Explain with Local AI (SLM / RAG)", "On-device RAG extracting 4-hour SQLite context windows, synthesizing neutral 2-sentence situational explanations.", ACCENT_EMERALD),
        ("Flow-State Recognition", "Positive reinforcement detecting ≥60 min uninterrupted focus with 0 distractions. Emerald hero card celebration.", ACCENT_EMERALD),
        ("Active Recalibration Feedback", "'This isn't accurate' one-click feedback button dynamically bumps sensitivity thresholds by +15%.", ACCENT_AMBER),
        ("Voice-Narrated Daily Summary", "Native Windows offline speech synthesis (System.Speech) delivering warm, human daily observational audio briefings.", ACCENT_PURPLE),
        ("Encrypted Wellbeing Wallet", "Password-protected AES-256-GCM encrypted ZIP archive (.mirrorwallet) ensuring 100% portable, sovereign data.", ACCENT_CYAN),
        ("Weekly PDF Report Card", "Executive printable vector PDF generated locally via QuestPDF with Windows 11 Fluent aesthetic and Local Privacy Seal.", colors.HexColor('#EC4899'))
    ]
    # Row 1: 4 cards
    w4 = 2.65 * inch
    h4 = 2.25 * inch
    for i in range(4):
        title, desc, clr = inno_cards[i]
        x = 1.0 * inch + i * 2.9 * inch
        y = height - 2.05 * inch - h4
        draw_card(x, y, w4, h4)
        c.setFont("Helvetica-Bold", 12)
        c.setFillColor(clr)
        c.drawString(x + 14, y + h4 - 26, title[:26])
        if len(title) > 26:
            c.drawString(x + 14, y + h4 - 40, title[26:])

        c.setFont("Helvetica", 9.5)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 28:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 14, y + h4 - 60 - li * 15, line)

    # Row 2: 3 cards
    w3 = 3.6 * inch
    h3 = 2.2 * inch
    for i in range(4, 7):
        title, desc, clr = inno_cards[i]
        idx = i - 4
        x = 1.0 * inch + idx * 3.86 * inch
        y = 0.8 * inch
        draw_card(x, y, w3, h3)
        c.setFont("Helvetica-Bold", 13)
        c.setFillColor(clr)
        c.drawString(x + 16, y + h3 - 28, title)

        c.setFont("Helvetica", 10)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 38:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 16, y + h3 - 56 - li * 16, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 6: NPU & Performance
    # -------------------------------------------------------------
    draw_background()
    draw_header("HARDWARE ACCELERATION & EDGE AI", "Optimized First for Qualcomm Snapdragon X Copilot+ PCs", ACCENT_CYAN)

    stats = [
        ("0.02 ms", "STEADY-STATE P50 LATENCY", "Over 50x faster than real-time threshold requirements. High-efficiency inference.", ACCENT_CYAN),
        ("< 67 MB", "PROCESS MEMORY FOOTPRINT", "Extremely lightweight footprint suitable for constant background execution.", ACCENT_EMERALD),
        ("< 0.1%", "BACKGROUND CPU LOAD", "Virtually zero battery drain on Snapdragon X & x64 laptops throughout the day.", ACCENT_AMBER),
        ("3-Tier", "EXECUTION PROVIDER FALLBACK", "Qualcomm QNN (NPU) -> DirectML (GPU) -> CPU transparent runtime fallback.", ACCENT_PURPLE)
    ]
    w_stat = 2.65 * inch
    h_stat = 4.4 * inch
    for i, (metric, label, desc, clr) in enumerate(stats):
        x = 1.0 * inch + i * 2.9 * inch
        y = 1.0 * inch
        draw_card(x, y, w_stat, h_stat)

        c.setFont("Helvetica-Bold", 34)
        c.setFillColor(clr)
        c.drawString(x + 18, y + h_stat - 48, metric)

        c.setFont("Helvetica-Bold", 9.5)
        c.setFillColor(TEXT_WHITE)
        c.drawString(x + 18, y + h_stat - 80, label)

        c.setFont("Helvetica", 11)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 24:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 18, y + h_stat - 120 - li * 18, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 7: Security & Audit
    # -------------------------------------------------------------
    draw_background()
    draw_header("VERIFICATION & SECURITY AUDIT", "Rigorous Verification & Indisputable Privacy", ACCENT_EMERALD)

    verifs = [
        ("65 / 65 Automated Tests", "100% test pass rate across 6 test suites: Core, Tracking, Persistence, Analytics, Inference, and Privacy.", ACCENT_EMERALD),
        ("Reflection Privacy Guard", "Automated code inspection dynamically verifies zero references to System.Net, HttpClient, or sockets.", ACCENT_CYAN),
        ("Military-Grade AES-256-GCM", "Wellbeing wallet exports use 100,000 iteration PBKDF2 key derivation and AES-GCM authenticated cipher.", ACCENT_PURPLE),
        ("Non-Clinical Vocabulary Enforced", "Strict ethical test asserts zero medical, psychiatric, or diagnostic terms in all user-facing copy.", ACCENT_AMBER)
    ]
    w_ver = 5.45 * inch
    h_ver = 2.1 * inch
    for i, (title, desc, clr) in enumerate(verifs):
        row = i // 2
        col = i % 2
        x = 1.0 * inch + col * 5.85 * inch
        y = height - 2.2 * inch - row * 2.45 * inch - h_ver
        draw_card(x, y, w_ver, h_ver)
        c.setFont("Helvetica-Bold", 15)
        c.setFillColor(clr)
        c.drawString(x + 20, y + h_ver - 32, title)

        c.setFont("Helvetica", 11)
        c.setFillColor(TEXT_MUTED)
        words = desc.split()
        lines = []
        cur = []
        for w in words:
            if len(" ".join(cur + [w])) < 50:
                cur.append(w)
            else:
                lines.append(" ".join(cur))
                cur = [w]
        if cur: lines.append(" ".join(cur))
        for li, line in enumerate(lines):
            c.drawString(x + 20, y + h_ver - 62 - li * 18, line)

    c.showPage()

    # -------------------------------------------------------------
    # SLIDE 8: Summary & Vision
    # -------------------------------------------------------------
    draw_background()
    draw_card(1.0 * inch, 0.9 * inch, 11.3 * inch, 5.7 * inch, border_color=ACCENT_CYAN)

    c.setFont("Helvetica-Bold", 11)
    c.setFillColor(ACCENT_CYAN)
    c.drawString(1.4 * inch, height - 1.6 * inch, "THE FUTURE OF DIGITAL WELLBEING")

    c.setFont("Helvetica-Bold", 28)
    c.setFillColor(TEXT_WHITE)
    c.drawString(1.4 * inch, height - 2.15 * inch, "Empowering Users with True On-Device Edge Intelligence")

    c.setFont("Helvetica", 13.5)
    c.setFillColor(TEXT_MUTED)
    p_text = (
        "Mirror proves that personal edge AI does not require sacrificing personal privacy. "
        "By demonstrating that sophisticated behavioral intelligence, personalized baselines, "
        "generative voice briefings, and local RAG can run entirely on Snapdragon X NPUs and "
        "Windows 11 PCs, Mirror establishes a new standard for ethical, user-sovereign computing."
    )
    words = p_text.split()
    lines = []
    cur = []
    for w in words:
        if len(" ".join(cur + [w])) < 76:
            cur.append(w)
        else:
            lines.append(" ".join(cur))
            cur = [w]
    if cur: lines.append(" ".join(cur))
    for li, line in enumerate(lines):
        c.drawString(1.4 * inch, height - 2.8 * inch - li * 22, line)

    # Bullet points
    c.setFont("Helvetica-Bold", 12.5)
    c.setFillColor(TEXT_WHITE)
    bullets = [
        ("Production-Grade Architecture", "17 decoupled modules, WinUI 3, 65/65 passing tests, zero cloud dependency."),
        ("Qualcomm Snapdragon X Ready", "Instant NPU acceleration with graceful GPU/CPU fallback (0.02 ms latency)."),
        ("Indisputable User Sovereignty", "Your behavioral data stays on your PC. Encrypted wallet export/restore anytime.")
    ]
    for bi, (b_title, b_desc) in enumerate(bullets):
        by = height - 4.2 * inch - bi * 34
        c.setFillColor(ACCENT_EMERALD)
        c.circle(1.5 * inch, by + 4, 3, fill=1, stroke=0)
        c.setFillColor(TEXT_WHITE)
        c.drawString(1.7 * inch, by, f"{b_title}: ")
        t_len = c.stringWidth(f"{b_title}: ", "Helvetica-Bold", 12.5)
        c.setFont("Helvetica", 12.5)
        c.setFillColor(TEXT_MUTED)
        c.drawString(1.7 * inch + t_len, by, b_desc)
        c.setFont("Helvetica-Bold", 12.5)

    c.showPage()
    c.save()
    print(f"Generated {PPTX_PDF_PATH} successfully!")

if __name__ == '__main__':
    create_brief_description_pdf()
    create_pitch_presentation_pdf()
