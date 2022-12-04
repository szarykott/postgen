using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Postgen.Model;

namespace Postgen;

internal class CollectionWriter
{
    public static void WriteV21(string fileName, ApplicationDescriptor applicationDescriptor)
    {
        var collection = new PostmanCollection21
        {
            Info = new Collection21Info
            {
                Name = "Application Controllers",
                ExporterId = Random.Shared.Next(),
                PostmanId = Guid.NewGuid()
            },
            Item = new List<object>()
        };

        foreach (var (controller, methods) in applicationDescriptor.Controllers)
        {
            var group = new Collection21ItemGroup
            {
                Name = controller.Name,
                Item = new List<object>()
            };

            foreach (var method in methods)
            {
                var item = new Collection21Item
                {
                    Name = method.Name,
                    Request = new Request
                    {
                        Method = method.HttpMethod,
                        Url = $"http://example.com/{controller.RoutePrefix}/{method.Route}",
                    }
                };

                if (method.Body is not null)
                {
                    item.Request.Body = new RequestBody
                    {
                        Raw = method.Body.ToString()!,
                        Options = new RequestBodyOptions
                        {
                            Raw = new RawBodyOptions
                            {
                                Language = "json"
                            }
                        }
                    };
                }

                group.Item.Add(item);
            }

            collection.Item.Add(group);
        }
        
        var serialized = JsonSerializer.Serialize(collection, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        File.WriteAllText(fileName, serialized);
    }
}