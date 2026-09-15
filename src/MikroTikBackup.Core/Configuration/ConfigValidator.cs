using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Core.Configuration;

public sealed class ConfigValidator
{
    public IReadOnlyList<string> Validate(
        AppConfig config)
    {
        var errors = new List<string>();

        ValidateStorage(config, errors);
        ValidateBackup(config, errors);
        ValidateTimeouts(config, errors);
        ValidateRetry(config, errors);
        ValidateRetention(config, errors);
        ValidateRouters(config, errors);

        return errors;
    }

    private static void ValidateStorage(
        AppConfig config,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(config.Storage.Path))
        {
            errors.Add(
                "storage.path is required.");
        }
    }

    private static void ValidateBackup(
        AppConfig config,
        List<string> errors)
    {
        if (!config.Backup.Binary &&
            !config.Backup.Export)
        {
            errors.Add(
                "At least one of backup.binary or backup.export must be enabled.");
        }

        if (config.Backup.Parallel < 1)
        {
            errors.Add(
                "backup.parallel must be greater than zero.");
        }
    }

    private static void ValidateTimeouts(
        AppConfig config,
        List<string> errors)
    {
        if (config.Timeouts.ConnectionSeconds <= 0)
        {
            errors.Add(
                "timeouts.connection_seconds must be greater than zero.");
        }

        if (config.Timeouts.OperationSeconds <= 0)
        {
            errors.Add(
                "timeouts.operation_seconds must be greater than zero.");
        }

        if (config.Timeouts.TransferSeconds <= 0)
        {
            errors.Add(
                "timeouts.transfer_seconds must be greater than zero.");
        }
    }

    private static void ValidateRetry(
        AppConfig config,
        List<string> errors)
    {
        if (config.Retry.Attempts < 1)
        {
            errors.Add(
                "retry.attempts must be at least 1.");
        }

        if (config.Retry.DelaySeconds < 0)
        {
            errors.Add(
                "retry.delay_seconds cannot be negative.");
        }
    }

    private static void ValidateRetention(
        AppConfig config,
        List<string> errors)
    {
        if (config.Retention.Days < 1)
        {
            errors.Add(
                "retention.days must be greater than zero.");
        }
    }

    private static void ValidateRouters(
        AppConfig config,
        List<string> errors)
    {
        if (config.Routers.Count == 0)
        {
            errors.Add(
                "At least one router must be configured.");

            return;
        }

        var names = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var router in config.Routers)
        {
            if (string.IsNullOrWhiteSpace(router.Name))
            {
                errors.Add(
                    "Router name is required.");
            }
            else if (!names.Add(router.Name))
            {
                errors.Add(
                    $"Duplicate router name: {router.Name}");
            }

            if (string.IsNullOrWhiteSpace(router.Address))
            {
                errors.Add(
                    $"Router '{router.Name}' address is required.");
            }

            var protocol =
                router.Protocol.ToLowerInvariant();

            if (protocol is not "api" and not "api-ssl")
            {
                errors.Add(
                    $"Router '{router.Name}' has invalid protocol: {router.Protocol}");
            }

            if (router.Port is < 1 or > 65535)
            {
                errors.Add(
                    $"Router '{router.Name}' has invalid port: {router.Port}");
            }

            if (string.IsNullOrWhiteSpace(router.Credential))
            {
                errors.Add(
                    $"Router '{router.Name}' credential reference is required.");
            }
            if (router.TransferPort < 1 ||
                router.TransferPort > 65535)
            {
                errors.Add(
                    $"Router '{router.Name}': transfer_port must be between 1 and 65535.");
            }
            if (router.Name.IndexOfAny(
                    Path.GetInvalidFileNameChars()) >= 0)
            {
                errors.Add(
                    $"Router '{router.Name}': name contains invalid filename characters.");
            }
            if (router.Name is "." or "..")
            {
                errors.Add(
                    $"Router '{router.Name}': invalid name.");
            }
        }
    }
}