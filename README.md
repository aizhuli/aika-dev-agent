# aika — AI Coding Agent

A command-line software development agent powered by Claude. It reads, writes, edits, searches, and executes commands in your project — guided by natural language.

## Features

- **Interactive REPL** — multi-turn conversation with persistent context
- **Single-prompt mode** — pipe a task directly and get a result
- **8 built-in tools** — file read/write/edit/delete, directory listing, glob search, regex grep, shell execution
- **Safety prompts** — destructive operations (`delete_file`, `execute_command`) require confirmation before running
- **Sandboxed** — all file operations are restricted to the working directory; path traversal is rejected
- **Ctrl+C cancellation** — abort the current request without killing the process
- **Token tracking** — input/output token counts shown after every turn

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- An [Anthropic API key](https://console.anthropic.com/)

## Installation

```bash
git clone https://github.com/aizhuli/aika-dev-agent.git
cd aika-dev-agent
```

Set your API key:

```bash
# Windows (PowerShell)
$env:ANTHROPIC_API_KEY = "sk-ant-..."

# macOS / Linux
export ANTHROPIC_API_KEY="sk-ant-..."
```

## Usage

### Interactive REPL

```bash
dotnet run --project src/aica
```

```
   __ _  (_)   ___    __ _
  / _` | | |  / __|  / _` |
 | (_| | | | | (__  | (_| |
  \__,_| |_|  \___|  \__,_|

workdir  /home/user/myproject
model    claude-sonnet-4-6
Type /help for commands or quit to exit.

> explain the architecture of this codebase
> add a --verbose flag to the CLI
> run the tests and fix any failures
```

### Single prompt

```bash
dotnet run --project src/aica -- "list all TODO comments in the codebase"
```

### Options

| Flag | Default | Description |
|---|---|---|
| `--workdir <path>` | current directory | Directory the agent operates in |
| `--model <model>` | `claude-sonnet-4-6` | Claude model to use |
| `--yes` | false | Auto-approve all confirmation prompts |

### REPL commands

| Command | Description |
|---|---|
| `/clear` | Reset conversation history |
| `/help` | Show command reference |
| `quit` / `exit` | Exit the agent |
| `Ctrl+C` | Cancel the current request |

## Tools

The agent has access to these tools:

| Tool | Description |
|---|---|
| `read_file` | Read file contents with line numbers; supports `offset` and `limit` |
| `write_file` | Create or fully overwrite a file |
| `edit_file` | Exact string find-and-replace (fails if match is ambiguous) |
| `delete_file` | Delete a file or empty directory ⚠ requires confirmation |
| `list_directory` | List files and directories at a path |
| `search_files` | Find files by glob pattern (e.g. `**/*.cs`) |
| `grep_search` | Search file contents by regex with optional glob filter |
| `execute_command` | Run a shell command, capture stdout/stderr/exit code ⚠ requires confirmation |

## Publish a self-contained binary

```bash
# Windows x64
dotnet publish src/aica -r win-x64 --self-contained -p:PublishSingleFile=true -c Release -o publish/

# macOS arm64
dotnet publish src/aica -r osx-arm64 --self-contained -p:PublishSingleFile=true -c Release -o publish/

# Linux x64
dotnet publish src/aica -r linux-x64 --self-contained -p:PublishSingleFile=true -c Release -o publish/
```

## Development

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run tests for a specific tool
dotnet test --filter "FullyQualifiedName~EditFileToolTests"
```

## Architecture

```
src/aica/
  Program.cs                CLI entry point (System.CommandLine)
  AppConfig.cs              Config from env vars + CLI flags
  Agent/
    AgentService.cs         Agentic loop: sends messages, executes tools, repeats until done
    SystemPromptBuilder.cs  System prompt with injected workdir/OS/shell/date
  Repl/
    ReplLoop.cs             Interactive REPL and single-prompt runner
  Tools/
    ITool.cs                Interface: Name, Description, InputSchema, ExecuteAsync
    ToolResult.cs           Ok(content) / Error(message) return type
    PathSandbox.cs          Rejects paths outside WorkingDirectory
    ToolRegistry.cs         Tool lookup map
    Impl/                   One file per tool

tests/aica.Tests/
  Tools/                    xUnit integration tests (33 tests)
```

### How the agent loop works

1. User message is appended to conversation history
2. History + tool schemas are sent to Claude
3. If Claude returns `tool_use` blocks, each tool is executed and results are sent back
4. Steps 2–3 repeat until Claude returns a plain text response
5. Response and token counts are shown to the user

Conversation history persists across turns within a session. Use `/clear` to reset it.

## License

MIT
