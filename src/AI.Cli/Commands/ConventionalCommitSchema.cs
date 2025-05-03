using System.Text;

namespace AI.Cli.Commands;

/// <summary>
/// Represents a single commit following Conventional Commit spec
/// https://www.conventionalcommits.org/en/v1.0.0
/// </summary>
internal sealed class ConventionalCommitSchema
{
    /// <summary>
    /// Commit type (feat, fix, docs, etc.)
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Optional commit scope (e.g., "api")
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Indicates breaking change (using "!")
    /// </summary>
    public bool IsBreakingChange { get; set; }

    /// <summary>
    /// Short commit message description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    // /// <summary>
    // /// Optional detailed commit body
    // /// </summary>
    // public string? Body { get; set; }

    // // Optional footers (e.g., BREAKING CHANGE, Refs, Reviewed-by)
    // public Dictionary<string, string> Footers { get; set; } = [];

    /// <summary>
    /// Generate conventional commit formatted string
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var scopeText = string.IsNullOrWhiteSpace(Scope) ? "" : $"({Scope})";
        var breaking = IsBreakingChange ? "!" : "";

        var header = $"{Type}{scopeText}{breaking}: {Description}";

        var commitBuilder = new StringBuilder(header);

        // if (!string.IsNullOrWhiteSpace(Body))
        // {
        //     commitBuilder.AppendLine().AppendLine().Append(Body);
        // }
        //
        // if (Footers.Count > 0)
        // {
        //     commitBuilder.AppendLine();
        //     foreach (var footer in Footers)
        //     {
        //         commitBuilder.AppendLine().Append($"{footer.Key}: {footer.Value}");
        //     }
        // }

        return commitBuilder.ToString();
    }
}