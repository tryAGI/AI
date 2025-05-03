using System.CommandLine;
using AI.Cli.Commands;

var rootCommand = new DoCommand();

return await rootCommand.InvokeAsync(args).ConfigureAwait(false);