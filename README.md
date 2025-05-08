# AI CLI
AI in your terminal — powered by agents and MCP

[![Nuget package](https://img.shields.io/nuget/vpre/tryAGI.AI)](https://www.nuget.org/packages/tryAGI.AI/)
[![dotnet](https://github.com/tryAGI/AI/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/tryAGI/AI/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/github/license/tryAGI/AI)](https://github.com/tryAGI/AI/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1115206893015662663?label=Discord&logo=discord&logoColor=white&color=d82679)](https://discord.gg/Ca2xhfBf3v)

This is a console utility that will help you use AI for such tasks:
- Summarize text
- Generate release notes
- Generate changelog
- Generate code documentation
- Do actions using mcp servers

## Usage:
```
dotnet tool install --global tryagi.ai --prerelease
set OPENAI_API_KEY = <your_api_key>

# Actions in filesystem and git usage
ai  --tools filesystem git
    --directories "/Users/havendv/GitHub/tryAGI/"
    --provider openrouter
    --model free-fast
    --input "Please create new repo with name `Do` in `/Users/havendv/GitHub/tryAGI/` dir and git init it."

# Auto-labeling GitHub issues
set GITHUB_TOKEN = <your_github_token>

{
    echo "You work in the ${{ github.repository }} repository on the issue #${{ inputs.issue-number }}"
    echo "Your goal is to update the issue with suitable labels."
    echo "Always retrieve the issue body/comments and the available labels for the repository first."
    echo "Then, select appropriate labels based on the issue content and update the issue accordingly."
    echo "Do not change the issue body or other data."
} > .prompt.md

ai  --tools github[issues,labels]
    --input-file .prompt.md
```

## Support

Priority place for bugs: https://github.com/tryAGI/AI/issues  
Priority place for ideas and general questions: https://github.com/tryAGI/AI/discussions  
Discord: https://discord.gg/Ca2xhfBf3v  

## Acknowledgments

![JetBrains logo](https://resources.jetbrains.com/storage/products/company/brand/logos/jetbrains.png)

This project is supported by JetBrains through the [Open Source Support Program](https://jb.gg/OpenSourceSupport).

![CodeRabbit logo](https://opengraph.githubassets.com/1c51002d7d0bbe0c4fd72ff8f2e58192702f73a7037102f77e4dbb98ac00ea8f/marketplace/coderabbitai)

This project is supported by CodeRabbit through the [Open Source Support Program](https://github.com/marketplace/coderabbitai).