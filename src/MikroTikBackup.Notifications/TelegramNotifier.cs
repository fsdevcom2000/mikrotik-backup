// MikroTik Backup Manager
//
// Sends optional backup notifications through the Telegram Bot API.
// Telegram failures are isolated from the backup workflow and do not
// change the backup result or process exit code.

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MikroTikBackup.Core.Interfaces;
using MikroTikBackup.Core.Models;

namespace MikroTikBackup.Notifications;

public sealed class TelegramNotifier : ITelegramNotifier
{
    private const string ApiBaseUrl = "https://api.telegram.org";

    private readonly HttpClient _httpClient;
    private readonly TelegramConfig _config;

    public TelegramNotifier(
        HttpClient httpClient,
        TelegramConfig config)
    {
        _httpClient = httpClient ??
            throw new ArgumentNullException(nameof(httpClient));

        _config = config ??
            throw new ArgumentNullException(nameof(config));

        ValidateConfiguration();
    }

    public async Task SendAsync(
        string message,
        CancellationToken cancellationToken)
    {
        if (!_config.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException(
                "Telegram message cannot be empty.",
                nameof(message));

        var url =
            $"{ApiBaseUrl}/bot{_config.Token}/sendMessage";

        var request = new TelegramSendMessageRequest
        {
            ChatId = _config.ChatId,
            Text = message,
            DisableWebPagePreview = true
        };

        using var response = await _httpClient.PostAsJsonAsync(
            url,
            request,
            cancellationToken);

        TelegramApiResponse? result = null;

        try
        {
            result =
                await response.Content.ReadFromJsonAsync<TelegramApiResponse>(
                    cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            // The HTTP status code below is still authoritative.
        }

        if (!response.IsSuccessStatusCode)
        {
            var description =
                result?.Description ??
                $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            throw new InvalidOperationException(
                $"Telegram API request failed: {description}");
        }

        if (result is null || !result.Ok)
        {
            var description =
                result?.Description ??
                "Telegram API returned an unsuccessful response.";

            throw new InvalidOperationException(
                $"Telegram API request failed: {description}");
        }
    }
    private void ValidateConfiguration()
    {
        var hasToken = !string.IsNullOrWhiteSpace(_config.Token);
        var hasChatId = !string.IsNullOrWhiteSpace(_config.ChatId);

        // Telegram is completely disabled and no credentials were supplied.
        if (!_config.Enabled && !hasToken && !hasChatId)
            return;

        // When Telegram is enabled, both credentials are mandatory.
        if (_config.Enabled)
        {
            if (!hasToken)
            {
                throw new ArgumentException(
                    "Telegram bot token is required when Telegram notifications are enabled.",
                    nameof(_config));
            }

            if (!hasChatId)
            {
                throw new ArgumentException(
                    "Telegram chat ID is required when Telegram notifications are enabled.",
                    nameof(_config));
            }

            return;
        }

        // Telegram is disabled, but a partially configured credential pair
        // is still considered invalid.
        if (hasToken != hasChatId)
        {
            throw new ArgumentException(
                "Telegram configuration is incomplete. " +
                "Both bot token and chat ID must be specified together.",
                nameof(_config));
        }

        // Both credentials supplied while disabled is valid.
    }
    
    private sealed class TelegramSendMessageRequest
    {
        [JsonPropertyName("chat_id")]
        public string ChatId { get; init; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;

        [JsonPropertyName("disable_web_page_preview")]
        public bool DisableWebPagePreview { get; init; }
    }

    private sealed class TelegramApiResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }
}