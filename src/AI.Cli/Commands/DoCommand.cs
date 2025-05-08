using System.CommandLine;

namespace AI.Cli.Commands;

internal sealed class DoCommand : RootCommand
{
    public DoCommand(DoCommandHandler doCommandHandler) : base(description: "Generates text using a prompt.")
    {
        AddOption(doCommandHandler.InputOption);
        AddOption(doCommandHandler.InputFileOption);
        AddOption(doCommandHandler.OutputFileOption);
        AddOption(doCommandHandler.ToolsOption);
        AddOption(doCommandHandler.DirectoriesOption);
        AddOption(doCommandHandler.FormatOption);
        AddOption(doCommandHandler.DebugOption);
        AddOption(doCommandHandler.ModelOption);
        AddOption(doCommandHandler.ProviderOption);
        AddOption(doCommandHandler.ImagesOption);

        Handler = doCommandHandler;
    }
}