// MikroTik Backup Manager
//
// Command-line entry point for configuration, backup, testing,
// status reporting and credential management.
// Coordinates application services and optional Telegram notifications.

using MikroTikBackup.Cli;
using MikroTikBackup.Cli.Security;
using MikroTikBackup.Core.Configuration;
using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;
using MikroTikBackup.Core.Services;
using MikroTikBackup.RouterOS;
using MikroTikBackup.RouterOS.Transfer;
using MikroTikBackup.Storage;
using MikroTikBackup.Notifications;


const string configPath = "config.yaml";
const string credentialsPath = "credentials.dat";

if (args.Length == 0)
{
    PrintUsage();
    return 3;
}

var command = args[0].ToLowerInvariant();

try
{
    switch (command)
    {
        case "config":
            await HandleConfigCommandAsync(args);
            return 0;

        case "credentials":
            await HandleCredentialsCommandAsync(args);
            return 0;

        case "test":
            return await HandleTestCommandAsync(args);

        case "backup":
            return await HandleBackupCommandAsync(args);

        case "status":
            return await HandleStatusCommandAsync(args);

        default:
            Console.Error.WriteLine(
                $"Unknown command: {args[0]}");

            PrintUsage();

            return 3;
    }
}
catch (InvalidDataException ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Configuration error:");
    Console.Error.WriteLine(ex.Message);

    return 2;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(ex.Message);

    return 2;
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine(ex.Message);

    return 3;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"ERROR: {ex.Message}");

    return 4;
}

static async Task HandleConfigCommandAsync(
    string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine(
            "Usage: MikroTikBackup.exe config validate");

        throw new ArgumentException();
    }

    if (!args[1].Equals(
            "validate",
            StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            $"Unknown config command: {args[1]}");

        throw new ArgumentException();
    }

    var service = new ConfigService();

    var config =
        service.LoadAndValidate(configPath);

    using var logger =
        CreateLogger(config);

    logger.Info("Application started");
    logger.Info("Command: config validate");

    try
    {
        Console.WriteLine(
            "Configuration is valid.");

        logger.Info("Configuration validation completed successfully");

        await Task.CompletedTask;
    }
    catch (Exception ex)
    {
        logger.Error(
            "Configuration validation failed",
            ex);

        throw;
    }
    finally
    {
        logger.Info("Application finished");
    }
}

static async Task HandleCredentialsCommandAsync(
    string[] args)
{
    if (args.Length < 2 ||
        !args[1].Equals(
            "add",
            StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine(
            "Usage: MikroTikBackup.exe credentials add <reference>");

        throw new ArgumentException();
    }

    if (args.Length < 3)
    {
        Console.Error.WriteLine(
            "Credential reference is required.");

        throw new ArgumentException();
    }

    var reference = args[2];

    var configService =
        new ConfigService();

    var config =
        configService.LoadAndValidate(configPath);

    using var logger =
        CreateLogger(config);

    logger.Info("Application started");
    logger.Info("Command: credentials add");
    logger.Info(
        $"Credential reference: '{reference}'");

    try
    {
        Console.Write("Username: ");

        var username =
            Console.ReadLine();

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Username cannot be empty.");
        }

        Console.Write("Password: ");

        var password =
            ReadPassword();

        await CredentialFileManager.AddAsync(
            credentialsPath,
            reference,
            username,
            password,
            CancellationToken.None);

        Console.WriteLine();
        Console.WriteLine(
            $"Credential '{reference}' saved.");

        logger.Info(
            $"Credential '{reference}' saved successfully");
    }
    catch (Exception ex)
    {
        logger.Error(
            "Credential operation failed",
            ex);

        throw;
    }
    finally
    {
        logger.Info("Application finished");
    }
}

static async Task<int> HandleTestCommandAsync(
    string[] args)
{
    var configService =
        new ConfigService();

    var config =
        configService.LoadAndValidate(
            configPath);

    using var logger =
        CreateLogger(config);

    logger.Info("Application started");
    logger.Info("Command: test");

    var router =
        SelectRouter(
            config.Routers,
            args);

    logger.Info(
        $"Selected router: '{router.Name}'");

    var credentialStore =
        new MikroTikBackup.Core.Security.CredentialStore(
            credentialsPath);

    var credential =
        await credentialStore.GetAsync(
            router.Credential,
            CancellationToken.None);

    logger.Info(
        $"Starting connectivity test for router '{router.Name}'");

    Console.WriteLine();
    Console.WriteLine(
        "MikroTik Backup Manager");

    Console.WriteLine();
    Console.WriteLine(
        $"Testing router: {router.Name}");

    await using var client =
        new RouterOsClient();

    string? remoteBackupName = null;
    string? remoteExportName = null;
    string? localBackupPath = null;

    try
    {
        Console.Write("  Connecting       ");

        logger.Info(
            $"Connecting to router '{router.Name}'");

        await client.ConnectAsync(
            router,
            credential,
            TimeSpan.FromSeconds(
                config.Timeouts.ConnectionSeconds),
            CancellationToken.None);

        Console.WriteLine("OK");

        logger.Info(
            $"Connected to router '{router.Name}'");

        Console.Write("  RouterOS         ");

        var version =
            await client.GetRouterOsVersionAsync(
                CancellationToken.None);

        Console.WriteLine(version);

        logger.Info(
            $"RouterOS version: {version}");

        remoteBackupName =
            $"mbm-{Guid.NewGuid():N}.backup";

        Console.Write("  Creating backup  ");

        logger.Info(
            $"Creating remote backup '{remoteBackupName}'");

        await client.CreateBackupAsync(
            remoteBackupName,
            CancellationToken.None);

        var backupSize =
            await client.GetRemoteFileSizeAsync(
                remoteBackupName,
                CancellationToken.None);

        Console.WriteLine(
            $"OK ({backupSize} bytes)");

        logger.Info(
            $"Remote backup created: {backupSize} bytes");

        localBackupPath =
            Path.Combine(
                Path.GetTempPath(),
                remoteBackupName);

        Console.Write("  SFTP download    ");

        logger.Info(
            $"Downloading '{remoteBackupName}' via SFTP");

        var transfer =
            new SftpFileTransfer();

        await transfer.DownloadAsync(
            router,
            credential,
            remoteBackupName,
            localBackupPath,
            TimeSpan.FromSeconds(
                config.Timeouts.TransferSeconds),
            CancellationToken.None);

        var localBackupInfo =
            new FileInfo(localBackupPath);

        Console.WriteLine(
            $"OK ({localBackupInfo.Length} bytes)");

        logger.Info(
            $"SFTP download completed: {localBackupInfo.Length} bytes");

        Console.Write("  Size verification");

        if (localBackupInfo.Length != backupSize)
        {
            throw new InvalidDataException(
                $"Downloaded file size mismatch. " +
                $"Remote: {backupSize} bytes, " +
                $"local: {localBackupInfo.Length} bytes.");
        }

        Console.WriteLine(" OK");

        logger.Info(
            "Backup size verification passed");

        remoteExportName =
            $"mbm-{Guid.NewGuid():N}.rsc";

        Console.Write("  Creating export  ");

        logger.Info(
            $"Creating remote export '{remoteExportName}'");

        await client.CreateExportAsync(
            remoteExportName,
            CancellationToken.None);

        var exportSize =
            await client.GetRemoteFileSizeAsync(
                remoteExportName,
                CancellationToken.None);

        Console.WriteLine(
            $"OK ({exportSize} bytes)");

        logger.Info(
            $"Remote export created: {exportSize} bytes");

        Console.Write("  Removing remote ");

        await client.DeleteRemoteFileAsync(
            remoteBackupName,
            CancellationToken.None);

        await client.DeleteRemoteFileAsync(
            remoteExportName,
            CancellationToken.None);

        Console.WriteLine("OK");

        logger.Info(
            "Remote test files removed");

        Console.WriteLine();
        Console.WriteLine(
            "Test completed successfully.");

        logger.Info(
            $"Connectivity test completed successfully for router '{router.Name}'");

        return 0;
    }
    catch (Exception ex)
    {
        logger.Error(
            $"Connectivity test failed for router '{router.Name}'",
            ex);

        throw;
    }
    finally
    {
        if (localBackupPath != null)
        {
            try
            {
                if (File.Exists(localBackupPath))
                {
                    File.Delete(localBackupPath);

                    logger.Info(
                        "Temporary local test file removed");
                }
            }
            catch (Exception ex)
            {
                logger.Warning(
                    $"Temporary test file cleanup failed: {ex.Message}");
            }
        }

        logger.Info("Application finished");
    }
}

static async Task<int> HandleBackupCommandAsync(
    string[] args)
{
    var configService =
        new ConfigService();

    var config =
        configService.LoadAndValidate(
            configPath);

    using var logger =
        CreateLogger(config);

    logger.Info("Application started");
    logger.Info("Command: backup");

    var routers =
        GetSelectedRouters(
            config.Routers,
            args);

    logger.Info(
        $"Selected routers: {routers.Count}");

    foreach (var router in routers)
    {
        logger.Info(
            $"Selected router: '{router.Name}'");
    }

    /*
     * Validate Telegram configuration before starting
     * the actual backup operation.
     *
     * This prevents a bad Telegram configuration from
     * being discovered only after the backups are complete.
     */
    TelegramNotifier telegramNotifier;
    TelegramSummaryBuilder telegramSummaryBuilder;
    HttpClient? telegramHttpClient = null;

    try
    {
        telegramHttpClient = new HttpClient();

        telegramNotifier =
            new TelegramNotifier(
                telegramHttpClient,
                config.Notifications.Telegram);

        telegramSummaryBuilder =
            new TelegramSummaryBuilder();
    }
    catch (ArgumentException ex)
    {
        telegramHttpClient?.Dispose();

        throw new InvalidDataException(
            ex.Message,
            ex);
    }

    using (telegramHttpClient)
    {
        if (!config.Notifications.Telegram.Enabled)
        {
            logger.Info(
                "Telegram notifications are disabled");
        }

        var credentialStore =
            new MikroTikBackup.Core.Security.CredentialStore(
                credentialsPath);

        var backupServiceFactory =
            new BackupServiceFactory(
                logger);

        var backupRunner =
            new BackupRunner(
                backupServiceFactory,
                credentialStore);

        try
        {
            Console.WriteLine();
            Console.WriteLine(
                "MikroTik Backup Manager");

            Console.WriteLine();

            Console.WriteLine(
                $"Backup started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            Console.WriteLine(
                $"Routers: {routers.Count}");

            logger.Info(
                $"Backup started for {routers.Count} router(s)");

            var runResult =
                await backupRunner.RunAsync(
                    routers,
                    config,
                    CancellationToken.None);

            foreach (var result in runResult.Results)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[{result.Router}]");

                PrintBackupResult(result);
            }

            Console.WriteLine();
            Console.WriteLine(
                "Backup completed");

            Console.WriteLine();

            Console.WriteLine(
                $"Successful: {runResult.SuccessCount}");

            Console.WriteLine(
                $"Warnings:   {runResult.WarningCount}");

            Console.WriteLine(
                $"Failed:     {runResult.FailedCount}");

            Console.WriteLine();

            logger.Info(
                $"Backup completed. Successful: {runResult.SuccessCount}, " +
                $"Warnings: {runResult.WarningCount}, " +
                $"Failed: {runResult.FailedCount}");

            /*
             * Telegram notification is deliberately performed
             * after the backup result has already been completed.
             *
             * A Telegram failure must never change the backup
             * result or its exit code.
             */
            if (config.Notifications.Telegram.Enabled)
            {
                try
                {
                    logger.Info(
                        "Sending backup summary to Telegram");

                    var message =
                        telegramSummaryBuilder.Build(
                            runResult);

                    await telegramNotifier.SendAsync(
                        message,
                        CancellationToken.None);

                    logger.Info(
                        "Telegram notification sent successfully");

                    Console.WriteLine(
                        "Telegram notification: OK");
                }
                catch (Exception ex)
                {
                    logger.Warning(
                        $"Telegram notification failed: {ex.Message}");

                    Console.WriteLine(
                        $"Telegram notification: FAILED - {ex.Message}");
                }
            }

            return runResult.IsSuccessful
                ? 0
                : 1;
        }
        catch (Exception ex)
        {
            logger.Error(
                "Backup command failed",
                ex);

            throw;
        }
        finally
        {
            logger.Info("Application finished");
        }
    }
}

static async Task<int> HandleStatusCommandAsync(
    string[] args)
{
    var configService =
        new ConfigService();

    var config =
        configService.LoadAndValidate(
            configPath);

    using var logger =
        CreateLogger(config);

    logger.Info("Application started");
    logger.Info("Command: status");

    try
    {
        var routers =
            GetSelectedRouters(
                config.Routers,
                args);

        logger.Info(
            $"Selected routers: {routers.Count}");

        var statusService =
            new MikroTikBackup.Storage.StatusService();

        var statuses =
            await statusService.GetStatusAsync(
                routers,
                config.Storage.Path,
                CancellationToken.None);

        Console.WriteLine();
        Console.WriteLine(
            "MikroTik Backup Manager");

        Console.WriteLine();
        Console.WriteLine(
            "Backup status");

        var okCount = 0;
        var warningCount = 0;
        var failedCount = 0;
        var noBackupCount = 0;

        foreach (var status in statuses)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"[{status.Router}]");

            Console.WriteLine(
                $"  Address:      {status.Address}");

            var statusText =
                status.Status switch
                {
                    BackupStatus.Success => "OK",
                    BackupStatus.SuccessWithWarning => "WARNING",
                    BackupStatus.Failed => "FAILED",
                    null => "NO BACKUP",
                    _ => "UNKNOWN"
                };

            Console.WriteLine(
                $"  Status:       {statusText}");

            switch (status.Status)
            {
                case BackupStatus.Success:
                    okCount++;
                    break;

                case BackupStatus.SuccessWithWarning:
                    warningCount++;
                    break;

                case BackupStatus.Failed:
                    failedCount++;
                    break;

                default:
                    noBackupCount++;
                    break;
            }

            if (!string.IsNullOrWhiteSpace(
                    status.RouterOsVersion))
            {
                Console.WriteLine(
                    $"  RouterOS:     {status.RouterOsVersion}");
            }

            if (status.CompletedAt.HasValue)
            {
                Console.WriteLine(
                    $"  Completed:    {status.CompletedAt.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}");

                Console.WriteLine(
                    $"  Last backup:  {FormatAge(status.CompletedAt.Value)}");
            }

            if (status.BackupSize.HasValue)
            {
                Console.WriteLine(
                    $"  Backup:       {status.BackupSize.Value} bytes");
            }

            if (status.ExportSize.HasValue)
            {
                Console.WriteLine(
                    $"  Export:       {status.ExportSize.Value} bytes");
            }

            if (!string.IsNullOrWhiteSpace(
                    status.Warning))
            {
                Console.WriteLine(
                    $"  Warning:      {status.Warning}");
            }

            if (!string.IsNullOrWhiteSpace(
                    status.Error))
            {
                Console.WriteLine(
                    $"  Error:        {status.Error}");
            }

            logger.Info(
                $"Status for '{status.Router}': {statusText}");
        }

        Console.WriteLine();
        Console.WriteLine(
            "Summary");

        Console.WriteLine(
            $"  OK:           {okCount}");

        Console.WriteLine(
            $"  Warnings:     {warningCount}");

        Console.WriteLine(
            $"  Failed:       {failedCount}");

        Console.WriteLine(
            $"  No backup:    {noBackupCount}");

        Console.WriteLine();

        logger.Info(
            $"Status completed. OK: {okCount}, " +
            $"Warnings: {warningCount}, " +
            $"Failed: {failedCount}, " +
            $"No backup: {noBackupCount}");

        return 0;
    }
    catch (Exception ex)
    {
        logger.Error(
            "Status command failed",
            ex);

        throw;
    }
    finally
    {
        logger.Info("Application finished");
    }
}

static FileLogger CreateLogger(
    AppConfig config)
{
    var path =
        config.Logging?.Path;

    if (string.IsNullOrWhiteSpace(path))
    {
        path = @"D:\MikroTik\Logs";
    }

    return new FileLogger(path);
}

static List<RouterConfig> GetSelectedRouters(
    List<RouterConfig> routers,
    string[] args)
{
    var routerName =
        GetRouterArgument(args);

    if (routerName == null)
    {
        return routers;
    }

    var router =
        routers.FirstOrDefault(
            x => string.Equals(
                x.Name,
                routerName,
                StringComparison.OrdinalIgnoreCase));

    if (router == null)
    {
        throw new InvalidDataException(
            $"Router not found: {routerName}");
    }

    return [router];
}

static RouterConfig SelectRouter(
    IReadOnlyList<RouterConfig> routers,
    string[] args)
{
    string? requestedName = null;

    for (var i = 1; i < args.Length - 1; i++)
    {
        if (args[i].Equals(
                "--router",
                StringComparison.OrdinalIgnoreCase))
        {
            requestedName =
                args[i + 1];

            break;
        }
    }

    if (requestedName == null)
    {
        if (routers.Count == 1)
            return routers[0];

        throw new InvalidDataException(
            "Multiple routers configured. " +
            "Specify --router <name>.");
    }

    var router =
        routers.FirstOrDefault(
            x => x.Name.Equals(
                requestedName,
                StringComparison.OrdinalIgnoreCase));

    if (router == null)
    {
        throw new InvalidDataException(
            $"Router '{requestedName}' was not found.");
    }

    return router;
}

static string ReadPassword()
{
    var builder =
        new System.Text.StringBuilder();

    while (true)
    {
        var key =
            Console.ReadKey(
                intercept: true);

        if (key.Key == ConsoleKey.Enter)
            break;

        if (key.Key == ConsoleKey.Backspace)
        {
            if (builder.Length > 0)
                builder.Length--;

            continue;
        }

        if (!char.IsControl(key.KeyChar))
            builder.Append(key.KeyChar);
    }

    Console.WriteLine();

    return builder.ToString();
}

static string? GetRouterArgument(
    string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(
                args[i],
                "--router",
                StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static void PrintBackupResult(
    RouterBackupResult result)
{
    var status =
        result.Status switch
        {
            BackupStatus.Success => "OK",
            BackupStatus.SuccessWithWarning => "WARNING",
            BackupStatus.Failed => "FAILED",
            _ => "UNKNOWN"
        };

    Console.WriteLine(
        $"  Status:      {status}");

    if (!string.IsNullOrWhiteSpace(
            result.RouterOsVersion))
    {
        Console.WriteLine(
            $"  RouterOS:    {result.RouterOsVersion}");
    }

    if (result.BackupSize.HasValue)
    {
        Console.WriteLine(
            $"  Backup:      {result.BackupSize.Value} bytes");
    }

    if (result.ExportSize.HasValue)
    {
        Console.WriteLine(
            $"  Export:      {result.ExportSize.Value} bytes");
    }

    if (!string.IsNullOrWhiteSpace(
            result.BackupSha256))
    {
        Console.WriteLine(
            $"  Backup SHA256: {result.BackupSha256}");
    }

    if (!string.IsNullOrWhiteSpace(
            result.ExportSha256))
    {
        Console.WriteLine(
            $"  Export SHA256: {result.ExportSha256}");
    }

    Console.WriteLine(
        $"  Duration:    {result.Duration.TotalSeconds:F1}s");

    if (!string.IsNullOrWhiteSpace(
            result.Error))
    {
        Console.WriteLine(
            $"  Error:       {result.Error}");
    }
}

static string FormatAge(
    DateTime completedAt)
{
    var age =
        DateTime.UtcNow - completedAt.ToUniversalTime();

    if (age.TotalSeconds < 60)
        return $"{Math.Max(0, (int)age.TotalSeconds)}s ago";

    if (age.TotalMinutes < 60)
        return $"{(int)age.TotalMinutes}m ago";

    if (age.TotalHours < 24)
        return $"{(int)age.TotalHours}h ago";

    return $"{(int)age.TotalDays}d ago";
}

static void PrintUsage()
{
    Console.WriteLine(
        """
        MikroTik Backup Manager

        Usage:

          MikroTikBackup.exe backup
          MikroTikBackup.exe backup --router <name>

          MikroTikBackup.exe config validate

          MikroTikBackup.exe credentials add <reference>

          MikroTikBackup.exe test
          MikroTikBackup.exe test --router <name>

          MikroTikBackup.exe status
          MikroTikBackup.exe status --router <name>
        """);
}