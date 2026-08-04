using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Worker.Presentation;


internal static class ConsoleReportRenderer
{
    public static void Render(ExtractionReport report, string systemName, DateTime executedAt, bool cancelled = false)
    {
        RenderHeader(systemName, executedAt);
        RenderTable(report);
        RenderErrorDetail(report);
        RenderFooter(report, cancelled);
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

    private static void RenderTable(ExtractionReport report)
    {
        const string headFuente = "Fuente";
        const string headLeidos = "Leídos";
        const string headInsertados = "Insertados";
        const string headRechazados = "Rechazados";
        const string headTruncados = "Truncados";
        const string headDuracion = "Duración";

        var sources = report.Sources;

        var wFuente = Max(headFuente.Length, "TOTAL".Length, sources.Count == 0 ? 0 : sources.Max(s => s.SourceName.Length));
        var wLeidos = Math.Max(headLeidos.Length, Width(report.TotalRead));
        var wInsertados = Math.Max(headInsertados.Length, Width(report.TotalWritten));
        var wRechazados = Math.Max(headRechazados.Length, Width(report.TotalRejected));
        var wTruncados = Math.Max(headTruncados.Length, Width(report.TotalTruncated));
        var wDuracion = Math.Max(headDuracion.Length, "00:00:00.00".Length);

        void Separator(char left, char mid, char right) =>
            Console.WriteLine(
                left + new string('─', wFuente + 2) + mid + new string('─', wLeidos + 2)
                + mid + new string('─', wInsertados + 2) + mid + new string('─', wRechazados + 2)
                + mid + new string('─', wTruncados + 2) + mid + new string('─', wDuracion + 2) + right);

        void Row(string fuente, string leidos, string insertados, string rechazados, string truncados, string duracion) =>
            Console.WriteLine(
                $"│ {fuente.PadRight(wFuente)} │ {leidos.PadLeft(wLeidos)} │ {insertados.PadLeft(wInsertados)} │ "
                + $"{rechazados.PadLeft(wRechazados)} │ {truncados.PadLeft(wTruncados)} │ {duracion.PadLeft(wDuracion)} │");

        Console.WriteLine();
        Separator('┌', '┬', '┐');
        Row(headFuente, headLeidos, headInsertados, headRechazados, headTruncados, headDuracion);
        Separator('├', '┼', '┤');

        foreach (var source in sources)
        {
            Row(
                source.SourceName,
                source.Read.ToString(),
                source.Written.ToString(),
                source.Rejected.ToString(),
                source.Truncated.ToString(),
                $"{source.Elapsed:hh\\:mm\\:ss\\.ff}");
        }

        Separator('├', '┼', '┤');
        Row(
            "TOTAL",
            report.TotalRead.ToString(),
            report.TotalWritten.ToString(),
            report.TotalRejected.ToString(),
            report.TotalTruncated.ToString(),
            string.Empty);
        Separator('└', '┴', '┘');
    }

    private static void RenderErrorDetail(ExtractionReport report)
    {
        var withErrors = report.Sources.Where(s => s.ErrorsByCode.Count > 0).ToList();
        if (withErrors.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Detalle de rechazos:");
        foreach (var source in withErrors)
        {
            foreach (var (code, count) in source.ErrorsByCode)
            {
                Console.WriteLine($"  {source.SourceName} → {code}: {count}");
            }
        }
    }

    private static void RenderFooter(ExtractionReport report, bool cancelled)
    {
        var status = cancelled
            ? "CANCELADO"
            : report.AnySourceFailed ? "COMPLETADO CON FALLOS" : "COMPLETADO";

        Console.WriteLine();
        Console.WriteLine($"Duración total: {report.Elapsed:hh\\:mm\\:ss\\.fff}");
        Console.WriteLine($"Estado: {status}");
        Console.WriteLine();
    }

    private static int Max(int a, int b, int c) => Math.Max(a, Math.Max(b, c));

    private static int Width(long value) => value.ToString().Length;
}
