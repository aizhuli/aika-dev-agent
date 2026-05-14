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
dotnet run --project src/aica -- --workdir ./myproject --model claude-opus-4-7 "prompt"

# Publish single-file binary
dotnet publish src/aica -r win-x64 --self-contained -p:PublishSingleFile=true -c Release
```

Required environment variable: `ANTHROPIC_API_KEY`

## Architecture

```
src/aica/
  Program.cs          — CLI entry point; System.CommandLine wiring
  AppConfig.cs        — Config loaded from env vars + CLI flags
  Repl/
    ReplLoop.cs       — Interactive REPL and single-prompt handler
  Tools/              — (Phase 2) ITool interface + tool implementations
  Agent/              — (Phase 3) Claude API client + agentic loop
```

### Key design decisions

- All file tool operations are sandboxed to `AppConfig.WorkingDirectory`; path traversal (`../`) is rejected at the tool level
- Destructive tools (`delete_file`, `execute_command`) require user confirmation unless `--yes` is passed
- The agent loop runs until Claude returns a response with no tool calls (standard tool-use loop pattern)
- Conversation history is kept in memory for the session; there is no persistence across runs

### Packages

| Package | Version | Purpose |
|---|---|---|
| `Spectre.Console` | 0.55.2 | Rich terminal output, markup rendering |
| `System.CommandLine` | 2.0.0-beta4 | CLI argument/option parsing |
| `Anthropic.SDK` | 5.10.0 | Claude API client with tool use support |
