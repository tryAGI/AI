# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AI CLI (`tryAGI.AI`) is a .NET console tool that uses AI models with MCP (Model Context Protocol) servers to perform tasks like text summarization, release note generation, changelog creation, code documentation, and general agent-driven actions. Distributed as a .NET global tool via NuGet. Supports multiple LLM providers (OpenAI, Anthropic, OpenRouter) and a variety of MCP tool integrations (filesystem, GitHub, git, Puppeteer, Slack, Figma, etc.).

## Build Commands

```bash
# Build entire solution
dotnet build AI.slnx

# Build just the CLI
dotnet build src/AI.Cli/AI.Cli.csproj

# Run integration tests (requires API key env vars)
dotnet test src/AI.Cli.IntegrationTests/AI.Cli.IntegrationTests.csproj

# Run a specific test
dotnet test src/AI.Cli.IntegrationTests/AI.Cli.IntegrationTests.csproj --filter "FullyQualifiedName~CliTests"

# Install globally for testing
dotnet pack src/AI.Cli/AI.Cli.csproj
dotnet tool install --global --add-source ./src/AI.Cli/bin/Debug tryagi.ai --prerelease

# Run locally (from repo root)
dotnet run --project src/AI.Cli/AI.Cli.csproj -- --input "Hello" --provider openai --model gpt-4.1
```

## Architecture

### Project Structure

```
AI/
├── AI.slnx
├── src/
│   ├── Directory.Build.props        # Shared build config (C# preview, nullable, implicit usings)
│   ├── Packaging.props              # NuGet packaging properties
│   ├── key.snk                      # Assembly signing key
│   ├── AI.Cli/                      # Main CLI project
│   │   ├── Program.cs               # Entry point — DI setup, root command
│   │   ├── Commands/
│   │   │   ├── DoCommand.cs         # Root command definition (options binding)
│   │   │   ├── DoCommandAction.cs   # Core logic — provider selection, MCP client setup, LLM invocation
│   │   │   ├── ConventionalCommitSchema.cs
│   │   │   ├── MarkdownSchema.cs
│   │   │   └── StringArraySchema.cs
│   │   ├── Models/
│   │   │   ├── Provider.cs          # Enum: Auto, OpenAi, OpenRouter, Anthropic, Free
│   │   │   ├── Tool.cs              # Enum + parser: Filesystem, Fetch, GitHub, Git, Puppeteer, etc.
│   │   │   └── Format.cs            # Output format: Text, Lines, ConventionalCommit, Markdown, Json
│   │   ├── Helpers.cs               # Input/output helpers, LLM client factory
│   │   └── CommonOptions.cs         # Shared CLI option definitions
│   └── AI.Cli.IntegrationTests/     # NUnit + AwesomeAssertions tests
│       ├── CliTests.cs
│       ├── ParseTests.cs
│       └── TestExtensions.cs
└── .github/workflows/               # CI/CD workflows
```

### Core Flow

1. `Program.cs` sets up DI (logging, `DoCommand`, `DoCommandAction`)
2. `DoCommand` defines CLI options (`--input`, `--tools`, `--provider`, `--model`, `--format`, etc.)
3. `DoCommandAction.InvokeAsync()` orchestrates execution:
   - Auto-detects provider from available API keys (`OPENAI_API_KEY`, `ANTHROPIC_API_KEY`, `OPENROUTER_API_KEY`)
   - Resolves model aliases (`auto`, `latest-fast`, `latest-smart`, `free`, `free-fast`, `free-smart`)
   - Spawns MCP clients for each requested tool via `StdioClientTransport` (Docker or npx)
   - Collects `AITool` instances from MCP servers plus built-in tools (file search, GitHub labels, agent delegation)
   - Calls `IChatClient.GetResponseAsync()` with the assembled tools and input
   - Formats output based on `--format` (text, JSON schema-backed structured output, etc.)

### MCP Tool Integrations

Tools are spawned as MCP server processes. Each tool uses either Docker or npx:

| Tool | Transport | Notes |
|------|-----------|-------|
| `Filesystem` | npx `@modelcontextprotocol/server-filesystem` | Scoped to `--directories` |
| `Fetch` | Docker `mcp/fetch` | URL content retrieval |
| `GitHub` | Docker `ghcr.io/github/github-mcp-server` | Requires `GITHUB_TOKEN`, supports toolsets via `github[issues,labels]` |
| `Git` | Docker `mcp/git` | Mounts `--directories` into container |
| `Puppeteer` | Docker `mcp/puppeteer` | Headless browser |
| `SequentialThinking` | Docker `mcp/sequentialthinking` | Planning tool |
| `Slack` | Docker `mcp/slack` | Requires `SLACK_BOT_TOKEN`, `SLACK_TEAM_ID` |
| `Figma` | npx `figma-developer-mcp` | Requires `FIGMA_API_KEY` |
| `DocumentConversion` | uvx `mcp-pandoc` | Format conversion |
| `Agents` | Self-invocation via CliWrap | Spawns sub-`ai` processes |

### Provider Configuration

- **OpenAI**: `OPENAI_API_KEY` env var. Default model: `o3`.
- **Anthropic**: `ANTHROPIC_API_KEY` env var. Default model: `claude-sonnet-4-0`.
- **OpenRouter**: `OPENROUTER_API_KEY` env var. Default model: `google/gemini-2.5-flash-preview`.
- **Auto**: Detects from available env vars in order: OpenAI, Anthropic, OpenRouter.

## Key Conventions

- **Target frameworks**: `net8.0`, `net9.0`
- **Language**: C# preview, nullable reference types, implicit usings
- **CLI framework**: `System.CommandLine` (beta)
- **AI abstraction**: `Microsoft.Extensions.AI` (`IChatClient`)
- **MCP**: `ModelContextProtocol` library for MCP client/transport
- **Testing**: NUnit with AwesomeAssertions, Moq
- **Packaging**: Distributed as .NET global tool (`PackAsTool`, command name `ai`, package ID `tryAGI.AI`)
- **Versioning**: Tag-based via MinVer
- **Code style**: 4 spaces indentation, PascalCase for types/methods, camelCase for locals
