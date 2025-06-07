using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

[Experimental("SKEXP0001")]
public class TestService(
    McpMiddlewareKernelFunctions mcpMiddlewareKernelFunctions,
    IOptions<Settings> optionsSettings) : BackgroundService
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

        kernel.Plugins.AddFromObject(mcpMiddlewareKernelFunctions);

        var executionSettings = new OpenAIPromptExecutionSettings()
        {
            Temperature = 0,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
        };

        var prompt = "/no_think Hello! Call test tool";
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        await foreach (var content in kernel.InvokePromptStreamingAsync(prompt, new(executionSettings), cancellationToken: stoppingToken))
        {
            Console.Write(content);
        }
    }
}