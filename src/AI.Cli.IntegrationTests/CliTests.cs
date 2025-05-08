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
        const string prompt = @"
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

    [Test]
    public async Task Describer()
    {
        const string prompt = @"
You are a bot which describes images. You need to create a description for the image in markdown format next to the image.

Example: if you have image `image.png`, you need to create a markdown description in the same directory with name `image.png.md` 
and content of this file should be a description of the image.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.
- Please try to provide information which help to combine/organize images in the directory.

All required data provided below:
Current Path: /Users/havendv/Downloads/scans/photo_2025-05-03 16.54.07.jpeg
";
//         const string prompt = @"
// You are a bot which describes images. You need to create a description for the image in markdown format next to the image.
//
// Example: if you have image `image.png`, you need to create a markdown description in the same directory with name `image.png.md` 
// and content of this file should be a description of the image.
//
// IMPORTANT:
// - Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.
// - Always call get_file_data before creating a description.
// - Please do it only for ONE image.
//
// All required data provided below:
// Path: /Users/havendv/Downloads/scans/
// ";
        
        await ("--tools filesystem " +
               "--model gpt-4.1 " +
               "--images \"/Users/havendv/Downloads/scans/photo_2025-05-03 16.54.07.jpeg\" " +
               "--directories \"/Users/havendv/Downloads/scans/\" " +
               $"--input \"{prompt}\"")
            .ShouldWork();
    }

    [Test]
    public async Task ManagerOfDescribers()
    {
        // - Please try to provide information which help to combine/organize images in the directory.
        // - Please do it only for ONE image because I testing it.
        // - Don't forget to pass images parameter to the agent, without it agent will not be able to see a image.
        const string prompt = @"
You are a bot which planning how to describe images in the directory.
You need to create a description for all images in folder in markdown format next to the image.
So you need to know list of all images in the directory.

Example: if you have image `image.png`, you need to create a markdown description in the same directory with name `image.png.md` 
and content of this file should be a description of the image.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.

- You need always know what is files in the directory before delegating any work to agent.

All required data provided below:
Directory: /Users/havendv/Downloads/scans/
";
        
        await ("--tools filesystem " + // ,agents
               "--directories \"/Users/havendv/Downloads/scans/\" " +
               $"--input \"{prompt}\"")
            .ShouldWork();
    }

    [Test]
    public async Task Documenter()
    {
        const string prompt = @"
You are a bot which converting text on images to markdown format.
You need to create a content for image in markdown format next to the image.

Example: if you have image `image.png`, you need to create a markdown description in the same directory with name `image.png.content.md` 
and content of this file should be a text content of the image.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.
- Please try to follow formatting of the text in the image.
- Keep underlining, italics and other formatting features - use markdown formatting like `*text*`, `__text__`, etc.

All required data provided below:
Image: /Users/havendv/Downloads/scans/photo_2025-05-03 16.54.07.jpeg
";
        
        await ("--tools filesystem " +
               "--model gpt-4.1 " +
               "--images \"/Users/havendv/Downloads/scans/photo_2025-05-03 16.54.07.jpeg\" " +
               "--directories \"/Users/havendv/Downloads/scans/\" " +
               $"--input \"{prompt}\"")
            .ShouldWork();
    }

    [Test]
    public async Task Worder()
    {
        const string prompt = @"
You are a bot which converting markdown files to .docx format.

Example: if you have image `content.md`, you need to create a markdown description in the same directory with name `content.docx`.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.

All required data provided below:
Path: /Users/havendv/Downloads/scans/photo_2025-05-03 16.54.07.jpeg.content.md
";
        
        await ("--tools filesystem document-conversion " +
               "--directories \"/Users/havendv/Downloads/scans/\" " +
               $"--input \"{prompt}\"")
            .ShouldWork();
    }

    [Test]
    public async Task DescriptionPlanner()
    {
            const string prompt = @"
You are a bot which planning how to describe images in the directory.
You need to create a description for all images in folder in markdown format next to the image.
So you need to know list of all images in the directory.

So you need create `description-plan.md` file in the same directory how to achieve this and track progress.
There should be a list of all images in the directory with current state of description.

Example: if you have image `image.png`, you need to create a markdown description in the same directory with name `image.png.md` 
and content of this file should be a description of the image.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.
- As part of the plan you need to create part to describe prompt for agent which will describe image. It should be very detailed and clear.
- To create new file agent should use `create_file` tool and allow access to `filesystem` tools.

All required data provided below:
Directory: /Users/havendv/Downloads/scans/
";
        
            await ("--tools filesystem " +
                   "--directories \"/Users/havendv/Downloads/scans/\" " +
                   $"--input \"{prompt}\"")
                    .ShouldWork();
    }

    [Test]
    public async Task Delegator()
    {
        const string prompt = @"
You are a bot which executes current plan and delegates specific tasks to other agents.

So read plan document and assign tasks to agents as described in the plan.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.
- ALWAYS read plan document before executing any task - using `read_file` tool.
- ALWAYS call 0 or more(depending on the plan) agents to execute tasks using `delegate_to_agent` tool.

All required data provided below:
Path: /Users/havendv/Downloads/scans/description-plan.md
";
        
            await ("--tools filesystem agents " +
                   "--directories \"/Users/havendv/Downloads/scans/\" " +
                   $"--input \"{prompt}\"")
                    .ShouldWork();
    }

    [Test]
    public async Task ContentPlanner()
    {
            const string prompt = @"
You are a bot which planning how to describe images in the directory.
You need to create a content for all images in folder in markdown format next to the image.
So you need to know list of all images in the directory.

So you need create `content-plan.md` file in the same directory how to achieve this and track progress.
There should be a list of all images in the directory with current state of description.

Example: if you have image `image.png`, you need to create a markdown description in the same directory with name `image.png.md` 
and content of this file should be a description of the image.

IMPORTANT:
- Don't ask anything additional, just do. You were run from CI/CD pipeline, so user can't provide any additional input.

All required data provided below:
Directory: /Users/havendv/Downloads/scans/
";
        
            await ("--tools filesystem " +
                   "--directories \"/Users/havendv/Downloads/scans/\" " +
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