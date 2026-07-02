namespace CustomerFeedbackSystem.Load.Configuration;

/// <summary>
/// Locates the "CSV opiniones de clientes" folder by walking up from the running
/// executable's directory, since it lives outside the solution folder and its
/// depth relative to bin/Debug/net9.0 is not something we want to hardcode.
/// </summary>
public static class CsvDirectoryResolver
{
    private const string CsvDirectoryName = "CSV opiniones de clientes";

    public static string Resolve(string? configuredBaseDirectory)
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

        throw new DirectoryNotFoundException(
            $"Could not locate the '{CsvDirectoryName}' folder starting from '{AppContext.BaseDirectory}'. " +
            "Set CsvSources:BaseDirectory explicitly in appsettings.json.");
    }
}
