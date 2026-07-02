using CustomerFeedbackSystem.Data.Reporting;

namespace CustomerFeedbackSystem.Load.Presentation;

/// <summary>Renders a LoadReport as a bordered console summary: header banner, per-source table, rejection detail and duration.</summary>
internal static class ConsoleReportRenderer
{
    public static void Render(LoadReport report, string systemName, DateTime executedAt)
    {
        RenderHeader(systemName, executedAt);
        RenderTable(report);
        RenderRejectionDetail(report);
        RenderFooter(report);
    }

    private static void RenderHeader(string systemName, DateTime executedAt)
    {
        var titleLine = $" {systemName} ";
        var dateLine = $" Fecha: {executedAt:yyyy-MM-dd}    Hora: {executedAt:HH:mm:ss} ";
        var width = Math.Max(titleLine.Length, dateLine.Length);

        Console.WriteLine();
        Console.WriteLine("╔" + new string('═', width) + "╗");
        Console.WriteLine("║" + titleLine.PadRight(width) + "║");
        Console.WriteLine("║" + dateLine.PadRight(width) + "║");
        Console.WriteLine("╚" + new string('═', width) + "╝");
    }

    private static void RenderTable(LoadReport report)
    {
        const string headFuente = "Fuente";
        const string headLeidos = "Leídos";
        const string headInsertados = "Insertados";
        const string headRechazados = "Rechazados";

        var sources = report.Sources;
        var widthFuente = Math.Max(headFuente.Length, sources.Count == 0 ? 0 : sources.Max(s => s.SourceName.Length));
        widthFuente = Math.Max(widthFuente, "TOTAL".Length);
        var widthLeidos = Math.Max(headLeidos.Length, NumberWidth(report.TotalRead));
        var widthInsertados = Math.Max(headInsertados.Length, NumberWidth(report.TotalInserted));
        var widthRechazados = Math.Max(headRechazados.Length, NumberWidth(report.TotalRejected));

        void Separator(char left, char mid, char right) =>
            Console.WriteLine(
                left + new string('─', widthFuente + 2) + mid + new string('─', widthLeidos + 2) + mid
                + new string('─', widthInsertados + 2) + mid + new string('─', widthRechazados + 2) + right);

        void Row(string fuente, string leidos, string insertados, string rechazados) =>
            Console.WriteLine(
                $"│ {fuente.PadRight(widthFuente)} │ {leidos.PadLeft(widthLeidos)} │ "
                + $"{insertados.PadLeft(widthInsertados)} │ {rechazados.PadLeft(widthRechazados)} │");

        Console.WriteLine();
        Separator('┌', '┬', '┐');
        Row(headFuente, headLeidos, headInsertados, headRechazados);
        Separator('├', '┼', '┤');

        foreach (var source in sources)
        {
            Row(source.SourceName, source.Read.ToString(), source.Inserted.ToString(), source.Rejected.ToString());
        }

        Separator('├', '┼', '┤');
        Row("TOTAL", report.TotalRead.ToString(), report.TotalInserted.ToString(), report.TotalRejected.ToString());
        Separator('└', '┴', '┘');
    }

    private static void RenderRejectionDetail(LoadReport report)
    {
        var withRejections = report.Sources.Where(s => s.RejectionsByReason.Count > 0).ToList();
        if (withRejections.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Detalle de rechazos:");
        foreach (var source in withRejections)
        {
            foreach (var (reason, count) in source.RejectionsByReason)
            {
                Console.WriteLine($"  {source.SourceName} → {reason}: {count}");
            }
        }
    }

    private static void RenderFooter(LoadReport report)
    {
        Console.WriteLine();
        Console.WriteLine($"Duración total: {report.Elapsed:hh\\:mm\\:ss\\.fff}");
        Console.WriteLine();
    }

    private static int NumberWidth(int value) => value.ToString().Length;
}
