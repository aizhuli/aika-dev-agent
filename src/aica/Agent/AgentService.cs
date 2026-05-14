using System.Text.Json.Nodes;
using Aica.Tools;
using Anthropic.SDK;
using Anthropic.SDK.Common;
using Anthropic.SDK.Messaging;
using Spectre.Console;

namespace Aica.Agent;

public class AgentService
{
    private readonly AnthropicClient _client;
    private readonly AppConfig _config;
    private readonly ToolRegistry _registry;
    private readonly List<Message> _history = [];
    private readonly List<SystemMessage> _system;
    private readonly IList<Anthropic.SDK.Common.Tool> _tools;

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
                ShowToolCall(block.Name, block.Input);
                var result = await InvokeToolAsync(block.Name, block.Input);
                ShowToolResult(result);
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

    private async Task<ToolResult> InvokeToolAsync(string name, JsonNode? input)
    {
        var tool = _registry.Get(name);
        if (tool is null)
            return ToolResult.Error($"Unknown tool: {name}");
        try
        {
            return await tool.ExecuteAsync(input, _config);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Tool '{name}' threw an exception: {ex.Message}");
        }
    }

    private static void ShowToolCall(string name, JsonNode? input)
    {
        AnsiConsole.MarkupLine($"[dim]  ⚙ {Markup.Escape(name)}[/]");
    }

    private static void ShowToolResult(ToolResult result)
    {
        var (color, icon) = result.IsError ? ("red", "✗") : ("dim", "✓");
        var preview = result.Content.ReplaceLineEndings(" ");
        if (preview.Length > 120) preview = preview[..120] + "…";
        AnsiConsole.MarkupLine($"[{color}]  {icon} {Markup.Escape(preview)}[/]");
    }

    private static Anthropic.SDK.Common.Tool ToAnthropicTool(ITool tool) =>
        new(new Function(tool.Name, tool.Description, (JsonNode)tool.InputSchema));
}
