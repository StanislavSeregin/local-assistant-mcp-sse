using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

[Experimental("SKEXP0001")]
public class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        await AddMcpClientsAndTools(
            builder.Services,
            async ct => [await CreateLocalSseMcpClient(ct)],
            CancellationToken.None);

        builder.Services
            .Configure<Settings>(builder.Configuration.GetSection(nameof(Settings)))
            .AddSingleton<McpMiddlewareKernelFunctions>()
            .AddHostedService<TestService>();

        var app = builder.Build();
        await app.RunAsync();
    }

    /// <summary>
    /// Register all MCP clients and tools from clients
    /// </summary>
    private static async Task AddMcpClientsAndTools(
        IServiceCollection services,
        Func<CancellationToken, Task<IMcpClient[]>> mcpClientFactory,
        CancellationToken cancellationToken)
    {
        foreach (var mcpClient in await mcpClientFactory(cancellationToken))
        {
            services.AddSingleton(mcpClient);
            var tools = await mcpClient.ListToolsAsync(cancellationToken: cancellationToken);
            foreach (var tool in tools)
            {
                services.AddSingleton(tool);
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
