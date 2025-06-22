using System.CommandLine;

namespace AI.Cli.Commands;

internal sealed class DoCommand : RootCommand
{
    public DoCommand(DoCommandAction action) : base(description: "Generates text using a prompt.")
    {
        Options.Add(action.InputOption);
        Options.Add(action.InputFileOption);
        Options.Add(action.OutputFileOption);
        Options.Add(action.ToolsOption);
        Options.Add(action.DirectoriesOption);
        Options.Add(action.FormatOption);
        Options.Add(action.DebugOption);
        Options.Add(action.ModelOption);
        Options.Add(action.ProviderOption);
        Options.Add(action.ImagesOption);

        Action = action;
    }
}