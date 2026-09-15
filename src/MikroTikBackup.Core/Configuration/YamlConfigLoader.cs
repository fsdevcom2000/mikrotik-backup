using MikroTikBackup.Core.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MikroTikBackup.Core.Configuration;

public sealed class YamlConfigLoader
{
    private readonly IDeserializer _deserializer;

    public YamlConfigLoader()
    {
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(
                UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    public AppConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Configuration file not found: {path}",
                path);
        }

        var yaml = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(yaml))
        {
            throw new InvalidDataException(
                "Configuration file is empty.");
        }

        var config = _deserializer.Deserialize<AppConfig>(yaml);

        if (config == null)
        {
            throw new InvalidDataException(
                "Failed to deserialize configuration.");
        }

        return config;
    }
}