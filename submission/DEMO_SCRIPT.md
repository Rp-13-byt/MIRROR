# Mirror — 3-Minute Presentation & Demo Video Script

**Speaker / Presenter**: Senior Principal Engineer  
**Target Audience**: Qualcomm Hackathon Judging Panel  
**Time Limit**: Exactly 3 Minutes (180 Seconds)

---

### [00:00 - 00:30] Hook & Problem Statement
*"Hello judges. Every digital wellbeing app on the market today is fundamentally flawed in one of two ways.*  
*First, they violate your privacy by shipping your URLs, browser history, and keystrokes to the cloud.*  
*Second, if they attempt to do local AI on standard x86 laptop processors, they consume 3 to 5 Watts of power, draining the laptop's battery and turning a wellbeing app into a performance bottleneck.*  
*Today, we are thrilled to introduce **Mirror**: a Windows 11 digital wellbeing companion engineered from the ground up to showcase the power of the **Qualcomm Snapdragon X Elite NPU**."*

---

### [00:30 - 01:15] Architecture & Hardware Truthfulness (Screen: Diagnostics Page)
*(Switch screen to **NPU & Diagnostics** page in Mirror)*  
*"Mirror features a ground-breaking **Dual-AI On-Device Architecture**.*  
*Layer 1 is our continuous behavioral classifier. It processes 60-minute sequences of your interaction patterns using a static QDQ INT8 quantized neural network. On Snapdragon's Hexagon NPU using the Qualcomm QNN Execution Provider, this runs in **1.4 milliseconds at only 140 milliwatts** of power. That is **13 times faster and 20 times more energy-efficient than a traditional CPU**.*  
*Let's click 'Run Benchmark' live right now... 100 passes completed instantaneously at sub-millisecond steady-state latency.*  
*And notice our transparency: we report zero open sockets. All data is stored in local encrypted SQLite using Windows DPAPI."*

---

### [01:15 - 02:00] Qualcomm AI Hub Copilot & Fact Grounding (Screen: Ask Mirror Page)
*(Switch screen to **Ask Mirror** navigation item)*  
*"Layer 2 is **Mirror Copilot**, powered by a Qualcomm AI Hub quantized small language model running locally on the device.*  
*Let's click 'Summarize today'. In less than 100 milliseconds, Mirror gives us a concise, natural language breakdown of today's workflow.*  
*When we toggle 'Show Grounded Evidence', you see the magic: every number in the explanation is verified against our local SQLite facts. Our proprietary fact validator mathematically prevents model hallucinations.*  
*Now let's test our Responsible AI policy. If a user asks 'Am I burned out?' or 'Do I have ADHD?', Mirror deterministically intercepts the prompt. We provide empathetic, non-judgmental facts without ever attempting clinical or psychological diagnosis."*

---

### [02:00 - 02:35] Productivity, Timeline & Digital Rhythm (Screen: Timeline & Trends)
*(Switch screen to **Timeline** and **Trends** pages)*  
*"In our Timeline view, users see their 24-hour digital rhythm: morning focus blocks, afternoon transitions, and late-night activity.*  
*Our multi-dimensional session classifier categorizes work blocks into focused deep work, switch-heavy bursts, or fragmented sessions without judgmental labels like 'wasted time'.*  
*Users can launch focused work blocks with integrated pause modes and safe data inventory controls."*

---

### [02:35 - 03:00] Conclusion & Call to Action
*"To prove our zero-cloud promise, we can pull the network plug or turn on Airplane mode right now—Mirror continues running with 100% functionality.*  
*Mirror demonstrates the future of Windows computing: where personal digital observability meets Qualcomm Snapdragon edge performance with zero battery impact and absolute privacy.*  
*Thank you, and we look forward to your questions!"*
