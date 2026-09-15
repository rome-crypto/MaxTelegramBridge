using System.Security.Cryptography;
using System.Text;
using MaxTelegramBridge.api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ConfigurationStore>();

builder.Services.AddHttpClient<MaxClient>();
builder.Services.AddHttpClient<TelegramClient>();

builder.Services.AddSingleton<BridgeService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "MaxTelegramBridge",
    status = "ok"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok"
}));

app.MapPost("/webhook", async (
    HttpRequest request,
    BridgeService bridge,
    ConfigurationStore store,
    CancellationToken cancellationToken) =>
{
    var configuration = await store.LoadAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(configuration.MaxWebhookSecret))
    {
        return Results.Unauthorized();
    }

    if (!request.Headers.TryGetValue(
            "X-Max-Bot-Api-Secret",
            out var receivedSecret))
    {
        return Results.Unauthorized();
    }

    var expectedBytes =
        Encoding.UTF8.GetBytes(configuration.MaxWebhookSecret);

    var receivedBytes =
        Encoding.UTF8.GetBytes(receivedSecret.ToString());

    if (!CryptographicOperations.FixedTimeEquals(
            expectedBytes,
            receivedBytes))
    {
        return Results.Unauthorized();
    }

    MaxUpdate? update;

    try
    {
        update = await request.ReadFromJsonAsync<MaxUpdate>(
            cancellationToken);
    }
    catch (Exception)
    {
        return Results.BadRequest();
    }

    if (update is null)
    {
        return Results.BadRequest();
    }

    await bridge.HandleAsync(
        update,
        cancellationToken);

    return Results.Ok();
});

app.MapPost("/admin/config", async (
    HttpRequest request,
    ConfigureRequest configureRequest,
    ConfigurationStore store,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    var adminSecret = configuration["Bridge:AdminSecret"];

    if (string.IsNullOrWhiteSpace(adminSecret))
    {
        return Results.Problem(
            "Bridge:AdminSecret is not configured.",
            statusCode: StatusCodes.Status500InternalServerError);
    }

    if (!request.Headers.TryGetValue(
            "X-Admin-Secret",
            out var receivedSecret))
    {
        return Results.Unauthorized();
    }

    var expectedBytes =
        Encoding.UTF8.GetBytes(adminSecret);

    var receivedBytes =
        Encoding.UTF8.GetBytes(receivedSecret.ToString());

    if (!CryptographicOperations.FixedTimeEquals(
            expectedBytes,
            receivedBytes))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(configureRequest.MaxToken) ||
        string.IsNullOrWhiteSpace(configureRequest.TelegramToken))
    {
        return Results.BadRequest(new
        {
            error = "MAX and Telegram tokens are required."
        });
    }

    if (string.IsNullOrWhiteSpace(
            configureRequest.MaxWebhookSecret))
    {
        return Results.BadRequest(new
        {
            error = "MAX webhook secret is required."
        });
    }

    if (string.IsNullOrWhiteSpace(
            configureRequest.MaxWebhookUrl))
    {
        return Results.BadRequest(new
        {
            error = "MAX webhook URL is required."
        });
    }

    var options = new BridgeOptions
    {
        MaxToken = configureRequest.MaxToken.Trim(),
        MaxWebhookSecret =
            configureRequest.MaxWebhookSecret.Trim(),
        MaxWebhookUrl =
            configureRequest.MaxWebhookUrl.Trim(),
        MaxChatId = configureRequest.MaxChatId,

        TelegramToken =
            configureRequest.TelegramToken.Trim(),
        TelegramChatId =
            configureRequest.TelegramChatId
    };

    await store.SaveAsync(
        options,
        cancellationToken);

    return Results.Ok(new
    {
        status = "saved"
    });
});

app.MapGet("/admin/status", async (
    HttpRequest request,
    ConfigurationStore store,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminAuthorized(request, configuration))
    {
        return Results.Unauthorized();
    }

    var options =
        await store.LoadAsync(cancellationToken);

    return Results.Ok(new
    {
        maxConfigured =
            !string.IsNullOrWhiteSpace(options.MaxToken),

        maxChatConfigured =
            options.MaxChatId != 0,

        webhookConfigured =
            !string.IsNullOrWhiteSpace(
                options.MaxWebhookUrl),

        telegramConfigured =
            !string.IsNullOrWhiteSpace(
                options.TelegramToken),

        telegramChatConfigured =
            options.TelegramChatId != 0
    });
});

app.MapPost("/admin/subscribe", async (
    HttpRequest request,
    MaxClient maxClient,
    ConfigurationStore store,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminAuthorized(request, configuration))
    {
        return Results.Unauthorized();
    }

    var config = await store.LoadAsync(cancellationToken);

    if (string.IsNullOrWhiteSpace(config.MaxWebhookUrl))
    {
        return Results.BadRequest(new
        {
            error = "MAX webhook URL is not configured."
        });
    }

    if (string.IsNullOrWhiteSpace(config.MaxWebhookSecret))
    {
        return Results.BadRequest(new
        {
            error = "MAX webhook secret is not configured."
        });
    }

    try
    {
        await maxClient.SubscribeAsync(
            config.MaxWebhookUrl,
            config.MaxWebhookSecret,
            cancellationToken);

        return Results.Ok(new
        {
            status = "subscribed"
        });
    }
    catch (Exception exception)
    {
        return Results.Problem(
            exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapPost("/admin/test-telegram", async (
    HttpRequest request,
    TelegramClient telegramClient,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminAuthorized(request, configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        await telegramClient.TestAsync(cancellationToken);

        return Results.Ok(new
        {
            status = "ok"
        });
    }
    catch (Exception exception)
    {
        return Results.Problem(
            exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapGet("/admin/telegram-updates", async (
    HttpRequest request,
    TelegramClient telegramClient,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminAuthorized(request, configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        var updates = await telegramClient.GetUpdatesAsync(
            cancellationToken);

        return Results.Content(
            updates,
            "application/json");
    }
    catch (Exception exception)
    {
        return Results.Problem(
            exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapGet("/admin/max-updates", async (
    HttpRequest request,
    MaxClient maxClient,
    IConfiguration configuration,
    CancellationToken cancellationToken) =>
{
    if (!IsAdminAuthorized(request, configuration))
    {
        return Results.Unauthorized();
    }

    try
    {
        var updates = await maxClient.GetUpdatesAsync(
            cancellationToken);

        return Results.Content(
            updates,
            "application/json");
    }
    catch (Exception exception)
    {
        return Results.Problem(
            exception.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
});

app.Run();

static bool IsAdminAuthorized(
    HttpRequest request,
    IConfiguration configuration)
{
    var adminSecret =
        configuration["Bridge:AdminSecret"];

    if (string.IsNullOrWhiteSpace(adminSecret))
    {
        return false;
    }

    if (!request.Headers.TryGetValue(
            "X-Admin-Secret",
            out var receivedSecret))
    {
        return false;
    }

    var expectedBytes =
        Encoding.UTF8.GetBytes(adminSecret);

    var receivedBytes =
        Encoding.UTF8.GetBytes(receivedSecret.ToString());

    return CryptographicOperations.FixedTimeEquals(
        expectedBytes,
        receivedBytes);
}