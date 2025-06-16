using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace McpTools;

public class TestHostedService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RoslynAnalyzer.Sample.Kek("C:/Dev/backend-mono/Rusmem.sln", stoppingToken);
    }
}
