using System.Text.Json.Nodes;
using Aica.Tools;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using Spectre.Console;

namespace Aica.Agent;

public class AgentService
{
    private static readonly HashSet<string> DestructiveTools = ["delete_file", "execute_command"];

    private readonly AnthropicClient _client;
    private readonly AppConfig _config;
    private readonly ToolRegistry _registry;
    private readonly List<Message> _history = [];
    private readonly List<SystemMessage> _system;
    private readonly IList<Anthropic.SDK.Common.Tool> _tools;

    public int TurnCount => _history.Count(m => m.Role == RoleType.User);

    public AgentService(AppConfig config, ToolRegistry registry)
    {
        _client = new AnthropicClient(config.ApiKey);
        _config = config;
        _registry = registry;
        _system = [new SystemMessage(SystemPromptBuilder.Build(config))];
        _tools = registry.All.Select(ToAnthropicTool).ToList();
    }

    public async Task<(string Response, int InputTokens, int OutputTokens)> RunTurnAsync(
        string userMessage, CancellationToken ct = default)
    {
        _history.Add(new Message
        {
            Role = RoleType.User,
            Content = [new TextContent { Text = userMessage }]
        });

        var totalInput = 0;
        var totalOutput = 0;

        while (true)
        {
            var response = await _client.Messages.GetClaudeMessageAsync(new MessageParameters
            {
                Model = _config.Model,
                MaxTokens = 8096,
                System = _system,
                Messages = _history,
                Tools = _tools
            }, ct);

            totalInput += response.Usage?.InputTokens ?? 0;
            totalOutput += response.Usage?.OutputTokens ?? 0;

            _history.Add(new Message
            {
                Role = RoleType.Assistant,
                Content = response.Content
            });

            if (response.StopReason != "tool_use")
            {
                var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text ?? "";
                return (text, totalInput, totalOutput);
            }

            var toolResults = new List<ContentBase>();
            foreach (var block in response.Content.OfType<ToolUseContent>())
            {
                ct.ThrowIfCancellationRequested();

                ShowToolCall(block.Name, block.Input);

                ToolResult result;
                if (!await ConfirmAsync(block.Name, block.Input))
                {
                    result = ToolResult.Error("User declined to run this tool.");
                    AnsiConsole.MarkupLine("[yellow]  ↩ skipped[/]");
                }
                else
                {
                    result = await InvokeToolAsync(block.Name, block.Input, ct);
                    ShowToolResult(result);
                }

                toolResults.Add(new ToolResultContent
                {
                    ToolUseId = block.Id,
                    Content = [new TextContent { Text = result.Content }],
                    IsError = result.IsError
                });
            }

            _history.Add(new Message
            {
                Role = RoleType.User,
                Content = toolResults
            });
        }
    }

    private Task<bool> ConfirmAsync(string toolName, JsonNode? input)
    {
        if (_config.AutoApprove || !DestructiveTools.Contains(toolName))
            return Task.FromResult(true);

        var detail = toolName switch
        {
            "delete_file"     => input?["path"]?.GetValue<string>() ?? "",
            "execute_command" => input?["command"]?.GetValue<string>() ?? "",
            _                 => ""
        };

        AnsiConsole.MarkupLine($"[yellow]  ⚠ {Markup.Escape(detail)}[/]");
        return Task.FromResult(AnsiConsole.Confirm("  Proceed?", defaultValue: false));
    }

    private async Task<ToolResult> InvokeToolAsync(string name, JsonNode? input, CancellationToken ct)
    {
        var tool = _registry.Get(name);
        if (tool is null)
            return ToolResult.Error($"Unknown tool: {name}");
        try
        {
            return await tool.ExecuteAsync(input, _config);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Tool '{name}' threw an exception: {ex.Message}");
        }
    }

    private static void ShowToolCall(string name, JsonNode? input)
    {
        var detail = name switch
        {
            "read_file"       => input?["path"]?.GetValue<string>(),
            "write_file"      => input?["path"]?.GetValue<string>(),
            "edit_file"       => input?["path"]?.GetValue<string>(),
            "delete_file"     => input?["path"]?.GetValue<string>(),
            "list_directory"  => input?["path"]?.GetValue<string>(),
            "search_files"    => input?["pattern"]?.GetValue<string>(),
            "grep_search"     => input?["pattern"]?.GetValue<string>(),
            "execute_command" => input?["command"]?.GetValue<string>(),
            _                 => null
        };

        var suffix = detail is not null ? $" [dim]{Markup.Escape(detail)}[/]" : "";
        AnsiConsole.MarkupLine($"[blue]  ⚙[/] [bold]{Markup.Escape(name)}[/]{suffix}");
    }

    private static void ShowToolResult(ToolResult result)
    {
        var (color, icon) = result.IsError ? ("red", "✗") : ("green", "✓");
        var preview = result.Content.ReplaceLineEndings(" ").Trim();
        if (preview.Length > 120) preview = preview[..120] + "…";
        AnsiConsole.MarkupLine($"[{color}]  {icon}[/] [dim]{Markup.Escape(preview)}[/]");
    }

    private static Anthropic.SDK.Common.Tool ToAnthropicTool(ITool tool) =>
        new(new Function(tool.Name, tool.Description, (JsonNode)tool.InputSchema));
}
