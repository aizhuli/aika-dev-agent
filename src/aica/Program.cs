using System.CommandLine;
using Aica;
using Aica.Repl;
using Spectre.Console;

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

return await rootCommand.InvokeAsync(args);
