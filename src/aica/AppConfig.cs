namespace Aica;

public class AppConfig
{
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
    public string Model { get; init; } = "claude-sonnet-4-6";
    public string ApiKey { get; init; } = string.Empty;
    public bool AutoApprove { get; init; }

    public static AppConfig Load(DirectoryInfo? workdir, string model, bool autoApprove)
    {
        var settings = Settings.Load();

        // Priority: env var > settings file
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")
                     ?? settings.ApiKey
                     ?? string.Empty;

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                $"No API key found. Set ANTHROPIC_API_KEY or run: aica config set api-key <key>\n" +
                $"Settings file: {Settings.SettingsPath}");

        // Priority: CLI flag > settings file > default
        var resolvedModel = model != "claude-sonnet-4-6"
            ? model
            : settings.DefaultModel ?? model;

        var resolvedWorkdir = workdir?.FullName ?? Directory.GetCurrentDirectory();

        if (!Directory.Exists(resolvedWorkdir))
            throw new DirectoryNotFoundException(
                $"Working directory does not exist: {resolvedWorkdir}");

        return new AppConfig
        {
            WorkingDirectory = resolvedWorkdir,
            Model = resolvedModel,
            ApiKey = apiKey,
            AutoApprove = autoApprove
        };
    }
}
