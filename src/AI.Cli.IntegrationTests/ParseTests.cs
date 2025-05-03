using AI.Cli.Models;

namespace AI.Cli.IntegrationTests;

[TestFixture]
public class ParseTests
{
    [Test]
    public void ParseTool_ShouldReturnToolAndNullToolsets_WhenNoToolsetProvided()
    {
        // Arrange & Act
        var result = ToolExtensions.ParseTool("git");

        // Assert
        result.Tool.Should().Be(Tool.Git);
        result.Toolsets.Should().BeNull();
    }

    [Test]
    public void ParseTool_ShouldReturnToolAndToolsets_WhenToolsetProvided()
    {
        // Arrange & Act
        var result = ToolExtensions.ParseTool("gitHub[repo,issues]");

        // Assert
        result.Tool.Should().Be(Tool.GitHub);
        result.Toolsets.Should().BeEquivalentTo("repo", "issues");
    }

    [Test]
    public void ParseTool_ShouldThrowArgumentException_ForUnknownTool()
    {
        // Arrange
        Action act = () => ToolExtensions.ParseTool("UnknownTool");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Unknown tool: UnknownTool");
    }
}