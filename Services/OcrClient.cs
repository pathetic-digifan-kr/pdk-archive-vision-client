using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PdkOcrClient.Models;

namespace PdkOcrClient.Services;

public class OcrClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OcrClient(string? baseUrl = null, HttpClient? httpClient = null)
    {
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? "http://127.0.0.1:8000"
            : baseUrl.TrimEnd('/');

        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(60)
        };
    }

    public async Task<RoiResponse?> SendOcrRequestAsync(
        Stream imageStream,
        IReadOnlyList<RoiModel> regions,
        CancellationToken cancellationToken = default)
    {
        if (imageStream is null)
        {
            throw new ArgumentNullException(nameof(imageStream));
        }

        if (regions is null)
        {
            throw new ArgumentNullException(nameof(regions));
        }

        using var content = new MultipartFormDataContent();

        imageStream.Position = 0;
        var fileName = "image.webp";
        var imageBytes = await ReadAllBytesAsync(imageStream, cancellationToken);
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        content.Add(imageContent, "file", fileName);

        var regionsJson = JsonSerializer.Serialize(regions, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var regionsContent = new StringContent(regionsJson, Encoding.UTF8, "application/json");
        content.Add(regionsContent, "zones_json");

        using var response = await _httpClient.PostAsync($"{_baseUrl}/vision/ocr-pipeline", content, cancellationToken);
        var responseBody = await response.Content.ReadFromJsonAsync<RoiResponse>(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OCR request failed with status {(int)response.StatusCode}: {responseBody}");
        }

        return responseBody;
    }

    public async Task<RoiResponse?> SendOcrRequestAsync(
        string imagePath,
        IReadOnlyList<RoiModel> regions,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(imagePath));
        }

        await using var stream = File.OpenRead(imagePath);
        return await SendOcrRequestAsync(stream, regions, cancellationToken);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return memoryStream.ToArray();
    }
}