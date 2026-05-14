namespace Aica;

public class AppConfig
{
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
    public string Model { get; init; } = "claude-sonnet-4-6";
    public string ApiKey { get; init; } = string.Empty;
    public bool AutoApprove { get; init; }

    public static AppConfig Load(DirectoryInfo? workdir, string model, bool autoApprove)
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "ANTHROPIC_API_KEY environment variable is not set.");

        var resolvedWorkdir = workdir?.FullName ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(resolvedWorkdir))
            throw new DirectoryNotFoundException(
                $"Working directory does not exist: {resolvedWorkdir}");

        return new AppConfig
        {
            WorkingDirectory = resolvedWorkdir,
            Model = model,
            ApiKey = apiKey,
            AutoApprove = autoApprove
        };
    }
}
