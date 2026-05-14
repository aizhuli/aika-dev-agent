using Aica.Agent;
using Aica.Tools;
using Spectre.Console;

namespace Aica.Repl;

public class ReplLoop(AppConfig config)
{
    private AgentService? _agent;

    public async Task RunAsync(string? initialPrompt)
    {
        _agent = new AgentService(config, new ToolRegistry());
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
        AnsiConsole.WriteLine();
        try
        {
            string response = "";
            int inputTokens = 0, outputTokens = 0;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("green"))
                .StartAsync("Thinking…", async ctx =>
                {
                    (response, inputTokens, outputTokens) =
                        await _agent!.RunTurnAsync(input);
                });

            if (!string.IsNullOrWhiteSpace(response))
            {
                AnsiConsole.WriteLine();
                Console.WriteLine(response);
            }

            AnsiConsole.MarkupLine(
                $"\n[dim]tokens: in={inputTokens:N0} out={outputTokens:N0}[/]");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled.[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }

        AnsiConsole.WriteLine();
    }

    private void PrintWelcome()
    {
        AnsiConsole.Write(new FigletText("aica").Color(Color.Green));
        AnsiConsole.MarkupLine($"[grey]workdir[/]  [bold]{Markup.Escape(config.WorkingDirectory)}[/]");
        AnsiConsole.MarkupLine($"[grey]model  [/]  [bold]{config.Model}[/]");
        AnsiConsole.MarkupLine("[grey]Type [bold]quit[/] to exit.[/]\n");
    }
}
