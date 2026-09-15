using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MaxTelegramBridge.api;

public sealed class ConfigurationStore
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly string _filePath;
    private readonly byte[] _key;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public ConfigurationStore(IConfiguration configuration)
    {
        _filePath =
            configuration["Bridge:ConfigFile"]
            ?? "/data/config.enc";

        var key = configuration["Bridge:EncryptionKey"];

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Bridge:EncryptionKey is not configured.");
        }

        try
        {
            _key = Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Bridge:EncryptionKey must be a Base64 string.");
        }

        if (_key.Length != KeySize)
        {
            throw new InvalidOperationException(
                "Bridge:EncryptionKey must contain exactly 32 bytes.");
        }
    }

    public async Task<BridgeOptions> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(_filePath))
            {
                return new BridgeOptions();
            }

            var encrypted =
                await File.ReadAllBytesAsync(
                    _filePath,
                    cancellationToken);

            return Decrypt(encrypted);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(
        BridgeOptions options,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            var directory =
                Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var encrypted = Encrypt(options);

            var temporaryFile =
                _filePath + ".tmp";

            await File.WriteAllBytesAsync(
                temporaryFile,
                encrypted,
                cancellationToken);

            File.Move(
                temporaryFile,
                _filePath,
                overwrite: true);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteAsync(
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private byte[] Encrypt(BridgeOptions options)
    {
        var json = JsonSerializer.Serialize(
            options,
            new JsonSerializerOptions
            {
                WriteIndented = false
            });

        var plaintext =
            Encoding.UTF8.GetBytes(json);

        var nonce = new byte[NonceSize];

        RandomNumberGenerator.Fill(nonce);

        var ciphertext =
            new byte[plaintext.Length];

        var tag =
            new byte[TagSize];

        using var aes =
            new AesGcm(_key, TagSize);

        aes.Encrypt(
            nonce,
            plaintext,
            ciphertext,
            tag);

        /*
         * Формат файла:

         * [nonce 12 bytes]
         * [tag   16 bytes]
         * [data  N bytes]
         */

        var result =
            new byte[
                NonceSize +
                TagSize +
                ciphertext.Length];

        Buffer.BlockCopy(
            nonce,
            0,
            result,
            0,
            NonceSize);

        Buffer.BlockCopy(
            tag,
            0,
            result,
            NonceSize,
            TagSize);

        Buffer.BlockCopy(
            ciphertext,
            0,
            result,
            NonceSize + TagSize,
            ciphertext.Length);

        return result;
    }

    private BridgeOptions Decrypt(byte[] encrypted)
    {
        if (encrypted.Length < NonceSize + TagSize)
        {
            throw new CryptographicException(
                "Encrypted configuration is corrupted.");
        }

        var nonce = encrypted[..NonceSize];

        var tag = encrypted[
            NonceSize..(NonceSize + TagSize)];

        var ciphertext = encrypted[
            (NonceSize + TagSize)..];

        var plaintext =
            new byte[ciphertext.Length];

        using var aes =
            new AesGcm(_key, TagSize);

        try
        {
            aes.Decrypt(
                nonce,
                ciphertext,
                tag,
                plaintext);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException(
                "Unable to decrypt configuration. " +
                "The encryption key may be incorrect " +
                "or the configuration is corrupted.");
        }

        var json =
            Encoding.UTF8.GetString(plaintext);

        return JsonSerializer.Deserialize<BridgeOptions>(
                   json)
               ?? new BridgeOptions();
    }
}