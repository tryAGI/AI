using System.ClientModel;
using AI.Cli.Models;
using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace AI.Cli;

#pragma warning disable CA1848

internal static class Helpers
{
    public static async Task<string> ReadInputAsync(string input, FileInfo? inputPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input) && inputPath is null)
        {
            throw new ArgumentException("Either input or input file must be provided.");
        }

        var inputText = input;
        if (inputPath is not null)
        {
            if (!string.IsNullOrWhiteSpace(inputText))
            {
                inputText += Environment.NewLine;
            }

            inputText += await File.ReadAllTextAsync(inputPath.FullName, cancellationToken).ConfigureAwait(false);
        }

        return inputText;
    }

    public static async Task WriteOutputAsync(string outputText, FileInfo? outputPath, TextWriter? console = null, CancellationToken cancellationToken = default)
    {
        if (outputPath is not null)
        {
            await File.WriteAllTextAsync(outputPath.FullName, outputText, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            if (console is not null)
            {
                await console.WriteLineAsync(outputText).ConfigureAwait(false);
            }
            else
            {
                Console.WriteLine(outputText);
            }
        }
    }

    public static IChatClient GetChatModel(
        string model,
        Provider provider,
        ILogger? logger = null,
        ILoggerFactory? factory = null)
    {
        logger?.LogInformation("Using provider: {Provider}", provider);
        logger?.LogInformation("Using model: {Model}", model);

        IChatClient chatClient;
        Uri? endpoint = provider switch
        {
            Provider.Free or Provider.OpenRouter => new Uri(tryAGI.OpenAI.CustomProviders.OpenRouterBaseUrl),
            _ => null,
        };
        var apiKey = provider switch
        {
            Provider.OpenAi => Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set."),
            Provider.OpenRouter or Provider.Free => Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ??
                throw new InvalidOperationException("OPENROUTER_API_KEY environment variable is not set."),
            Provider.Anthropic => Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ??
                throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable is not set."),
            _ => throw new NotImplementedException(),
        };

        switch (provider)
        {
            case Provider.Anthropic:
                {
#pragma warning disable CA2000
                    chatClient = new AnthropicClient(apiKey);
#pragma warning restore CA2000
                    break;
                }
            
            default:
                {
                    var openAiClient = new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions
                    {
                        Endpoint = endpoint,
                    });

                    chatClient = openAiClient.GetChatClient(model).AsIChatClient();
                    break;
                }
        }

        var client = new ChatClientBuilder(chatClient)
            .UseLogging(factory)
            .UseFunctionInvocation(factory, static invokingChatClient =>
            {
                invokingChatClient.AllowConcurrentInvocation = true;
                invokingChatClient.IncludeDetailedErrors = true;
                invokingChatClient.MaximumConsecutiveErrorsPerRequest = 3;
                invokingChatClient.MaximumIterationsPerRequest = 3;
            })
            .Build();

        return client;
    }
}