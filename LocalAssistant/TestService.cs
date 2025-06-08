using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

[Experimental("SKEXP0001")]
public class TestService(
    McpFlow mcpFlow,
    IEnumerable<KernelFunction> tools,
    IOptions<Settings> optionsSettings) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kernel = Kernel
            .CreateBuilder()
            .AddOpenAIChatCompletion(
                optionsSettings.Value.Model,
                new Uri(optionsSettings.Value.Endpoint),
                optionsSettings.Value.ApiKey,
                httpClient: new HttpClient() { Timeout = TimeSpan.FromMinutes(10) })
            .Build();

        kernel.Plugins.AddFromFunctions("McpTools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));
        kernel.Plugins.AddFromObject(mcpFlow);

        var prompt = "/no_think Привет! Какие инструменты тебе доступны?";

        var contents = kernel.InvokePromptStreamingAsync(
            prompt,
            new KernelArguments(new OpenAIPromptExecutionSettings()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new()
                {
                    RetainArgumentTypes = true
                })
            }),
            cancellationToken: stoppingToken);

        await foreach (var content in contents)
        {
            Console.Write(content);
        }
    }
}