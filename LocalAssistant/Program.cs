using LocalAssistant.Tools;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace LocalAssistant;

public class Program
{
    static Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services
            .AddFSTool(builder.Configuration)
            .AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly();

        var app = builder.Build();
        app.MapMcp();
        return app.RunAsync();
    }
}
