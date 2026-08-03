namespace CustomerFeedbackSystem.OLAP.Infrastructure.Configuration;
public static class CsvDirectoryResolver
{
    private const string CsvDirectoryName = "CSV opiniones de clientes";

  
    public static string? TryResolve(string? configuredBaseDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuredBaseDirectory))
        {
            return configuredBaseDirectory;
        }

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, CsvDirectoryName);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
