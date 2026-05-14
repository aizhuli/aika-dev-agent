using System.CommandLine;
using Aica;
using Aica.Repl;
using Spectre.Console;

// ── Options & arguments ──────────────────────────────────────────────────────

var workdirOption = new Option<DirectoryInfo?>(
    "--workdir",
    "Working directory for the agent (defaults to current directory)");

var modelOption = new Option<string>(
    "--model",
    getDefaultValue: () => "claude-sonnet-4-6",
    "Claude model to use");

var yesOption = new Option<bool>(
    "--yes",
    "Auto-approve all confirmation prompts");

var promptArgument = new Argument<string?>(
    "prompt",
    "Optional prompt. If omitted, starts an interactive REPL session.")
{
    Arity = ArgumentArity.ZeroOrOne
};

// ── Root command ─────────────────────────────────────────────────────────────

var rootCommand = new RootCommand("aica — AI Coding Agent")
{
    workdirOption,
    modelOption,
    yesOption,
    promptArgument
};

rootCommand.SetHandler(async (workdir, model, yes, prompt) =>
{
    try
    {
        var config = AppConfig.Load(workdir, model, yes);
        var repl = new ReplLoop(config);
        await repl.RunAsync(prompt);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        Environment.Exit(1);
    }
}, workdirOption, modelOption, yesOption, promptArgument);

// ── config command ────────────────────────────────────────────────────────────

var configCommand = new Command("config", "Manage aica settings");

// config show
var showCommand = new Command("show", "Show current configuration and settings file path");
showCommand.SetHandler(() =>
{
    var settings = Settings.Load();
    var apiKeyDisplay = string.IsNullOrWhiteSpace(settings.ApiKey)
        ? "[dim]not set[/]"
        : $"[green]set[/] [dim]({MaskKey(settings.ApiKey)})[/]";
    var envKeyDisplay = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") is { } k && k.Length > 0
        ? $"[green]set[/] [dim]({MaskKey(k)})[/]"
        : "[dim]not set[/]";

    var table = new Table().NoBorder().HideHeaders();
    table.AddColumn("key");
    table.AddColumn("value");
    table.AddRow("Settings file",  $"[bold]{Markup.Escape(Settings.SettingsPath)}[/]");
    table.AddRow("api_key (file)", apiKeyDisplay);
    table.AddRow("api_key (env)",  envKeyDisplay);
    table.AddRow("default_model",  settings.DefaultModel is { } m ? $"[bold]{m}[/]" : "[dim]not set[/]");

    AnsiConsole.WriteLine();
    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
});

// config set <key> <value>
var setKeyArg   = new Argument<string>("key",   "Setting name: api-key | model");
var setValueArg = new Argument<string>("value", "Value to store");
var setCommand  = new Command("set", "Set a configuration value") { setKeyArg, setValueArg };
setCommand.SetHandler((key, value) =>
{
    var settings = Settings.Load();
    switch (key.ToLower())
    {
        case "api-key":
        case "api_key":
            settings.ApiKey = value;
            settings.Save();
            AnsiConsole.MarkupLine($"[green]✓[/] api_key saved to {Markup.Escape(Settings.SettingsPath)}");
            break;
        case "model":
        case "default-model":
        case "default_model":
            settings.DefaultModel = value;
            settings.Save();
            AnsiConsole.MarkupLine($"[green]✓[/] default_model set to [bold]{Markup.Escape(value)}[/]");
            break;
        default:
            AnsiConsole.MarkupLine($"[red]Unknown key:[/] {Markup.Escape(key)}. Valid keys: api-key, model");
            Environment.Exit(1);
            break;
    }
}, setKeyArg, setValueArg);

// config unset <key>
var unsetKeyArg = new Argument<string>("key", "Setting name: api-key | model");
var unsetCommand = new Command("unset", "Remove a configuration value") { unsetKeyArg };
unsetCommand.SetHandler((key) =>
{
    var settings = Settings.Load();
    switch (key.ToLower())
    {
        case "api-key":
        case "api_key":
            settings.ApiKey = null;
            settings.Save();
            AnsiConsole.MarkupLine("[green]✓[/] api_key removed");
            break;
        case "model":
        case "default-model":
        case "default_model":
            settings.DefaultModel = null;
            settings.Save();
            AnsiConsole.MarkupLine("[green]✓[/] default_model removed");
            break;
        default:
            AnsiConsole.MarkupLine($"[red]Unknown key:[/] {Markup.Escape(key)}");
            Environment.Exit(1);
            break;
    }
}, unsetKeyArg);

configCommand.AddCommand(showCommand);
configCommand.AddCommand(setCommand);
configCommand.AddCommand(unsetCommand);
rootCommand.AddCommand(configCommand);

// ── Helpers ──────────────────────────────────────────────────────────────────

static string MaskKey(string key) =>
    key.Length <= 8 ? "***" : $"{key[..8]}…";

return await rootCommand.InvokeAsync(args);
