namespace MikroTikBackup.Core.Models;

public sealed class AppConfig
{
    public StorageConfig Storage { get; set; } = new();
    public BackupConfig Backup { get; set; } = new();
    public TimeoutConfig Timeouts { get; set; } = new();
    public RetryConfig Retry { get; set; } = new();
    public RetentionConfig Retention { get; set; } = new();
    public NotificationsConfig Notifications { get; set; } = new();
    public List<RouterConfig> Routers { get; set; } = [];
    public LoggingConfig Logging { get; set; } = new();
}

public sealed class StorageConfig
{
    public string Path { get; set; } = string.Empty;
}

public sealed class BackupConfig
{
    public bool Binary { get; set; } = true;
    public bool Export { get; set; } = true;
    public int Parallel { get; set; } = 5;
}

public sealed class TimeoutConfig
{
    public int ConnectionSeconds { get; set; } = 10;
    public int OperationSeconds { get; set; } = 60;
    public int TransferSeconds { get; set; } = 300;
}

public sealed class RetryConfig
{
    public int Attempts { get; set; } = 3;
    public int DelaySeconds { get; set; } = 5;
}

public sealed class RetentionConfig
{
    public int Days { get; set; } = 30;
}

public sealed class NotificationsConfig
{
    public TelegramConfig Telegram { get; set; } = new();
}

public sealed class TelegramConfig
{
    public bool Enabled { get; set; }
    public string Token { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

public sealed class RouterConfig
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Protocol { get; set; } = "api-ssl";
    public int Port { get; set; } = 8729;
    public int TransferPort { get; set; } = 22;
    public string Credential { get; set; } = string.Empty;
}
