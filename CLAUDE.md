# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build src/aica/aica.csproj

# Run (REPL mode)
dotnet run --project src/aica

# Run (single prompt)
dotnet run --project src/aica -- "your prompt here"

# Run with options
dotnet run --project src/aica -- --workdir ./myproject --model claude-opus-4-7 --yes "prompt"

# Test (all)
dotnet test

# Test (single class)
dotnet test --filter "FullyQualifiedName~ReadFileToolTests"

# Publish single-file binary (Windows x64)
dotnet publish src/aica -r win-x64 --self-contained -p:PublishSingleFile=true -c Release -o publish/
```

Required environment variable: `ANTHROPIC_API_KEY`

## Architecture

```
src/aica/
  Program.cs              — CLI entry point; System.CommandLine wiring
  AppConfig.cs            — Config loaded from env vars + CLI flags
  Agent/
    AgentService.cs       — Agentic loop: sends messages, handles tool_use, returns text
    SystemPromptBuilder.cs — Injects workdir/OS/shell/date into the system prompt
  Repl/
    ReplLoop.cs           — Interactive REPL and single-prompt handler
  Tools/
    ITool.cs              — Interface: Name, Description, InputSchema, ExecuteAsync
    ToolResult.cs         — Ok(content) / Error(message) return type
    PathSandbox.cs        — Rejects paths outside WorkingDirectory
    ToolRegistry.cs       — Lookup map keyed by tool name
    Impl/                 — One file per tool (8 total)

tests/aica.Tests/
  Tools/
    ToolTestBase.cs       — IDisposable base with TempDir + AppConfig + helpers
    *ToolTests.cs         — xUnit integration tests per tool (33 tests total)
```

### Agent loop (AgentService)

1. Append user message to `_history`
2. POST to Claude with all tool schemas
3. If `StopReason == "tool_use"`: execute each tool, append results, repeat from 2
4. Otherwise return the text response and accumulated token counts

### Tool permission model

- `delete_file` and `execute_command` pause and ask `Proceed? [y/N]` before running
- `--yes` flag skips all confirmation prompts
- All file tools resolve paths through `PathSandbox.Resolve()` which rejects `../` traversal

### Packages

| Package | Version | Purpose |
|---|---|---|
| `Spectre.Console` | 0.55.2 | Rich terminal output, markup rendering |
| `System.CommandLine` | 2.0.0-beta4 | CLI argument/option parsing |
| `Anthropic.SDK` | 5.10.0 | Claude API client with tool use support |
| `Microsoft.Extensions.FileSystemGlobbing` | 10.0.8 | Glob pattern matching for search_files |
