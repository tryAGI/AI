﻿using AI.Cli.Commands;

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
               "--debug " +
               "--model free-smart " +
               "--input \"Please show me the commit message for the following text: There was fixed a bug in FixOpenAPISpec project.\"")
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_WithGitAndFileSystem_CreatesRepo_ShouldReturnValidOutput()
    {
        await ("--tools filesystem,git " +
               "--directories \"/Users/havendv/GitHub/tryAGI/\" " +
               "--provider openrouter " +
               "--model free-fast " +
               "--input \"Please create new repo with name `Do` in `/Users/havendv/GitHub/tryAGI/` dir and git init it.\"")
            .ShouldWork();
    }

    [Test]
    public async Task DoCommand_AutoLabeling_ShouldReturnValidOutput()
    {
        await ("--tools github[issues] " +
               "--provider openrouter " +
               "--model free-fast " +
               "--debug " +
               "--input \"You work in the tryAGI/Replicate repository on the issue #98. Always retrieve issue body/comments and always retrieve available labels (because almost always there is custom labels), and always call `update_issue` tool to update the issue with suitable labels. Don't change body and other data.\"")
            .ShouldWork();
    }
    
    [Test]
    public async Task DoCommand_ExtractIssueToPullRequestData_ShouldReturnValidOutput()
    {
        await ("--tools github[issues] filesystem " +
               "--provider openrouter " +
               "--model free-fast " +
               "--debug " +
               "--input \"You work in the tryAGI/Replicate repository on the issue #97. Always retrieve issue body/comments and always retrieve available labels (because almost always there is custom labels), and always call `update_issue` tool to update the issue with suitable labels. Don't change body and other data.\"")
            .ShouldWork();
    }
}