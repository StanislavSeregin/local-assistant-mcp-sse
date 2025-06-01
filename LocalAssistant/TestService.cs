using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

[Experimental("SKEXP0001")]
public class TestService(IOptions<Settings> optionsSettings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(10);

        var kernel = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(
                optionsSettings.Value.Model,
                new Uri(optionsSettings.Value.Endpoint),
                optionsSettings.Value.ApiKey,
                httpClient: httpClient)
            .Build();

        var sseClientTransportOptions = new SseClientTransportOptions()
        {
            Name = "SomeTools",
            Endpoint = new Uri("http://localhost:3001/sse")
        };

        await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(
            new SseClientTransport(sseClientTransportOptions),
            cancellationToken: stoppingToken);

        var tools = await mcpClient.ListToolsAsync(cancellationToken: stoppingToken);
        kernel.Plugins.AddFromFunctions(
            "SomeTools",
            tools.Select(aiFunction => aiFunction.AsKernelFunction()));

        var executionSettings = new OpenAIPromptExecutionSettings()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
        };

        var prompt = "Hello! How are you?";
        await foreach (var content in kernel.InvokePromptStreamingAsync(prompt, new(executionSettings), cancellationToken: stoppingToken))
        {
            Console.Write(content);
        }
    }
}
