namespace MikroTikBackup.RouterOS.Exceptions;

public class RouterOsException : Exception
{
    public RouterOsException(string message)
        : base(message)
    {
    }

    public RouterOsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}