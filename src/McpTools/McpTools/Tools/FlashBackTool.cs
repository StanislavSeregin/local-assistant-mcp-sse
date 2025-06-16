using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace McpTools.Tools;

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
            : "Instruction not found. Use your own discretion.";
    }
}

[AttributeUsage(AttributeTargets.All)]
public class DynamicDescription : DescriptionAttribute
{
    private static readonly string KnowledgeOverview = string.Join(
        separator: Environment.NewLine,
        values:
        [
            "You must use the instructions from the list if the task mentions concepts from the description.",
            "Be sure to request the necessary instructions to complete the task.",
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
            "PlantUML", new Prompt("PlantUML sequence diagram", """
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
            "MongoDB", new Prompt("MongoDB using C#", """
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

                - Примечания: Всегда явно описывай дата классы на основе исходной информации, даже если информации недостаточно! Запрещается переиспользовать дата классы MongoDB вне контекста MongoDB!
                """)
        },
        /* LiteDB */
        {
            "LiteDB", new Prompt("Embedded database using C#", """
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

                using var db = new LiteDatabase(./data.db);
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

                - Примечания: Всегда явно описывай дата классы на основе исходной информации, даже если информации недостаточно! Запрещается переиспользовать дата классы LiteDB вне контекста LiteDB!
                """)
        },
        /* Jupyter Notebook */
        {
            "Notebook", new Prompt("\"Notebook\", \"Jupyter Notebook\", \"Dotnet Interactive\" or \"Polyglot\"", """
                - Файл представляет из себя разновидность Jupyter Notebook, поддерживающий dotnet и должен иметь расширение `.dib`
                - Секция Markdown объявляется строкой `#!markdown`, после которой следует описание в этом формате
                - Секция C# кода объявляется строкой `#!csharp`, после которой следует C# код
                - Для вывода результатов всегда использовать статический метод `display(object payload);`
                - Если необходимы сторонние пакеты, то они должны быть перечислены в отдельном блоке C# кода, например `#r "nuget: MongoDB.Driver"`.
                - Запрещается устанавливать дополнительные пакеты для обеспечения поддержки Jupyter Notebook в .NET, так как они включены неявно по умолчанию
                - Пример:
                ```dib
                #!markdown

                ## Заголовок

                #!csharp

                // Пример установки nuget пакетов
                #r "nuget: LiteDB"
                #r "nuget: MongoDB.Driver"

                #!csharp

                // Основное тело скрипта
                var someText = "asdf";
                display(someText);
                ```
                """)
        }
    };
}