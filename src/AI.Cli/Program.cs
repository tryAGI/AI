using System.CommandLine;
using AI.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();

services.AddLogging(builder =>
{
#if DEBUG
    builder.AddConsole().SetMinimumLevel(LogLevel.Information);
    builder.AddDebug().SetMinimumLevel(LogLevel.Information);
#else
    builder.AddConsole().SetMinimumLevel(LogLevel.Error);
    builder.AddDebug().SetMinimumLevel(LogLevel.Error);
#endif
});

services.AddSingleton<DoCommand>();
services.AddSingleton<DoCommandHandler>();

var serviceProvider = services.BuildServiceProvider();

var command = serviceProvider.GetRequiredService<DoCommand>();

return await command.InvokeAsync(args).ConfigureAwait(false);