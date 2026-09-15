// MikroTik Backup Manager
//
// Provides simple file-based application logging.
// Log files are stored by date and contain no credentials or other secrets.

using MikroTikBackup.Core.Interfaces;

namespace MikroTikBackup.Storage;

public sealed class FileLogger : ILogger, IDisposable
{
    private readonly object _sync = new();
    private readonly string _logDirectory;

    private StreamWriter? _writer;
    private string? _currentDate;

    public FileLogger(string logDirectory)
    {
        if (string.IsNullOrWhiteSpace(logDirectory))
            throw new ArgumentException(
                "Log directory cannot be empty.",
                nameof(logDirectory));

        _logDirectory =
            Path.GetFullPath(logDirectory);

        Directory.CreateDirectory(_logDirectory);
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Warning(string message)
    {
        Write("WARN", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    public void Error(
        string message,
        Exception exception)
    {
        Write(
            "ERROR",
            $"{message}: {exception.Message}");
    }

    private void Write(
        string level,
        string message)
    {
        var now = DateTime.Now;
        var date = now.ToString("yyyy-MM-dd");

        var line =
            $"{now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        lock (_sync)
        {
            try
            {
                EnsureWriter(date);

                _writer!.WriteLine(line);
                _writer.Flush();
            }
            catch
            {
                // Logging must never break the main application.
            }
        }
    }

    private void EnsureWriter(string date)
    {
        if (string.Equals(
                _currentDate,
                date,
                StringComparison.Ordinal))
        {
            return;
        }

        _writer?.Dispose();
        _writer = null;

        var path =
            Path.Combine(
                _logDirectory,
                $"{date}.log");

        _writer =
            new StreamWriter(
                new FileStream(
                    path,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite),
                new System.Text.UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

        _currentDate = date;
    }

    public void Dispose()
    {
        lock (_sync)
        {
            _writer?.Dispose();
            _writer = null;
            _currentDate = null;
        }
    }
}