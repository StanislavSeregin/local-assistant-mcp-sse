﻿using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
            .Configure<Settings>(builder.Configuration.GetSection(nameof(Settings)))
            .AddHostedService<TestService>();

        var app = builder.Build();
        return app.RunAsync();
    }
}
