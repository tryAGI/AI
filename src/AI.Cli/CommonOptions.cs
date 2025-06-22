using System.CommandLine;
using AI.Cli.Models;

namespace AI.Cli;

internal static class CommonOptions
{
    public static Option<string> Input => new(
        name: "--input",
        aliases: ["-i"])
    {
        Description = "Input text",
        DefaultValueFactory = _ => string.Empty,
    };

    public static Option<FileInfo?> InputFile => new(
        name: "--input-file")
    {
        Description = "Input file path",
        DefaultValueFactory = _ => null,
    };

    public static Option<FileInfo?> OutputFile => new(
        name: "--output-file")
    {
        Description = "Output file path",
        DefaultValueFactory = _ => null,
    };

    public static Option<bool> Debug => new(
        name: "--debug")
    {
        Description = "Show Debug Information",
        DefaultValueFactory = _ => false,
    };

    public static Option<string?> Model => new(
        name: "--model")
    {
        Description = "Model to use for commands.",
        DefaultValueFactory = _ => null,
    };

    public static Option<Provider> Provider => new(
        name: "--provider")
    {
        Description = "Provider to use for commands.",
        DefaultValueFactory = _ => default,
    };
}