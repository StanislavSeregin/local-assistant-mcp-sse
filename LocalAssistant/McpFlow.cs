using Microsoft.SemanticKernel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LocalAssistant;

public class McpFlow(IEnumerable<KernelFunction> tools)
{
    [KernelFunction("test_tool")]
    [Description("This is test tool.")]
    public Task<string> TestTool(
        [Description("Tool payload.")] string payload,
        CancellationToken cancellationToken)
    {
        var t = tools.ToArray();
        var message = "This is test message 1234!";
        return Task.FromResult(message);
    }
}
