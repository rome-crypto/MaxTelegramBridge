using System.Text.Json;

namespace MaxTelegramBridge.api;

public sealed class MaxUpdate
{
    public string? UpdateType { get; set; }

    public long ChatId { get; set; }

    public long Timestamp { get; set; }

    public MaxMessage? Message { get; set; }
}

public sealed class MaxMessage
{
    public MaxUser? Sender { get; set; }

    public MaxRecipient? Recipient { get; set; }

    public long Timestamp { get; set; }

    public MaxMessageBody? Body { get; set; }

    public MaxLinkedMessage? Link { get; set; }

    public string? Mid { get; set; }
}

public sealed class MaxUser
{
    public long UserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Username { get; set; }

    public bool IsBot { get; set; }
}

public sealed class MaxRecipient
{
    public long ChatId { get; set; }

    public string? ChatType { get; set; }
}

public sealed class MaxMessageBody
{
    public string? Text { get; set; }

    public List<MaxAttachment>? Attachments { get; set; }
}

public sealed class MaxAttachment
{
    public string? Type { get; set; }

    public JsonElement Payload { get; set; }
}

public sealed class MaxLinkedMessage
{
    public string? Type { get; set; }

    public string? Mid { get; set; }
}