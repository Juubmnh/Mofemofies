using System.Diagnostics.CodeAnalysis;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Mofemofies.Data;

public abstract class Scriptable
{
    [YamlMember(Order = -1)]
    public string Type { get; init; }

    public Scriptable(string? type = null)
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

    public static ISerializer Serializer { get; private set; }

    public static IDeserializer Deserializer { get; private set; }

    public static INamingConvention NamingConvention { get; private set; }

    [MemberNotNull(nameof(Serializer), nameof(Deserializer), nameof(NamingConvention))]
    public static void SetNamingConvention(INamingConvention namingConvention)
    {
        NamingConvention = namingConvention;
        Serializer = new SerializerBuilder()
            .WithNamingConvention(NamingConvention)
            .Build();
        Deserializer = new DeserializerBuilder()
            .WithNamingConvention(NamingConvention)
            .WithTypeDiscriminatingNodeDeserializer(options => options
            .AddKeyValueTypeDiscriminator<Scriptable>("type", TypeMapping))
            .Build();
    }

    static Scriptor()
    {
        SetNamingConvention(CamelCaseNamingConvention.Instance);
    }
}
