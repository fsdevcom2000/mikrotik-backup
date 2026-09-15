using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Configuration;

public sealed class ConfigService
{
    private readonly YamlConfigLoader _loader;
    private readonly ConfigValidator _validator;

    public ConfigService()
    {
        _loader = new YamlConfigLoader();
        _validator = new ConfigValidator();
    }

    public AppConfig LoadAndValidate(string path)
    {
        var config = _loader.Load(path);

        var errors = _validator.Validate(config);

        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                string.Join(
                    Environment.NewLine,
                    errors.Select(x => $"  - {x}")));
        }

        return config;
    }
}