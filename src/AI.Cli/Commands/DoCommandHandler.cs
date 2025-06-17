using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;
using AI.Cli.Models;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Octokit;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatResponseFormat = Microsoft.Extensions.AI.ChatResponseFormat;
using Credentials = Octokit.Credentials;
using Models_Tool = AI.Cli.Models.Tool;
using Tool = AI.Cli.Models.Tool;

namespace AI.Cli.Commands;

#pragma warning disable CA1848
#pragma warning disable CA1861

internal sealed class DoCommandHandler(
    ILogger<DoCommandHandler> logger,
    ILoggerFactory loggerFactory) : ICommandHandler
{
    public Option<string> InputOption { get; } = CommonOptions.Input;
    public Option<FileInfo?> InputFileOption { get; } = CommonOptions.InputFile;
    public Option<FileInfo?> OutputFileOption { get; } = CommonOptions.OutputFile;
    public Option<bool> DebugOption { get; } = CommonOptions.Debug;
    public Option<string?> ModelOption { get; } = CommonOptions.Model;
    public Option<Provider> ProviderOption { get; } = CommonOptions.Provider;
    public Option<string[]> ToolsOption { get; } = new(
        aliases: ["--tools", "-t"],
        description: $"Tools you want to use - {string.Join(", ", Enum.GetNames<Models_Tool>())}. " +
                     "You can specify toolsets using square brackets, e.g., github[issues].")
    {
        AllowMultipleArgumentsPerToken = true,
    };
    public Option<string[]> ImagesOption { get; } = new(
        aliases: ["--images"],
        description: "Paths to images you want to use.")
    {
        AllowMultipleArgumentsPerToken = true,
    };
    public Option<DirectoryInfo[]> DirectoriesOption { get; } = new(
        aliases: ["--directories", "-d"],
        getDefaultValue: () => [new DirectoryInfo(".")],
        description: "Directories you want to use for filesystem.")
    {
        AllowMultipleArgumentsPerToken = true,
    };
    public Option<Format> FormatOption { get; } = new(
        aliases: ["--format", "-f"],
        getDefaultValue: () => Format.Text,
        description: "Format of answer.");

    public int Invoke(InvocationContext context)
    {
        throw new NotImplementedException();
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var input = context.ParseResult.GetValueForOption(InputOption) ?? string.Empty;
        var inputPath = context.ParseResult.GetValueForOption(InputFileOption);
        var outputPath = context.ParseResult.GetValueForOption(OutputFileOption);
        //var debug = context.ParseResult.GetValueForOption(DebugOption);
        var model = context.ParseResult.GetValueForOption(ModelOption);
        var provider = context.ParseResult.GetValueForOption(ProviderOption);
        var toolStrings = context.ParseResult.GetValueForOption(ToolsOption) ?? [];
        var images = context.ParseResult.GetValueForOption(ImagesOption) ?? [];
        var directories = context.ParseResult.GetValueForOption(DirectoriesOption) ?? [];
        var format = context.ParseResult.GetValueForOption(FormatOption);

        // Parse tool strings into tools and toolsets
        var toolsWithToolsets = toolStrings.Select(ToolExtensions.ParseTool).ToArray();
        var tools = toolsWithToolsets.Select(t => t.Tool).Distinct().ToArray();

        // Create a dictionary to store toolsets for each tool
        var toolsetsByTool = toolsWithToolsets
            .Where(t => t.Toolsets != null)
            .GroupBy(t => t.Tool)
            .ToDictionary(
                g => g.Key,
                g => g
                    .SelectMany(t => t.Toolsets!)
                    .Select(x => x.Trim())
                    .ToArray());

        var inputText = await Helpers.ReadInputAsync(input, inputPath).ConfigureAwait(false);
        if (provider == Provider.Auto)
        {
            provider =
                !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
                    ? Provider.OpenAi
                    : !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                        ? Provider.Anthropic
                        : !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"))
                            ? Provider.OpenRouter
                            : throw new InvalidOperationException("No API key found for any provider.");
        }
        model = model switch
        {
            "auto" when provider == Provider.OpenAi => "o3",
            "latest-fast" when provider == Provider.OpenAi => "o3",
            "latest-smart" when provider == Provider.OpenAi => "o3",
            null when provider == Provider.OpenAi => "o3",
            _ when provider == Provider.OpenAi && string.IsNullOrWhiteSpace(model) => "o3",
            
            "auto" when provider == Provider.Anthropic => "claude-sonnet-4-0",
            "latest-fast" when provider == Provider.Anthropic => "claude-sonnet-4-0",
            "latest-smart" when provider == Provider.Anthropic => "claude-opus-4-0",
            null when provider == Provider.Anthropic => "claude-sonnet-4-0",
            _ when provider == Provider.Anthropic && string.IsNullOrWhiteSpace(model) => "claude-sonnet-4-0",
            
            "auto" when provider == Provider.OpenRouter => "google/gemini-2.5-flash-preview",
            "latest-fast" when provider == Provider.OpenRouter => "google/gemini-2.5-flash-preview",
            "latest-smart" when provider == Provider.OpenRouter => "google/gemini-2.5-pro-preview",
            null when provider == Provider.OpenRouter => "google/gemini-2.5-flash-preview",
            _ when provider == Provider.OpenRouter && string.IsNullOrWhiteSpace(model) => "google/gemini-2.5-flash-preview",
            
            "free" or "free-fast" when provider == Provider.OpenRouter => "google/gemini-2.0-flash-exp:free",
            "free-smart" when provider == Provider.OpenRouter => "deepseek/deepseek-r1:free",
            
            _ => model,
        };
        var llm = Helpers.GetChatModel(model!, provider, logger, loggerFactory);

        var clients = await Task.WhenAll(tools.Except([Tool.Agents]).Select(async tool =>
        {
            // Get toolsets for this tool if any
            var toolsets = (toolsetsByTool.GetValueOrDefault(tool) ?? [])
                .Except(["labels"])
                .ToArray();

            return await McpClientFactory.CreateAsync(
                new StdioClientTransport(
                    tool switch
                    {
                        Tool.Filesystem => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.Filesystem),
                            Command = "npx",
                            Arguments = [
                                "-y",
                                "@modelcontextprotocol/server-filesystem",
                                ..directories.Select(x => x.FullName)
                            ],
                        },
                        Tool.Fetch => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.Fetch),
                            Command = "docker",
                            Arguments = [
                                "run",
                                "-i",
                                "--rm",
                                "mcp/fetch"
                            ],
                        },
                        Tool.GitHub => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.GitHub),
                            Command = "docker",
                            Arguments = [
                                "run",
                                "-i",
                                "--rm",
                                "-e",
                                $"GITHUB_PERSONAL_ACCESS_TOKEN={
                                    Environment.GetEnvironmentVariable("GITHUB_TOKEN") ??
                                    throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set.")}",
                                //"-e GITHUB_DYNAMIC_TOOLSETS=1",
                                .. toolsets.Length != 0 ?
                                    ["-e", $"GITHUB_TOOLSETS={string.Join(',', toolsets)}"]
                                    : Array.Empty<string>(),
                                "ghcr.io/github/github-mcp-server"
                            ],
                        },
                        Tool.Git => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.Git),
                            Command = "docker",
                            Arguments = [
                                "run",
                                "-i",
                                "--rm",
                                ..directories.Length != 0
                                    ? new [] { "--mount" }.Concat(directories.Select(x => $"type=bind,src={x},dst={x}"))
                                    : [],
                                "mcp/git"
                            ],
                        },
                        Tool.Puppeteer => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.Puppeteer),
                            Command = "docker",
                            Arguments = [
                                "run",
                                "-i",
                                "--rm",
                                "--init",
                                "-e",
                                "DOCKER_CONTAINER=true",
                                "mcp/puppeteer"
                            ],
                        },
                        Tool.SequentialThinking => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.SequentialThinking),
                            Command = "docker",
                            Arguments = [
                                "run",
                                "-i",
                                "--rm",
                                "mcp/sequentialthinking"
                            ],
                        },
                        Tool.Slack => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.Slack),
                            Command = "docker",
                            Arguments = [
                                "run",
                                "-i",
                                "--rm",
                                "-e",
                                $"SLACK_BOT_TOKEN={Environment.GetEnvironmentVariable("SLACK_BOT_TOKEN")}",
                                "-e",
                                $"SLACK_TEAM_ID={Environment.GetEnvironmentVariable("SLACK_TEAM_ID")}",
                                "-e",
                                $"SLACK_CHANNEL_IDS={Environment.GetEnvironmentVariable("SLACK_CHANNEL_IDS")}",
                                "mcp/slack"
                            ],
                        },
                        Tool.Figma => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.Figma),
                            Command = "npx",
                            Arguments = [
                                "-y",
                                "figma-developer-mcp",
                                $"--figma-api-key={Environment.GetEnvironmentVariable("FIGMA_API_KEY")}",
                                "--stdio"
                            ],
                        },
                        Tool.DocumentConversion => new StdioClientTransportOptions
                        {
                            Name = nameof(Tool.DocumentConversion),
                            Command = "uvx",
                            Arguments = [
                                "mcp-pandoc",
                            ],
                        },
                        _ => throw new ArgumentException($"Unknown tool: {tool}"),
                    }),
                new McpClientOptions
                {
                    ClientInfo = new Implementation
                    {
                        Name = "LangChain CLI DO client",
                        Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0",
                    },
                    InitializationTimeout = TimeSpan.FromMinutes(10),
                }).ConfigureAwait(false);
        })).ConfigureAwait(false);

        var mcpTools = await Task.WhenAll(clients
            .Select(async client => await client.ListToolsAsync().ConfigureAwait(false)))
            .ConfigureAwait(false);

        List<AITool> allTools =
        [
            .. mcpTools.SelectMany(x => x).ToArray(),
            
            .. tools.Contains(Tool.Agents)
                ? new[]
                {
                    AIFunctionFactory.Create(
                        AskAgent,
                        name: "delegate_to_agent",
                        description: "Delegates the task to another agent." +
                                     "It useful if you need to wath image or do only specific things in parallel."),
                }
                : [],
            
            .. tools.Contains(Tool.Filesystem)
                ? new[]
                {
                    AIFunctionFactory.Create(
                        FindFilePathsByContent,
                        name: "find_file_paths_by_content",
                        description: "Finds file paths by content."),
                }
                : [],
            
            .. toolsetsByTool.GetValueOrDefault(Tool.GitHub)?.Contains("labels") == true
                ? new[]
                {
                    AIFunctionFactory.Create(
                        GetAvailableLabels,
                        name: "get_available_labels",
                        description: "Retrieves all available labels for a GitHub repository.")
                }
                : [],
        ];

        logger.LogInformation("Found {Length} AI functions: {@AiTools}",
            allTools.Count,
            allTools);

        var response = await llm.GetResponseAsync(
            new ChatMessage
            {
                Contents = [
                    new TextContent(inputText),
                    .. images
                        .Select(static path => new DataContent(File.ReadAllBytes(path), GetMimeType(path))),
                    .. directories.Length != 0 
                        ? new [] { new TextContent("You have access to these directories:" + Environment.NewLine + string.Join(Environment.NewLine, directories.Select(x => x.FullName))) }
                        : [],
                ]
            },
            new ChatOptions
            {
                ModelId = model,
                Tools = allTools,
                ResponseFormat = format switch
                {
                    Format.Text => ChatResponseFormat.Text,
                    Format.Lines => ChatResponseFormatForType<StringArraySchema>(),
                    Format.ConventionalCommit => ChatResponseFormatForType<ConventionalCommitSchema>(
                        schemaName: "ConventionalCommitSchema",
                        schemaDescription: "Conventional commit schema. Use this schema to generate conventional commits."),
                    Format.Markdown => ChatResponseFormatForType<MarkdownSchema>(
                        schemaName: "MarkdownSchema",
                        schemaDescription: "Markdown schema. Use this schema to generate markdown."),
                    Format.Json => ChatResponseFormat.Json,
                    _ => throw new ArgumentException($"Unknown format: {format}"),
                },
            }).ConfigureAwait(false);

        foreach (var message in response.Messages)
        {
            var toolNames = string.Join(",", message.Contents
                .OfType<FunctionCallContent>()
                .Select(x => $"{x.Name}({string.Join(",", x.Arguments?.Select(y => y.Value?.ToString() ?? string.Empty)?? [])})"));
            // var resultIds = string.Join(",", message.Contents
            //     .OfType<FunctionResultContent>()
            //     .Select(x => x.CallId));
            var content = !string.IsNullOrWhiteSpace(toolNames)
                ? toolNames
                : message.Text;
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }
            
            logger.LogInformation("{Role}: {Content}",
                message.Role.Value,
                content);
        }

        var output = response.Text;
        if (format == Format.Lines)
        {
            var value = JsonSerializer.Deserialize<StringArraySchema>(response.Text);

            output = string.Join(Environment.NewLine, value?.Value ?? []);
        }
        else if (format == Format.Markdown)
        {
            var value = JsonSerializer.Deserialize<MarkdownSchema>(response.Text);

            output = value?.Markdown ?? string.Empty;
        }
        else if (format == Format.ConventionalCommit)
        {
            var value = JsonSerializer.Deserialize<ConventionalCommitSchema>(response.Text);

            output = value?.ToString() ?? string.Empty;
        }

        await Helpers.WriteOutputAsync(output, outputPath, context.Console).ConfigureAwait(false);

        return 0;

        [Description("Finds file paths by content.")]
        static async Task<IList<string>> FindFilePathsByContent(
            [Description("The directory in which the search will be performed. Includes all subdirectories")] string directory,
            [Description("The content to search for in the files. Ignores case.")] string content)
        {
            var paths = new List<string>();

            Debug.WriteLine($"Searching for files in \"{directory}\" containing \"{content}\"...");

            foreach (var path in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    var extension = Path.GetExtension(path);
                    if (extension is not ".txt" and not ".md" and not ".json" and not ".cs" and not ".csproj" and not ".sln" and not ".sh" and not ".yml" and not ".yaml")
                    {
                        continue;
                    }

                    //FileInfo info = new FileInfo(path);
                    var text = await File.ReadAllTextAsync(path).ConfigureAwait(false);

                    if (text.Contains(content, StringComparison.OrdinalIgnoreCase))
                    {
                        paths.Add(path);
                    }
                }
#pragma warning disable CA1031
                catch (Exception)
#pragma warning restore CA1031
                {
                    // ignore
                }
            }

            Debug.WriteLine($"Found {paths.Count} files:");
            foreach (var path in paths)
            {
                Debug.WriteLine(path);
            }

            return paths;
        }

        [Description("Retrieves all available labels for a GitHub repository.")]
        static async Task<IReadOnlyList<Label>> GetAvailableLabels(
            [Description("The owner of the repository")] string owner,
            [Description("The name of the repository")] string name)
        {
            var github = new GitHubClient(new ProductHeaderValue("tryAGI-AI-MCP-extension"))
            {
                Credentials = new Credentials(Environment.GetEnvironmentVariable("GITHUB_TOKEN") ??
                                              throw new InvalidOperationException("GITHUB_TOKEN environment variable is not set."))
            };
            var labels = await github.Issue.Labels.GetAllForRepository(owner, name).ConfigureAwait(false);

            return labels;
        }

        [Description("Asks separate agent to do some specific thing.")]
        static async Task<string> AskAgent(
            [Description("The prompt cleary describes who are the agent, who he need to do, all context, all required output and current situation.")] string prompt,
            [Description("The model to use." +
                         "'gpt-4.1' best for images." +
                         "'o4-mini' better for planning and all other things.")] string? model = null,
            [Description("Tools he can use. Possible tools:" +
                         "Filesystem - allows to interact with files inside defined directories," +
                         "Fetch - allows to retrieve data from url," +
                         "GitHub," +
                         "Git," +
                         "Puppeteer - allows to use headless browser," +
                         "SequentialThinking - allows to plan something," +
                         "Slack - allows to report progress," +
                         "Figma - allows to read design from urls," +
                         "DocumentConversion - allows to convert document between formats - PDF, markdown, Word etc," +
                         "Agents - allows to spawn new agents,")] string[]? tools = null,
            [Description("The full paths to images.")] string[]? images = null,
            [Description("The full paths to directories - you need this only if use Filesystem tool.")] string[]? directories = null)
        {
            var result = await CliWrap.Cli.Wrap(
#if DEBUG
                "/Users/havendv/GitHub/tryAGI/AI/src/AI.Cli/bin/Debug/net9.0/AI.CLI"
#else
                "ai"
#endif
                )
                .WithArguments([
                    "--input", prompt,
                    .. model != null ? ["--model", model] : Array.Empty<string>(),
                    .. tools != null ? ["--tools", .. tools] : Array.Empty<string>(),
                    .. images != null ? ["--images", .. images] : Array.Empty<string>(),
                    .. directories != null ? ["--directories", .. directories] : Array.Empty<string>(),
                ])
                .WithWorkingDirectory(Path.GetFullPath("."))
                .WithValidation(CommandResultValidation.None)
                .ExecuteBufferedAsync();

            return result.StandardOutput;
        }
        
        static string GetMimeType(string path)
        {
            return Path.GetExtension(path).ToUpperInvariant() switch
            {
                ".JPG" or ".JPEG" => "image/jpeg",
                ".PNG" => "image/png",
                ".GIF" => "image/gif",
                ".WEBP" => "image/webp",
                _ => "image/png",
            };
        }
    }

    public static ChatResponseFormatJson ChatResponseFormatForType<T>(
        string? schemaName = null,
        string? schemaDescription = null)
    {
        return ChatResponseFormat.ForJsonSchema(
            JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T), new JsonSchemaExporterOptions
            {
                // Marks root-level types as non-nullable
                TreatNullObliviousAsNonNullable = true,
            }).Deserialize<JsonElement>(), schemaName, schemaDescription);
    }

    public static ChatResponseFormatJson Markdown { get; } = ChatResponseFormatForType<MarkdownSchema>(
        schemaName: "MarkdownSchema",
        schemaDescription: "Markdown schema. Use this schema to generate markdown.");
}