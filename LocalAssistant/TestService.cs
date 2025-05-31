using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

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

        var funPluginDirectoryPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "prompt_template_samples",
            "FunPlugin");

        var funPluginFunctions = kernel.ImportPluginFromPromptDirectory(funPluginDirectoryPath);

        var arguments = new KernelArguments() { ["input"] = "time travel to dinosaur age" };

        await foreach (var content in kernel.InvokeStreamingAsync(funPluginFunctions["Joke"], arguments, stoppingToken))
        {
            Console.Write(content);
        }
    }
}
