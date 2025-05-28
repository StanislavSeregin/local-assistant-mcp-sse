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
        /* PlantUML */
        {
            "PlantUML", new Prompt("Describes how to work with PlantUML sequence diagrams", """
                При работе с диаграммами последовательностей PlantUML необходимо использовать специальную нотацию:
                - Для описания входящих запросов использовать `->`, указывать путь эндпоинта с указанием метода и при необходимости описывать тип запроса
                - Для описания ответов использовать `-->`, кратко указывать что будет возвращено и при необходимости описывать тип ответа
                - При описании типов использовать `typescript`
                - Для раскрытия внутренней логики использовать замыкающие стрелки на себя с описанием операции `A -> A : Do some calculation`

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
        },
        /* MongoDB */
        {
            "MongoDB", new Prompt("Describes how to work with MongoDB using C#", """
                Эта инструкция объясняет на примере, как использовать MongoDB в C#:
                - Пример описания дата класса:
                ```csharp
                using System;
                using MongoDB.Bson;
                using MongoDB.Bson.Serialization.Attributes;

                /// <summary>
                /// Example of a database entity
                /// </summary>
                public class ExampleDataEntity
                {
                    /// <summary>
                    /// Required field for any entity
                    /// </summary>
                    [BsonId]
                    public ObjectId DocumentId { get; set; }

                    /// <summary>
                    /// All enums must be marked with the attribute
                    /// </summary>
                    [BsonRepresentation(BsonType.String)]
                    public SomeEnum SomeStatus { get; set; }

                    /// <summary>
                    /// Simple text field
                    /// </summary>
                    public string? SomeData { get; set; }

                    /// <summary>
                    /// Simple date field
                    /// </summary>
                    public DateTime CreatedAt { get; set; }
                }

                /// <summary>
                /// Example of simple enum
                /// </summary>
                public enum SomeEnum
                {
                    None,
                    Some
                }
                ```

                - Пример инициализации mongo коллекции:
                ```csharp
                using Mongo.Initialization;
                using MongoDB.Driver;

                var collectionName = "YOUR_COLLECTION_NAME";
                var mongoConnectionString = "YOUR_CONNECTION_STRING";
                var dbName = MongoUrl.Create(mongoConnectionString).DatabaseName;
                var mongoClient = MongoInitializer.Create(mongoConnectionString);
                var collection = mongoClient.GetDatabase(dbName).GetCollection<ExampleDataEntity>(collectionName);
                ```

                - Пример чтения 500 элементов из коллекции по статусу из коллекции:
                ```csharp
                var entities = await collection
                    .Find(x => x.SomeStatus == SomeEnum.Some)
                    .Limit(500)
                    .ToListAsync(cancellationToken);
                ```
                """)
        },
        /* LiteDB */
        {
            "LiteDB", new Prompt("Describes how to work with embedded database using C#", """
                - Пример описания дата класса:
                ```csharp
                using System;

                /// <summary>
                /// Example of a database entity
                /// </summary>
                public class ExampleDataEntity
                {
                    /// <summary>
                    /// Id must be marked with the attribute
                    /// </summary>
                    [LiteDB.BsonId]
                    public Guid Id { get; set; }

                    /// <summary>
                    /// Simple text field
                    /// </summary>
                    public string? SomeData { get; set; }
                }
                ```

                - Пример инициализации LiteDB коллекции и добавления индексов
                ```csharp
                using System;
                using LiteDB;

                var db = new LiteDatabase(./data.db);
                var collection = db.GetCollection<ExampleDataEntity>();

                collection.EnsureIndex(x => x.Id, true);
                collection.EnsureIndex(x => x.SomeData);
                ```

                - Пример получения элемента из коллекции по идентификатору:
                ```csharp
                var id = Guid.Parse("SOME_GUID");
                var entity = collection
                    .Query()
                    .Where(x => x.Id == id)
                    .FirstOrDefault();
                ```

                - Пример вставки данных в коллекцию:
                ```csharp
                var entity = new ExampleDataEntity() { Id = Guid.NewGuid(), SomeData = "Some data text" };
                collection.Insert(entity);
                ```

                - Пример обновления элемента в коллекции:
                ```csharp
                collection.Upsert(entity);
                ```
                """)
        }
    };
}