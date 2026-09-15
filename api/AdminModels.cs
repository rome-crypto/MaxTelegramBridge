namespace MaxTelegramBridge.api;

public sealed class ConfigureRequest
{
    public string MaxToken { get; set; } = string.Empty;

    public string MaxWebhookSecret { get; set; } = string.Empty;

    public string MaxWebhookUrl { get; set; } = string.Empty;

    public long MaxChatId { get; set; }

    public string TelegramToken { get; set; } = string.Empty;

    public long TelegramChatId { get; set; }
}

public sealed class AdminRequest
{
    public string Secret { get; set; } = string.Empty;
}