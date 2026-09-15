namespace MikroTikBackup.RouterOS.Exceptions;

public sealed class RouterOsTrapException : RouterOsException
{
    public RouterOsTrapException(
        string message,
        string? category = null)
        : base(message)
    {
        Category = category;
    }

    public string? Category { get; }
}