using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Worker.Presentation;

internal static class ConsoleDimensionReportRenderer
{
    public static void Render(DimensionLoadReport report, string systemName, DateTime executedAt)
    {
        RenderHeader(systemName, executedAt);
        RenderTable(report);
        RenderNotes(report);
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

    private static void RenderTable(DimensionLoadReport report)
    {
        const string headDimension = "Dimensión";
        const string headLeidos = "Leídos";
        const string headInsertados = "Insertados";
        const string headDescartados = "Descartados";
        const string headDuracion = "Duración";

        var dimensions = report.Dimensions;

        var wDimension = Math.Max(
            Math.Max(headDimension.Length, "TOTAL".Length),
            dimensions.Count == 0 ? 0 : dimensions.Max(d => d.DimensionName.Length));
        var wLeidos = Math.Max(headLeidos.Length, Width(report.TotalRead));
        var wInsertados = Math.Max(headInsertados.Length, Width(report.TotalWritten));
        var wDescartados = Math.Max(headDescartados.Length, Width(report.TotalDiscarded));
        var wDuracion = Math.Max(headDuracion.Length, "00:00:00.00".Length);

        void Separator(char left, char mid, char right) =>
            Console.WriteLine(
                left + new string('─', wDimension + 2) + mid + new string('─', wLeidos + 2)
                + mid + new string('─', wInsertados + 2) + mid + new string('─', wDescartados + 2)
                + mid + new string('─', wDuracion + 2) + right);

        void Row(string dimension, string leidos, string insertados, string descartados, string duracion) =>
            Console.WriteLine(
                $"│ {dimension.PadRight(wDimension)} │ {leidos.PadLeft(wLeidos)} │ {insertados.PadLeft(wInsertados)} │ "
                + $"{descartados.PadLeft(wDescartados)} │ {duracion.PadLeft(wDuracion)} │");

        Console.WriteLine();
        Separator('┌', '┬', '┐');
        Row(headDimension, headLeidos, headInsertados, headDescartados, headDuracion);
        Separator('├', '┼', '┤');

        foreach (var dimension in dimensions)
        {
            Row(
                dimension.DimensionName,
                dimension.Read.ToString(),
                dimension.Written.ToString(),
                dimension.Discarded.ToString(),
                $"{dimension.Elapsed:hh\\:mm\\:ss\\.ff}");
        }

        Separator('├', '┼', '┤');
        Row(
            "TOTAL",
            report.TotalRead.ToString(),
            report.TotalWritten.ToString(),
            report.TotalDiscarded.ToString(),
            string.Empty);
        Separator('└', '┴', '┘');
    }

    private static void RenderNotes(DimensionLoadReport report)
    {
        var withNotes = report.Dimensions.Where(d => !string.IsNullOrWhiteSpace(d.Note)).ToList();
        if (withNotes.Count == 0)
        {
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Detalle por dimensión:");
        foreach (var dimension in withNotes)
        {
            Console.WriteLine($"  {dimension.DimensionName} → {dimension.Note}");
        }
    }

    private static void RenderFooter(DimensionLoadReport report)
    {
        Console.WriteLine();

        if (!report.Committed)
        {
            Console.WriteLine("Estado: REVERTIDO — no se escribió nada en el Data Warehouse");

            if (report.FailureReason is not null)
            {
                Console.WriteLine($"Motivo: {report.FailureReason}");
            }
        }
        else
        {
            Console.WriteLine($"Duración total: {report.Elapsed:hh\\:mm\\:ss\\.fff}");
            Console.WriteLine("Estado: COMPLETADO — transacción confirmada");
        }

        Console.WriteLine();
    }

    private static int Width(long value) => value.ToString().Length;
}
