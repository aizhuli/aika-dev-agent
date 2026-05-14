using Spectre.Console;

namespace Aica.Repl;

public class ReplLoop(AppConfig config)
{
    public async Task RunAsync(string? initialPrompt)
    {
        PrintWelcome();

        if (initialPrompt is not null)
        {
            await HandleInputAsync(initialPrompt);
            return;
        }

        while (true)
        {
            AnsiConsole.Markup("[bold green]>[/] ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                continue;

            if (input is "quit" or "exit" or "q")
            {
                AnsiConsole.MarkupLine("[grey]Goodbye.[/]");
                break;
            }

            await HandleInputAsync(input);
        }
    }

    private async Task HandleInputAsync(string input)
    {
        // Phase 3: connect to Claude agent loop
        AnsiConsole.MarkupLine($"\n[grey][[Agent not yet connected — coming in Phase 3]][/]\n");
        await Task.CompletedTask;
    }

    private void PrintWelcome()
    {
        AnsiConsole.Write(new FigletText("aica").Color(Color.Green));
        AnsiConsole.MarkupLine($"[grey]workdir[/]  [bold]{Markup.Escape(config.WorkingDirectory)}[/]");
        AnsiConsole.MarkupLine($"[grey]model  [/]  [bold]{config.Model}[/]");
        AnsiConsole.MarkupLine("[grey]Type [bold]quit[/] to exit.[/]\n");
    }
}
