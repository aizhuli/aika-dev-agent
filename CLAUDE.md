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

# Config subcommand
dotnet run --project src/aica -- config show
dotnet run --project src/aica -- config set ANTHROPIC_API_KEY sk-ant-...
dotnet run --project src/aica -- config set model claude-opus-4-7
dotnet run --project src/aica -- config unset model

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
  Program.cs              — CLI entry point; System.CommandLine wiring (root + config commands)
  AppConfig.cs            — Config resolved from env vars + CLI flags + settings.json
  EnvFile.cs              — Loads/saves ~/.aica/env.json; Apply() injects into process env
  Settings.cs             — Loads/saves ~/.aica/settings.json (default_model)
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
    Impl/                 — One file per tool (8 total):
      ReadFileTool.cs       read_file
      WriteFileTool.cs      write_file
      EditFileTool.cs       edit_file
      DeleteFileTool.cs     delete_file
      ListDirectoryTool.cs  list_directory
      SearchFilesTool.cs    search_files
      GrepSearchTool.cs     grep_search
      ExecuteCommandTool.cs execute_command

tests/aica.Tests/
  Tools/
    ToolTestBase.cs       — IDisposable base with TempDir + AppConfig + helpers
    *ToolTests.cs         — xUnit integration tests per tool (33 tests total)
```

### User data directory

All persistent user configuration lives in `~/.aica/`:

| File | Purpose |
|---|---|
| `env.json` | Key/value store for env vars (e.g. `ANTHROPIC_API_KEY`). Applied at startup. |
| `settings.json` | App settings (currently only `default_model`). |

`EnvFile.Apply()` never overwrites env vars already set in the shell — shell always wins.

### Model resolution order

1. `--model` CLI flag (if different from the hardcoded default `claude-sonnet-4-6`)
2. `settings.json` → `default_model`
3. Hardcoded fallback: `claude-sonnet-4-6`

### Agent loop (AgentService)

1. Append user message to `_history`
2. POST to Claude with all tool schemas
3. If `StopReason == "tool_use"`: execute each tool, append results, repeat from 2
4. Otherwise return the text response and accumulated token counts

### Tool permission model

- `delete_file` and `execute_command` pause and ask `Proceed? [y/N]` before running
- `--yes` flag skips all confirmation prompts
- All file tools resolve paths through `PathSandbox.Resolve()` which rejects `../` traversal

### REPL built-in commands

| Command | Effect |
|---|---|
| `/clear` | Resets conversation history (creates a fresh `AgentService`) |
| `/help` | Shows command reference table |
| `quit` / `exit` / `q` | Exits the REPL |
| `Ctrl+C` | Cancels the in-flight API request without exiting |

### Packages

| Package | Version | Purpose |
|---|---|---|
| `Anthropic.SDK` | 5.10.0 | Claude API client with tool use support |
| `Spectre.Console` | 0.55.2 | Rich terminal output, markup rendering |
| `System.CommandLine` | 2.0.0-beta4 | CLI argument/option parsing |
| `Microsoft.Extensions.FileSystemGlobbing` | 10.0.8 | Glob pattern matching for search_files |

## Conventions (solo project)

- **One file per tool** in `Tools/Impl/` — keep it that way; no mega-files.
- **No DI container** — dependencies are wired manually in `ReplLoop` and `Program.cs`.
- **Tests are integration tests** — they use real temp directories, not mocks. Keep it that way.
- **No comments** unless the WHY is non-obvious (e.g. the EnvFile.Apply no-overwrite invariant).
- Add new tools by: implementing `ITool`, registering in `ToolRegistry`, adding a test file.
