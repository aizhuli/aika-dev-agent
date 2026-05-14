using System.CommandLine;
using Aica;
using Aica.Repl;
using Spectre.Console;

// Inject env.json into process environment before anything else reads it.
EnvFile.Apply();

// ── Options & arguments ───────────────────────────────────────────────────────

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

// ── Root command ──────────────────────────────────────────────────────────────

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

var configCommand = new Command("config", "Manage aica settings and environment variables");

// config show
var showCommand = new Command("show", "Show current configuration, env.json contents, and file paths");
showCommand.SetHandler(() =>
{
    var envVars  = EnvFile.Load();
    var settings = Settings.Load();

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Files[/]");
    AnsiConsole.MarkupLine($"  env.json      {Markup.Escape(EnvFile.FilePath)}");
    AnsiConsole.MarkupLine($"  settings.json {Markup.Escape(Settings.FilePath)}");

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]env.json[/]");
    if (envVars.Count == 0)
    {
        AnsiConsole.MarkupLine("  [dim](empty)[/]");
    }
    else
    {
        foreach (var (key, value) in envVars)
        {
            var display = key.Contains("KEY") || key.Contains("TOKEN") || key.Contains("SECRET")
                ? MaskValue(value)
                : value;
            AnsiConsole.MarkupLine($"  [bold]{Markup.Escape(key)}[/] = {Markup.Escape(display)}");
        }
    }

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]settings.json[/]");
    AnsiConsole.MarkupLine(settings.DefaultModel is { } m
        ? $"  default_model = [bold]{Markup.Escape(m)}[/]"
        : "  default_model = [dim](not set)[/]");

    AnsiConsole.WriteLine();
});

// config set <KEY> <value>
var setKeyArg   = new Argument<string>("key",   "Environment variable name (e.g. ANTHROPIC_API_KEY) or 'model'");
var setValueArg = new Argument<string>("value", "Value to store");
var setCommand  = new Command("set", "Set an environment variable in env.json, or set default model") { setKeyArg, setValueArg };

setCommand.SetHandler((key, value) =>
{
    if (key.Equals("model", StringComparison.OrdinalIgnoreCase))
    {
        var s = Settings.Load();
        s.DefaultModel = value;
        s.Save();
        AnsiConsole.MarkupLine($"[green]✓[/] default_model = [bold]{Markup.Escape(value)}[/]  ({Markup.Escape(Settings.FilePath)})");
        return;
    }

    var vars = EnvFile.Load();
    vars[key] = value;
    EnvFile.Save(vars);

    var display = key.Contains("KEY") || key.Contains("TOKEN") || key.Contains("SECRET")
        ? MaskValue(value)
        : value;
    AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(key)} = {Markup.Escape(display)}  ({Markup.Escape(EnvFile.FilePath)})");
}, setKeyArg, setValueArg);

// config unset <KEY>
var unsetKeyArg  = new Argument<string>("key", "Environment variable name or 'model'");
var unsetCommand = new Command("unset", "Remove an entry from env.json or clear default model") { unsetKeyArg };

unsetCommand.SetHandler((key) =>
{
    if (key.Equals("model", StringComparison.OrdinalIgnoreCase))
    {
        var s = Settings.Load();
        s.DefaultModel = null;
        s.Save();
        AnsiConsole.MarkupLine("[green]✓[/] default_model cleared");
        return;
    }

    var vars = EnvFile.Load();
    if (vars.Remove(key))
    {
        EnvFile.Save(vars);
        AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(key)} removed from env.json");
    }
    else
    {
        AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(key)} was not in env.json[/]");
    }
}, unsetKeyArg);

configCommand.AddCommand(showCommand);
configCommand.AddCommand(setCommand);
configCommand.AddCommand(unsetCommand);
rootCommand.AddCommand(configCommand);

// ── Helpers ───────────────────────────────────────────────────────────────────

static string MaskValue(string v) =>
    v.Length <= 8 ? "***" : $"{v[..8]}…";

return await rootCommand.InvokeAsync(args);
