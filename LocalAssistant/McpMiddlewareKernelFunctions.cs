using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

public class McpMiddlewareKernelFunctions(IEnumerable<McpClientTool> tools)
{
    [KernelFunction("test_tool")]
    [Description("This is test tool")]
    public Task<string> TestTool(CancellationToken cancellationToken)
    {
        var t = tools.ToArray();
        var message = "This is test message 1234!";
        return Task.FromResult(message);
    }
}
