using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

[Experimental("SKEXP0001")]
public class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        await AddMcpToolsAsKernelFunctions(
            builder.Services,
            async ct => [await CreateLocalSseMcpClient(ct)],
            CancellationToken.None);

        builder.Services
            .Configure<Settings>(builder.Configuration.GetSection(nameof(Settings)))
            .AddSingleton<McpFlow>()
            .AddHostedService<TestService>();

        var app = builder.Build();
        await app.RunAsync();
    }

    /// <summary>
    /// Register all MCP clients then map to kernel functions
    /// </summary>
    private static async Task AddMcpToolsAsKernelFunctions(
        IServiceCollection services,
        Func<CancellationToken, Task<IMcpClient[]>> mcpClientFactory,
        CancellationToken cancellationToken)
    {
        foreach (var mcpClient in await mcpClientFactory(cancellationToken))
        {
            services.AddSingleton(mcpClient);
            var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            var kernelFunctions = tools.Select(tool => tool.AsKernelFunction());
            foreach (var kernelFunction in kernelFunctions)
            {
                services.AddSingleton(kernelFunction);
            }
        }
    }

    /// <summary>
    /// Connect to local hosted sse mcp server
    /// </summary>
    private static Task<IMcpClient> CreateLocalSseMcpClient(CancellationToken cancellationToken)
    {
        var sseClientTransport = new SseClientTransport(new SseClientTransportOptions()
        {
            Name = "LocalTools",
            Endpoint = new Uri("http://localhost:3001/sse")
        });

        return McpClientFactory.CreateAsync(sseClientTransport, cancellationToken: cancellationToken);
    }
}
