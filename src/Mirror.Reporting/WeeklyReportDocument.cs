using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mirror.Reporting;

public class WeeklyReportDocument : IDocument
{
    private readonly WeeklyReportData _data;

    public WeeklyReportDocument(WeeklyReportData data)
    {
        _data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI", "Lato").FontColor("#1E293B"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("MIRROR").FontSize(20).Bold().FontColor("#0EA5E9");
                col.Item().Text("WEEKLY DIGITAL WELLBEING REPORT").FontSize(14).SemiBold().FontColor("#0F172A");
                col.Item().Text(_data.DateRangeString).FontSize(9).FontColor("#64748B");
            });

            row.ConstantItem(180).AlignRight().Column(col =>
            {
                col.Item().Background("#F0FDF4").Padding(6).Border(1).BorderColor("#86EFAC").Column(box =>
                {
                    box.Item().Text("100% ON-DEVICE").FontSize(9).Bold().FontColor("#166534");
                    box.Item().Text("Zero cloud sync. Zero telemetry. Data never leaves your PC.").FontSize(7).FontColor("#15803D");
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(16).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Element(e => StatBox(e, "TOTAL SCREEN TIME", _data.FormattedTotalActive, _data.FormattedDailyAvg, "#0EA5E9"));
                row.ConstantItem(12);
                row.RelativeItem().Element(e => StatBox(e, "FLOW STATE ACHIEVED", _data.FormattedFlowTime, $"{_data.FlowSessionsCount} deep work sessions", "#10B981"));
                row.ConstantItem(12);
                row.RelativeItem().Element(e => StatBox(e, "CONTEXT SWITCHES", _data.TotalSwitches.ToString(), $"~{_data.TotalSwitches / 7}/day", "#8B5CF6"));
                row.ConstantItem(12);
                row.RelativeItem().Element(e => StatBox(e, "LATE NIGHT ACTIVITY", _data.FormattedLateNight, "Post 11:00 PM usage", "#F59E0B"));
            });

            col.Item().PaddingTop(20).Text("TOP APPLICATIONS THIS WEEK").FontSize(11).Bold().FontColor("#334155");

            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Application");
                    header.Cell().Element(HeaderStyle).Text("Category");
                    header.Cell().Element(HeaderStyle).Text("Active Time");
                    header.Cell().Element(HeaderStyle).AlignRight().Text("Share");

                    static IContainer HeaderStyle(IContainer container) =>
                        container.DefaultTextStyle(x => x.SemiBold().FontColor("#64748B").FontSize(8))
                                 .PaddingBottom(4)
                                 .BorderBottom(1)
                                 .BorderColor("#CBD5E1");
                });

                foreach (var app in _data.TopApps)
                {
                    table.Cell().Element(CellStyle).Text(app.DisplayName).SemiBold();
                    table.Cell().Element(CellStyle).Text(app.Category);
                    table.Cell().Element(CellStyle).Text($"{app.ActiveSeconds / 3600}h {(app.ActiveSeconds % 3600) / 60}m");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{app.Percentage:F1}%");

                    static IContainer CellStyle(IContainer container) =>
                        container.BorderBottom(1)
                                 .BorderColor("#F1F5F9")
                                 .PaddingVertical(6)
                                 .DefaultTextStyle(x => x.FontSize(9));
                }
            });

            col.Item().PaddingTop(20).Background("#F8FAFC").Padding(12).Border(1).BorderColor("#E2E8F0").Column(box =>
            {
                box.Item().Text("ADAPTIVE PERSONAL BASELINE").FontSize(10).Bold().FontColor("#0F172A");
                box.Item().PaddingTop(4).Text(_data.BaselineStatus).FontSize(9).FontColor("#475569");
                box.Item().PaddingTop(4).Text($"Behavioral Pattern Events Detected: {_data.PatternEventsCount}").FontSize(8).FontColor("#64748B");
            });
        });
    }

    private static void StatBox(IContainer container, string title, string value, string subtitle, string accentColor)
    {
        container.Border(1)
            .BorderColor("#E2E8F0")
            .Background("#F8FAFC")
            .Padding(10)
            .Column(col =>
            {
                col.Item().Text(title).FontSize(7).SemiBold().FontColor("#64748B");
                col.Item().PaddingTop(2).Text(value).FontSize(14).Bold().FontColor(accentColor);
                col.Item().PaddingTop(2).Text(subtitle).FontSize(7).FontColor("#94A3B8");
            });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor("#E2E8F0").PaddingTop(8).Row(row =>
        {
            row.RelativeItem().Text("Mirror — Private On-Device Digital Wellbeing").FontSize(8).FontColor("#94A3B8");
            row.RelativeItem().AlignRight().Text($"Report generated locally on {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(8).FontColor("#94A3B8");
        });
    }
}
