namespace MikroTikBackup.RouterOS.Protocol;

public sealed class RouterOsResponse
{
    public string Type { get; init; } = string.Empty;

    public Dictionary<string, string> Attributes { get; } = [];

    public List<string> Words { get; } = [];

    public string? Get(string name)
    {
        return Attributes.TryGetValue(name, out var value)
            ? value
            : null;
    }
}