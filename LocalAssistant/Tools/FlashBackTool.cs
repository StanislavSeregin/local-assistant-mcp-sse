using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LocalAssistant.Tools;

[McpServerToolType]
public class FlashBackTool
{
    [McpServerTool, DynamicDescription]
    public string GetKnowledge([Description("Name of requested instruction.")] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return $"Error: Name should be provided.";
        }

        var filteredName = name
            .Trim()
            .Replace("[", string.Empty)
            .Replace("]", string.Empty);

        return SimplePromptStorage.PromptDict.TryGetValue(filteredName, out var instruction)
            ? instruction.Text
            : "Instruction not found.";
    }
}

[AttributeUsage(AttributeTargets.All)]
public class DynamicDescription : DescriptionAttribute
{
    private static readonly string KnowledgeOverview = string.Join(
        separator: Environment.NewLine,
        values:
        [
            "Analyze the current context and determine which of the following instructions you will need.",
            "The instructions are very important and must be followed.",
            "Available instructions:",
            .. SimplePromptStorage.PromptDict.Select(kvp => $"- [{kvp.Key}]: {kvp.Value.Description}")
        ]);

    public DynamicDescription()
    {
        DescriptionValue = KnowledgeOverview;
    }
}

public record Prompt(string Description, string Text);

public static class SimplePromptStorage
{
    public static readonly Dictionary<string, Prompt> PromptDict = new(StringComparer.OrdinalIgnoreCase)
    {
        {
            "PlantUML", new Prompt("Describes how to work with PlantUML sequence diagrams", """
                При работе с диаграммами последовательностей PlantUML необходимо использовать специальную нотацию:
                - Для описания входящих запросов использовать `->`, указывать путь эндпоинта с указанием метода и описывать тип запроса, если необходимо
                - Для описания ответов использовать `-->`, кратко указывать что будет возвращено и описывать тип ответа, если необходимо
                - При описании типов использовать `typescript`
                - Для раскрытия внутренней логики, использовать замыкающие стрелки на себя с описанием операции `A -> A : Do some calculation`

                Например:
                ```plantuml
                @startuml Sample
                skin rose
                actor Client
                participant Api
                Client -> Api : [POST] /some/path
                note left
                type Request = {
                    id: string,
                    param?: {
                        from: number,
                        to: number
                    }
                }
                end note
                Api -> Api : Some internal action
                Api --> Client : Some response
                note left
                type Response = {
                    kek: string,
                    puk: number
                };
                end note
                @enduml
                ```
                """)
        }
    };
}