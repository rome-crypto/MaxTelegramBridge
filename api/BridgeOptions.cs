namespace MaxTelegramBridge.api;

public sealed class BridgeOptions
{
    public string MaxToken { get; set; } = string.Empty;

    public string MaxWebhookSecret { get; set; } = string.Empty;

    public string MaxWebhookUrl { get; set; } = string.Empty;

    public long MaxChatId { get; set; }

    public string TelegramToken { get; set; } = string.Empty;

    public long TelegramChatId { get; set; }
}