using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia;

namespace ImageViewer.Helpers;

public static class UniversalBitmapLoader
{
    public static Bitmap? LoadAnyImage(string filePath, Vector displayDpi)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found.", filePath);

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        // 1. Native Avalonia Route (JPEG, PNG, WebP, GIF, BMP)
        if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".bmp" || extension == ".webp")
        {
            return new Bitmap(filePath);
        }

        // SkiaSharp Route (AVIF)
        if (extension == ".avif")
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            return AvifImageDecoder.DecodeAvifToAvalonia(fileStream, displayDpi);

        }
        /*
        //  (JPEG XL / .jxl)
        if (extension == ".jxl")
        {
            return DecodeJxlToAvalonia(filePath);
        }
        */

        throw new NotSupportedException($"The extension {extension} is not supported.");
    }
}