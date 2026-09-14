using System.Security.Cryptography;

namespace SmartX.API.Services;

public class FileService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    private readonly string[] _allowedExtensions =
    {
        ".json",
        ".txt",
        ".log",
        ".csv",
        ".xml",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public FileService(
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<(bool Success, string Message, string? FileName)>
        SaveEncryptedAsync(
            Guid sensorId,
            IFormFile file)
    {
        if (file == null || file.Length == 0)
            return (false, "No file was supplied.", null);

        var maxMb =
            _configuration
                .GetValue<int>("SmartX:MaxUploadSizeMB");

        if (file.Length > maxMb * 1024L * 1024L)
            return (
                false,
                $"File exceeds the {maxMb} MB limit.",
                null);

        var extension =
            Path.GetExtension(file.FileName)
                .ToLowerInvariant();

        if (!_allowedExtensions.Contains(extension))
            return (
                false,
                "This file type is not supported.",
                null);

        var root =
            Path.Combine(
                _environment.ContentRootPath,
                "Uploads",
                sensorId.ToString());

        Directory.CreateDirectory(root);

        var safeName =
            $"{Guid.NewGuid():N}{extension}.enc";

        var outputPath =
            Path.Combine(root, safeName);

        var key =
            Convert.FromBase64String(
                _configuration["SmartX:EncryptionKey"]!);

        using var aes = Aes.Create();

        aes.Key = key;

        aes.GenerateIV();

        await using var output =
            new FileStream(
                outputPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true);

        // Store IV at the beginning of the encrypted file.
        await output.WriteAsync(
            aes.IV,
            0,
            aes.IV.Length);

        await using var crypto =
            new CryptoStream(
                output,
                aes.CreateEncryptor(),
                CryptoStreamMode.Write);

        await file.CopyToAsync(crypto);

        await crypto.FlushAsync();

        return (
            true,
            "File uploaded and encrypted successfully.",
            safeName);
    }
}