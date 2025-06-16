using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace McpTools.Tools;

public class FSToolSettings
{
    public required string RootPath { get; set; }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFSTool(
        this IServiceCollection services,
        IConfiguration configuration,
        string settingsPath = nameof(FSToolSettings))
    {
        return services
            .Configure<FSToolSettings>(configuration.GetSection(settingsPath))
            .AddSingleton<FSTool>();
    }
}

[McpServerToolType]
public class FSTool(
    IOptions<FSToolSettings> settingsOptions,
    ILogger<FSTool> logger)
{
    [McpServerTool,Description("""
        Retrieves a list of files and folders from the file system based on a given path, for root folder example:
        - [Folder] ./my_folder
        - [File] ./some.txt
        - [File] ./other.cs
        """)]
    public string Listing([Description("Path to target folder. Default folder is './'.")] string path)
    {
        logger.LogInformation("Path is {path}", path);

        path = string.IsNullOrWhiteSpace(path)
            ? "./"
            : path;

        if (!path.StartsWith("./"))
        {
            return $"Error: Path '{path}' should be starts with './'.";
        }

        var absolutPath = Path
            .GetFullPath(path, settingsOptions.Value.RootPath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (!Directory.Exists(absolutPath))
        {
            return $"Error: Path '{path}' does not exist.";
        }

        if (File.Exists(absolutPath))
        {
            return $"Error: '{path}' is a file, not a directory.";
        }

        var result = new StringBuilder();
        foreach (var item in Directory.GetFileSystemEntries(absolutPath))
        {
            var relativePath = Path.GetRelativePath(settingsOptions.Value.RootPath, item);
            if (Directory.Exists(item))
            {
                result.AppendLine($"[Folder] {relativePath}");
            }
            else
            {
                result.AppendLine($"[File] {relativePath}");
            }
        }

        return result.ToString();
    }

    [McpServerTool, Description("Allows to get the contents of a file in UTF-8 encoding by the full file name.")]
    public async Task<string> ReadFile([Description("Path to file. Should be starts with './'.")] string filePath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Path is {filePath}", filePath);

        var absolutPath = Path
            .GetFullPath(filePath, settingsOptions.Value.RootPath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        if (!File.Exists(absolutPath))
        {
            return $"Error: '{filePath}' file is not exists.";
        }

        return await File.ReadAllTextAsync(absolutPath, Encoding.UTF8, cancellationToken);
    }

    [McpServerTool, Description("""
        Allows to write the content to file by the full file name.
        Folders will be created if they do not exist.
        """)]
    public async Task<string> WriteFile(
        [Description("Path to file, should be starts with './'.")] string filePath,
        [Description("UTF-8 encoding content.")] string content,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Path is {filePath}", filePath);

        if (!filePath.StartsWith("./"))
        {
            return $"Error: Path '{filePath}' should be starts with './'.";
        }

        var absolutPath = Path
            .GetFullPath(filePath, settingsOptions.Value.RootPath)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        var dir = Path.GetDirectoryName(absolutPath);
        if (dir is { } && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await File.WriteAllTextAsync(absolutPath, content, cancellationToken);

        return "Done.";
    }
}
