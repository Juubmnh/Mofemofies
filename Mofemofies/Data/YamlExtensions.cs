using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Mofemofies.Data;

public static class YamlExtensions
{
    public static string SerializeAll(this ISerializer serializer, params IEnumerable<object> items)
    {
        using StringWriter writer = new();

        foreach (var obj in items)
        {
            if (obj is null)
            {
                continue;
            }

            writer.WriteLine("---");
            serializer.Serialize(writer, obj);
        }

        return writer.ToString();
    }

    public static List<T> DeserializeAll<T>(this IDeserializer deserializer, string yaml)
    {
        List<T> items = [];

        using StringReader reader = new(yaml);
        Parser parser = new(reader);
        _ = parser.Consume<StreamStart>();

        while (parser.Accept<DocumentStart>(out _))
        {
            var obj = deserializer.Deserialize<T>(parser);
            if (obj is not null)
            {
                items.Add(obj);
            }
        }

        return items;
    }
}
