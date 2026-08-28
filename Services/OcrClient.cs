using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

    /// <summary>
    /// 메시지를 보낼 때는 Camel case로 보낸다.
    /// </summary>
    private readonly JsonSerializerOptions jsonSendOption = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// 메시지를 받을 때는 대소문자를 따지지 않고 받는다.
    /// </summary>
    private readonly JsonSerializerOptions jsonRecvOption = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
        if (regions.Count == 0)
        {
            throw new ArgumentException("ROI의 개수가 0입니다.", nameof(regions));
        }

        using var content = new MultipartFormDataContent();

        if (imageStream.CanSeek)
        {
            imageStream.Position = 0;
        }

        var fileName = "image.webp";
        var imageBytes = await ReadAllBytesAsync(imageStream, cancellationToken);
        if (imageBytes.Length == 0)
        {
            throw new InvalidOperationException("불러온 이미지의 길이가 0 입니다.");
        }

        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/webp");
        content.Add(imageContent, "file", fileName);

        var regionsJson = JsonSerializer.Serialize(regions, jsonSendOption);

        var regionsContent = new StringContent(regionsJson, Encoding.UTF8, "application/json");
        content.Add(regionsContent, "zones_json");

        using var response = await _httpClient.PostAsync($"{_baseUrl}/vision/ocr-pipeline", content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"OCR 요청이 실패했습니다. : {(int)response.StatusCode}: {responseBody}");
        }

        return JsonSerializer.Deserialize<RoiResponse>(responseBody, jsonRecvOption);
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
