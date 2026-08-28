using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace PdkOcrClient.Services;

public static class ImageEncodingService
{
    public static Task<MemoryStream> ConvertBitmapToWebpAsync(Bitmap bitmap)
    {
        var pngStream = new MemoryStream();
        bitmap.Save(pngStream);
        pngStream.Position = 0;

        using var skBitmap = SKBitmap.Decode(pngStream) ?? throw new InvalidOperationException("이미지를 디코딩할 수 없습니다.");

        return Task.FromResult(EncodeWebp(skBitmap));
    }

    public static MemoryStream ConvertImageFileToWebpStream(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("경로가 비어있습니다.", nameof(imagePath));
        }

        using var skBitmap = SKBitmap.Decode(imagePath) ?? throw new InvalidOperationException("이미지를 디코딩할 수 없습니다.");

        return EncodeWebp(skBitmap);
    }

    private static MemoryStream EncodeWebp(SKBitmap bitmap)
    {
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new InvalidOperationException("이미지 크기가 올바르지 않습니다.");
        }

        var stream = new MemoryStream();
        var encoded = bitmap.Encode(stream, SKEncodedImageFormat.Webp, 100);
        if (!encoded || stream.Length == 0)
        {
            stream.Dispose();
            throw new InvalidOperationException("이미지를 WebP로 인코딩할 수 없습니다.");
        }

        stream.Position = 0;
        return stream;
    }
}
