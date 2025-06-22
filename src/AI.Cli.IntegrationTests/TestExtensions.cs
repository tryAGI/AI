using System.CommandLine;
using AI.Cli.Commands;
using Microsoft.Extensions.Logging;

namespace AI.Cli.IntegrationTests;

public static class TestExtensions
{
    public static async Task ShouldWork(this string arguments)
    {
        // Arrange
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Information);
            builder.AddDebug().SetMinimumLevel(LogLevel.Information);
        });
        
        await using var outputWriter = new StringWriter();
        await using var errorWriter = new StringWriter();
        var rootCommand = new DoCommand(new DoCommandAction(loggerFactory.CreateLogger<DoCommandAction>(), loggerFactory));

        //var test = rootCommand.Parse(arguments);
        //test.Errors.Should().BeEmpty();

        // Act
        var result = await new CommandLineConfiguration(rootCommand)
        {
            Error = errorWriter,
            Output = outputWriter,
        }.Parse(arguments).InvokeAsync();

        Console.WriteLine(outputWriter.ToString());
        Console.WriteLine(errorWriter.ToString());

        // Assert
        result.Should().Be(0);
        errorWriter.ToString().Trim().Should().Be(string.Empty);
        outputWriter.ToString().Trim().Should().NotBeNullOrEmpty();
    }
}