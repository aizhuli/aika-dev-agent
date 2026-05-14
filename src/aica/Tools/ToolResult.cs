namespace Aica.Tools;

public record ToolResult(string Content, bool IsError = false)
{
    public static ToolResult Ok(string content) => new(content);
    public static ToolResult Error(string message) => new(message, IsError: true);
}
