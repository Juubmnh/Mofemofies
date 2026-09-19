using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mofemofies.Data;

public abstract class Scriptable
{
    [YamlMember(Order = -1)]
    public string Type { get; init; }

    protected Scriptable(string? type = null)
    {
        Type = type ?? GetType().Name;
    }
}

public static class Scriptor
{
    private static readonly Dictionary<string, Type> TypeMapping = AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(asm => asm.GetTypes())
        .Where(type => !type.IsAbstract && typeof(Scriptable).IsAssignableFrom(type))
        .ToDictionary(type => ((Scriptable)Activator.CreateInstance(type)!).Type, type => type);

    public static readonly INamingConvention NamingConvention = CamelCaseNamingConvention.Instance;

    public static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(NamingConvention)
        .Build();

    public static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(NamingConvention)
        .WithTypeDiscriminatingNodeDeserializer(options => options
        .AddKeyValueTypeDiscriminator<Scriptable>("type", TypeMapping))
        .Build();
}
