using AI.Cli.Commands;

namespace AI.Cli.IntegrationTests;

[Explicit]
[TestFixture]
public class CliTests
{
    [Test]
    public async Task DoCommand_WithHelp_ShouldReturnValidOutput()
    {
        await "--help"
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_WithFilesystemTool_ShouldReturnValidOutput()
    {
        await ("--tools filesystem " +
               "--directories \"/Users/havendv/GitHub/tryAGI/\" " +
               "--format markdown " +
               "--input \"Please show me path of FixOpenAPISpec project Program.cs which contains `openApiDocument.Paths` modification. Start from `/Users/havendv/GitHub/tryAGI/` dir. \"")
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_WithConventionalCommitFormat_ShouldReturnValidOutput()
    {
        await ("--format ConventionalCommit " +
               "--input \"There was fixed a bug in FixOpenAPISpec project. Please show me the commit message.\"")
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_WithOpenRouterProvider_ShouldReturnValidOutput()
    {
        await ("--format ConventionalCommit " +
               "--provider openrouter " +
               //"--debug " +
               //"--model free-smart " +
               "--input \"Please show me the commit message for the following text: There was fixed a bug in FixOpenAPISpec project.\"")
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_WithGitAndFileSystem_CreatesRepo_ShouldReturnValidOutput()
    {
        await ("--tools filesystem git " +
               "--directories \"/Users/havendv/GitHub/tryAGI/\" " +
               "--provider openrouter " +
               //"--model free-fast " +
               "--input \"Please create new repo with name `Do` in `/Users/havendv/GitHub/tryAGI/` dir and git init it.\"")
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_AutoLabeling_ShouldReturnValidOutput()
    {
        var prompt = @"
You are a GitHub issue labeling bot. Your task is to label issues based on their content.

IMPORTANT:
- Always retrieve issue body/comments
- Always retrieve available labels to know what is available (because almost always there is custom labels)
- Always call `update_issue` tool to update the issue with suitable labels, but it should be only suitable labels.
- Don't change issue body and other data.
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.
- Carefully analyze the issue body and comments to determine the most appropriate labels.
- Carefully analyze the available labels and their descriptions to ensure you are using the correct labels.

All required data provided below:
Repository Owner: tryAGI
Repository Name: Replicate
Issue Number: 98
";
        await ("--tools github[issues,labels] " +
               "--provider openrouter " +
               "--model google/gemini-2.5-flash-preview " +
               //"--debug " +
               $"--input \"{prompt}\"")
            .ShouldWork();
    }
    
    // [Test]
    // public async Task DoCommand_ExtractIssueToPullRequestData_ShouldReturnValidOutput()
    // {
    //     await ("--tools github[issues] filesystem " +
    //            "--provider openrouter " +
    //            //"--model free-fast " +
    //            //"--debug " +
    //            "--input \"You work in the tryAGI/Replicate repository on the issue #97. Always retrieve issue body/comments and always retrieve available labels (because almost always there is custom labels), and always call `update_issue` tool to update the issue with suitable labels. Don't change body and other data.\"")
    //         .ShouldWork();
    // }
}