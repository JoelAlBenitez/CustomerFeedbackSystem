using CustomerFeedbackSystem.OLAP.Core.Reporting;

namespace CustomerFeedbackSystem.OLAP.Worker.Presentation;

internal static class ConsoleFactReportRenderer
{
    public static void Render(FactLoadReport report, string systemName, DateTime executedAt)
    {
        RenderHeader(systemName, executedAt);
        RenderReset(report);
        RenderTables(report);
        RenderChannels(report);
        RenderResolution(report);
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

    private static void RenderReset(FactLoadReport report)
    {
        Console.WriteLine();

        Console.WriteLine(report.Reset.Cleared
            ? $"Refresco completo: se eliminaron {report.Reset.Opiniones} opinión(es) y "
              + $"{report.Reset.Palabras} vínculo(s) previos antes de insertar."
            : "Refresco completo: las tablas de hechos estaban vacías; se insertó directamente.");
    }

    private static void RenderTables(FactLoadReport report)
    {
        const string headTabla = "Tabla de hechos";
        const string headLeidos = "Leídos";
        const string headInsertados = "Insertados";
        const string headDescartados = "Descartados";

        var tables = report.Tables;

        var wTabla = Math.Max(
            Math.Max(headTabla.Length, "TOTAL".Length),
            tables.Count == 0 ? 0 : tables.Max(t => t.FactName.Length));
        var wLeidos = Math.Max(headLeidos.Length, Width(report.TotalRead));
        var wInsertados = Math.Max(headInsertados.Length, Width(report.TotalWritten));
        var wDescartados = Math.Max(headDescartados.Length, Width(report.TotalDiscarded));

        void Separator(char left, char mid, char right) =>
            Console.WriteLine(
                left + new string('─', wTabla + 2) + mid + new string('─', wLeidos + 2)
                + mid + new string('─', wInsertados + 2) + mid + new string('─', wDescartados + 2) + right);

        void Row(string tabla, string leidos, string insertados, string descartados) =>
            Console.WriteLine(
                $"│ {tabla.PadRight(wTabla)} │ {leidos.PadLeft(wLeidos)} │ "
                + $"{insertados.PadLeft(wInsertados)} │ {descartados.PadLeft(wDescartados)} │");

        Console.WriteLine();
        Separator('┌', '┬', '┐');
        Row(headTabla, headLeidos, headInsertados, headDescartados);
        Separator('├', '┼', '┤');

        foreach (var table in tables)
        {
            Row(
                table.FactName,
                table.Read.ToString(),
                table.Written.ToString(),
                table.Discarded.ToString());
        }

        Separator('├', '┼', '┤');
        Row(
            "TOTAL",
            report.TotalRead.ToString(),
            report.TotalWritten.ToString(),
            report.TotalDiscarded.ToString());
        Separator('└', '┴', '┘');
    }

    private static void RenderChannels(FactLoadReport report)
    {
        if (report.Channels.Count == 0)
        {
            return;
        }

        const string headCanal = "Canal";
        const string headTotal = "Total";
        const string headPositivas = "Positivas";
        const string headNeutras = "Neutras";
        const string headNegativas = "Negativas";
        const string headSinClasificar = "Sin clasif.";
        const string headConPuntaje = "Con puntaje";

        var wCanal = Math.Max(
            Math.Max(headCanal.Length, "TOTAL".Length),
            report.Channels.Max(c => c.Canal.Length));

        void Separator(char left, char mid, char right) =>
            Console.WriteLine(
                left + new string('─', wCanal + 2) + mid + new string('─', headTotal.Length + 2)
                + mid + new string('─', headPositivas.Length + 2) + mid + new string('─', headNeutras.Length + 2)
                + mid + new string('─', headNegativas.Length + 2) + mid + new string('─', headSinClasificar.Length + 2)
                + mid + new string('─', headConPuntaje.Length + 2) + right);

        void Row(string canal, string total, string positivas, string neutras,
            string negativas, string sinClasificar, string conPuntaje) =>
            Console.WriteLine(
                $"│ {canal.PadRight(wCanal)} │ {total.PadLeft(headTotal.Length)} │ "
                + $"{positivas.PadLeft(headPositivas.Length)} │ {neutras.PadLeft(headNeutras.Length)} │ "
                + $"{negativas.PadLeft(headNegativas.Length)} │ {sinClasificar.PadLeft(headSinClasificar.Length)} │ "
                + $"{conPuntaje.PadLeft(headConPuntaje.Length)} │");

        Console.WriteLine();
        Console.WriteLine("Clasificación de opiniones por canal:");
        Separator('┌', '┬', '┐');
        Row(headCanal, headTotal, headPositivas, headNeutras, headNegativas, headSinClasificar, headConPuntaje);
        Separator('├', '┼', '┤');

        foreach (var channel in report.Channels)
        {
            Row(
                channel.Canal,
                channel.Total.ToString(),
                channel.Positivas.ToString(),
                channel.Neutras.ToString(),
                channel.Negativas.ToString(),
                channel.SinClasificar.ToString(),
                channel.ConPuntaje.ToString());
        }

        Separator('├', '┼', '┤');
        Row(
            "TOTAL",
            report.Channels.Sum(c => c.Total).ToString(),
            report.Channels.Sum(c => c.Positivas).ToString(),
            report.Channels.Sum(c => c.Neutras).ToString(),
            report.Channels.Sum(c => c.Negativas).ToString(),
            report.Channels.Sum(c => c.SinClasificar).ToString(),
            report.Channels.Sum(c => c.ConPuntaje).ToString());
        Separator('└', '┴', '┘');
    }

    private static void RenderResolution(FactLoadReport report)
    {
        var resolution = report.Resolution;

        Console.WriteLine();
        Console.WriteLine("Resolución de claves surrogate:");
        Console.WriteLine(
            $"  Clientes    → {resolution.ClientesResueltos} resueltos, "
            + $"{resolution.ClientesDesconocidos} a 'Desconocido', {resolution.ClientesAnonimos} a 'Anónimo'");
        Console.WriteLine(
            $"  Productos   → {resolution.ProductosResueltos} resueltos, "
            + $"{resolution.ProductosDesconocidos} a 'Desconocido', {resolution.ProductosSinOrigen} sin origen");
        Console.WriteLine(
            $"  Fechas      → {resolution.FechasResueltas} resueltas, "
            + $"{resolution.FechasSinResolver} al centinela 19000101");
        Console.WriteLine(
            $"  Puntajes    → {resolution.PuntajesValidos} válidos (1-5), "
            + $"{resolution.PuntajesAusentes} ausentes o fuera de rango");

        if (report.Agreement.Comparadas > 0)
        {
            Console.WriteLine(
                $"  Clasificador→ {report.Agreement.Coincidencias} de {report.Agreement.Comparadas} coinciden "
                + $"con la etiqueta de origen ({report.Agreement.Porcentaje:F2} %)");
        }
    }

    private static void RenderFooter(FactLoadReport report)
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
