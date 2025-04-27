using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LocalAssistant.Tools;

[McpServerToolType]
public class EchoTool
{
    [McpServerTool, Description("Echoes the input back to the client.")]
    public static string Echo(string message)
    {
        return "hello " + message;
    }
}
