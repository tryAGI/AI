using System.CommandLine;
using System.CommandLine.IO;
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
        
        var console = new TestConsole();
        var rootCommand = new DoCommand(new DoCommandHandler(loggerFactory.CreateLogger<DoCommandHandler>(), loggerFactory));

        //var test = rootCommand.Parse(arguments);
        //test.Errors.Should().BeEmpty();

        // Act
        var result = await rootCommand.InvokeAsync(arguments, console);

        Console.WriteLine(console.Error.ToString());
        Console.WriteLine(console.Out.ToString());

        // Assert
        result.Should().Be(0);
        console.Error.ToString()?.Trim().Should().Be(string.Empty);
        //console.Out.ToString()?.Trim().Should().NotBeNullOrEmpty();
    }
}