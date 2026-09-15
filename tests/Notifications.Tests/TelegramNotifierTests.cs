using System.Net;
using System.Net.Http;
using System.Text;
using MikroTikBackup.Core.Models;
using MikroTikBackup.Notifications;
using Xunit;

namespace Notifications.Tests;

public sealed class TelegramNotifierTests
{
    [Fact]
    public async Task SendAsync_WhenDisabled_DoesNotSendRequest()
    {
        var handler = new TestHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = false
        };

        var notifier = new TelegramNotifier(
            httpClient,
            config);

        await notifier.SendAsync(
            "Test message",
            CancellationToken.None);

        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public void Constructor_WhenEnabledWithoutToken_ThrowsArgumentException()
    {
        var handler = new TestHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            ChatId = "123456789"
        };

        var exception = Assert.Throws<ArgumentException>(
            () => new TelegramNotifier(
                httpClient,
                config));

        Assert.Contains(
            "Telegram bot token is required",
            exception.Message);
    }

    [Fact]
    public void Constructor_WhenEnabledWithoutChatId_ThrowsArgumentException()
    {
        var handler = new TestHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            Token = "test-token"
        };

        var exception = Assert.Throws<ArgumentException>(
            () => new TelegramNotifier(
                httpClient,
                config));

        Assert.Contains(
            "Telegram chat ID is required",
            exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsSuccess_CompletesSuccessfully()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateResponse(
                HttpStatusCode.OK,
                """
                {
                  "ok": true,
                  "result": {
                    "message_id": 123
                  }
                }
                """)
        };

        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            Token = "123456:test-token",
            ChatId = "987654321"
        };

        var notifier = new TelegramNotifier(
            httpClient,
            config);

        await notifier.SendAsync(
            "Backup completed",
            CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        Assert.NotNull(handler.LastRequest);

        Assert.Equal(
            HttpMethod.Post,
            handler.LastRequest!.Method);

        Assert.Contains(
            "/bot123456:test-token/sendMessage",
            handler.LastRequest.RequestUri!.ToString());

        var body = await handler.LastRequestContent!.ReadAsStringAsync();

        Assert.Contains(
            "\"chat_id\":\"987654321\"",
            body);

        Assert.Contains(
            "\"text\":\"Backup completed\"",
            body);

        Assert.Contains(
            "\"disable_web_page_preview\":true",
            body);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsHttpError_ThrowsInvalidOperationException()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateResponse(
                HttpStatusCode.BadRequest,
                """
                {
                  "ok": false,
                  "description": "Bad Request: chat not found"
                }
                """)
        };

        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            Token = "123456:test-token",
            ChatId = "987654321"
        };

        var notifier = new TelegramNotifier(
            httpClient,
            config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => notifier.SendAsync(
                "Backup completed",
                CancellationToken.None));

        Assert.Contains(
            "Telegram API request failed",
            exception.Message);

        Assert.Contains(
            "chat not found",
            exception.Message);

        Assert.DoesNotContain(
            "123456:test-token",
            exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsOkFalse_ThrowsInvalidOperationException()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateResponse(
                HttpStatusCode.OK,
                """
                {
                  "ok": false,
                  "description": "Forbidden: bot was blocked by the user"
                }
                """)
        };

        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            Token = "123456:test-token",
            ChatId = "987654321"
        };

        var notifier = new TelegramNotifier(
            httpClient,
            config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => notifier.SendAsync(
                "Backup completed",
                CancellationToken.None));

        Assert.Contains(
            "bot was blocked by the user",
            exception.Message);

        Assert.DoesNotContain(
            "123456:test-token",
            exception.Message);
    }

    [Fact]
    public async Task SendAsync_WhenMessageIsEmpty_ThrowsArgumentException()
    {
        var handler = new TestHttpMessageHandler();
        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            Token = "123456:test-token",
            ChatId = "987654321"
        };

        var notifier = new TelegramNotifier(
            httpClient,
            config);

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => notifier.SendAsync(
                " ",
                CancellationToken.None));

        Assert.Contains(
            "Telegram message cannot be empty",
            exception.Message);

        Assert.Equal(
            0,
            handler.RequestCount);
    }

    [Fact]
    public async Task SendAsync_WhenApiReturnsInvalidJson_ThrowsInvalidOperationException()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateResponse(
                HttpStatusCode.OK,
                "not-json")
        };

        using var httpClient = new HttpClient(handler);

        var config = new TelegramConfig
        {
            Enabled = true,
            Token = "123456:test-token",
            ChatId = "987654321"
        };

        var notifier = new TelegramNotifier(
            httpClient,
            config);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => notifier.SendAsync(
                "Backup completed",
                CancellationToken.None));

        Assert.Contains(
            "Telegram API request failed",
            exception.Message);

        Assert.DoesNotContain(
            "123456:test-token",
            exception.Message);
    }

    [Fact]
    public void Constructor_WithTokenButWithoutChatId_ThrowsArgumentException()
    {
        var config = new TelegramConfig
        {
            Enabled = false,
            Token = "test-token",
            ChatId = string.Empty
        };

        using var httpClient = new HttpClient();

        var exception = Assert.Throws<ArgumentException>(() =>
            new TelegramNotifier(httpClient, config));

        Assert.Contains(
            "incomplete",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Constructor_WithChatIdButWithoutToken_ThrowsArgumentException()
    {
        var config = new TelegramConfig
        {
            Enabled = false,
            Token = string.Empty,
            ChatId = "123456"
        };

        using var httpClient = new HttpClient();

        var exception = Assert.Throws<ArgumentException>(() =>
            new TelegramNotifier(httpClient, config));

        Assert.Contains(
            "incomplete",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static HttpResponseMessage CreateResponse(
        HttpStatusCode statusCode,
        string content)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                content,
                Encoding.UTF8,
                "application/json")
        };
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } =
            new(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "ok": true
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };

        public int RequestCount { get; private set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public HttpContent? LastRequestContent { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            LastRequest = request;

            if (request.Content is not null)
            {
                var content = await request.Content.ReadAsStringAsync(
                    cancellationToken);

                LastRequestContent = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/json");
            }

            return Response;
        }
    }
}
