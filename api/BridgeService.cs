namespace MaxTelegramBridge.api;

public sealed class BridgeService
{
    private readonly ConfigurationStore _configurationStore;
    private readonly TelegramClient _telegramClient;
    private readonly ILogger<BridgeService> _logger;

    public BridgeService(
        ConfigurationStore configurationStore,
        TelegramClient telegramClient,
        ILogger<BridgeService> logger)
    {
        _configurationStore = configurationStore;
        _telegramClient = telegramClient;
        _logger = logger;
    }

    public async Task HandleAsync(
        MaxUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
                update.UpdateType,
                "message_created",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (update.Message is null)
        {
            return;
        }

        var config = await _configurationStore.LoadAsync(
            cancellationToken);

        // Если настроен конкретный MAX-чат,
        // принимаем сообщения только из него.
        if (config.MaxChatId != 0 &&
            update.Message.Recipient?.ChatId != config.MaxChatId)
        {
            return;
        }

        var text = update.Message.Body?.Text;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        _logger.LogInformation(
            "Received message from MAX chat {ChatId}.",
            update.Message.Recipient?.ChatId);

        await _telegramClient.SendMessageAsync(
            config.TelegramChatId,
            text,
            cancellationToken);
    }
}