using System.Text.Json;

namespace MaxTelegramBridge.api;

public sealed class MaxClient
{
    private const string BaseUrl =
        "https://platform-api2.max.ru";

    private readonly HttpClient _http;
    private readonly ConfigurationStore _configurationStore;

    public MaxClient(
        HttpClient http,
        ConfigurationStore configurationStore)
    {
        _http = http;
        _configurationStore = configurationStore;
    }

    public async Task<JsonElement> SubscribeAsync(
        string webhookUrl,
        string secret,
        CancellationToken cancellationToken)
    {
        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(config.MaxToken))
        {
            throw new InvalidOperationException(
                "MAX bot token is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{BaseUrl}/subscriptions");

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            config.MaxToken);

        var payload = new
        {
            url = webhookUrl,

            update_types = new[]
            {
                "message_created"
            },

            secret
        };

        request.Content = JsonContent.Create(payload);

        using var response = await _http.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MAX error {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    public async Task<JsonElement> GetSubscriptionsAsync(
        CancellationToken cancellationToken)
    {
        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        if (string.IsNullOrWhiteSpace(config.MaxToken))
        {
            throw new InvalidOperationException(
                "MAX bot token is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{BaseUrl}/subscriptions");

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            config.MaxToken);

        using var response = await _http.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MAX error {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<JsonElement>(body);
    }

    public async Task<string> GetUpdatesAsync(
    CancellationToken cancellationToken)
    {
        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        Console.WriteLine(
    $"MAX token configured: {!string.IsNullOrWhiteSpace(config.MaxToken)}");

        Console.WriteLine(
            $"MAX token length: {config.MaxToken.Length}");

        if (string.IsNullOrWhiteSpace(config.MaxToken))
        {
            throw new InvalidOperationException(
                "MAX bot token is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{BaseUrl}/updates");

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            config.MaxToken);

        using var response = await _http.SendAsync(
            request,
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"MAX error {(int)response.StatusCode}: {body}");
        }

        return body;
    }
}