using System.Text.Json;

namespace MaxTelegramBridge.api;

public sealed class TelegramClient
{
    private readonly HttpClient _http;
    private readonly ConfigurationStore _configurationStore;

    public TelegramClient(
        HttpClient http,
        ConfigurationStore configurationStore)
    {
        _http = http;
        _configurationStore = configurationStore;
    }

    public async Task SendMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken)
    {
        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(config.TelegramToken))
        {
            throw new InvalidOperationException(
                "Telegram bot token is not configured.");
        }

        if (chatId == 0)
        {
            throw new InvalidOperationException(
                "Telegram chat ID is not configured.");
        }

        foreach (var chunk in SplitText(text, 4096))
        {
            var url =
                $"https://api.telegram.org/" +
                $"bot{config.TelegramToken}/sendMessage";

            var payload = new
            {
                chat_id = chatId,
                text = chunk
            };

            using var response = await _http.PostAsJsonAsync(
                url,
                payload,
                cancellationToken);

            var body = await response.Content.ReadAsStringAsync(
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Telegram error {(int)response.StatusCode}: {body}");
            }
        }
    }

    public async Task<bool> TestAsync(
        CancellationToken cancellationToken)
    {
        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(config.TelegramToken))
        {
            throw new InvalidOperationException(
                "Telegram bot token is not configured.");
        }

        var url =
            $"https://api.telegram.org/" +
            $"bot{config.TelegramToken}/getMe";

        using var response = await _http.GetAsync(
            url,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    public async Task<string> GetUpdatesAsync(
        CancellationToken cancellationToken)
    {
        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(config.TelegramToken))
        {
            throw new InvalidOperationException(
                "Telegram bot token is not configured.");
        }

        var url =
            $"https://api.telegram.org/" +
            $"bot{config.TelegramToken}/getUpdates";

        using var response = await _http.GetAsync(
            url,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Telegram error {(int)response.StatusCode}: {body}");
        }

        return body;
    }

    private static IEnumerable<string> SplitText(
        string text,
        int maxLength)
    {
        for (var i = 0; i < text.Length; i += maxLength)
        {
            var length = Math.Min(
                maxLength,
                text.Length - i);

            yield return text.Substring(i, length);
        }
    }
}