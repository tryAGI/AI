namespace AI.Cli.Models;

internal enum Tool
{
    Filesystem,
    Fetch,
    GitHub,
    Git,
    Puppeteer,
    SequentialThinking,
    Slack,
    Figma,
    DocumentConversion,
    Agents,
}

// Extension class to handle tool parsing with optional toolsets
internal static class ToolExtensions
{
    // Parse a string into a Tool with optional toolset
    public static (Tool Tool, string[]? Toolsets) ParseTool(string input)
    {
        // Preprocess input to remove dashes for kebab-case support
        string processedInput = input.Replace("-", "", StringComparison.Ordinal);

        // Check if the input contains a toolset in square brackets
        int openBracketIndex = processedInput.IndexOf('[', StringComparison.Ordinal);
        if (openBracketIndex > 0 && processedInput.EndsWith(']'))
        {
            string toolName = processedInput.Substring(0, openBracketIndex);
            string toolsetsString = processedInput.Substring(openBracketIndex + 1, processedInput.Length - openBracketIndex - 2);

            // Split by comma to handle multiple toolsets
            string[] toolsets = toolsetsString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (Enum.TryParse<Tool>(toolName, true, out var tool))
            {
                return (tool, toolsets);
            }
        }

        // No toolset specified, just parse the tool name
        if (Enum.TryParse<Tool>(processedInput, ignoreCase: true, out var simpleTool))
        {
            return (simpleTool, null);
        }

        throw new ArgumentException($"Unknown tool: {input}");
    }
}