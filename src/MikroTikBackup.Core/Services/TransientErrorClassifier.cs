using System.IO;
using System.Net.Sockets;
using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Core.Services;

public sealed class TransientErrorClassifier : ITransientErrorClassifier
{
    public bool IsTransient(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            IOException => true,
            SocketException => true,
            OperationCanceledException => true,
            _ => false
        };
    }
}