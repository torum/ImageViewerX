using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace ImageViewer.Helpers;

public static class AvifImageDecoder
{
    /// <summary>
    /// Decodes an AVIF image from a stream using an explicit target Screen DPI.
    /// Thread-safe and optimized for background task threading.
    /// </summary>
    public static WriteableBitmap? DecodeAvifToAvalonia(Stream avifStream, Vector screenDpi)
    {
        ArgumentNullException.ThrowIfNull(avifStream);

        // 1. Decode the image into Skia
        using var skiaBitmap = SKBitmap.Decode(avifStream);
        if (skiaBitmap == null || skiaBitmap.IsEmpty)
        {
            System.Diagnostics.Debug.WriteLine("@DecodeAvifToAvalonia: Failed to decode AVIF image using SkiaSharp.");
            return null;
        }

        var pixelSize = new PixelSize(skiaBitmap.Width, skiaBitmap.Height);

        // 2. Initialize the Avalonia Bitmap container
        var avaloniaBitmap = new WriteableBitmap(
            pixelSize,
            screenDpi,
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        // 3. Fast unmanaged pixel blitting context
        using (var lockedBuffer = avaloniaBitmap.Lock())
        {
            // Calculate exact target byte size to prevent memory-corruption overflows
            int requiredBytes = lockedBuffer.RowBytes * pixelSize.Height;

            if (skiaBitmap.ColorType != SKColorType.Bgra8888)
            {
                // Allocate a temporary managed canvas configuration to handle layout conversion safely
                using var convertedBitmap = new SKBitmap();
                if (!skiaBitmap.CopyTo(convertedBitmap, SKColorType.Bgra8888))
                {
                    return null; // Format conversion failed
                }

                int byteCount = convertedBitmap.ByteCount;
                if (byteCount > requiredBytes) byteCount = requiredBytes; // Bound check guard

                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)convertedBitmap.GetPixels(),
                        (void*)lockedBuffer.Address,
                        requiredBytes,
                        byteCount);
                }
            }
            else
            {
                int byteCount = skiaBitmap.ByteCount;
                if (byteCount > requiredBytes) byteCount = requiredBytes; // Bound check guard

                unsafe
                {
                    Buffer.MemoryCopy(
                        (void*)skiaBitmap.GetPixels(),
                        (void*)lockedBuffer.Address,
                        requiredBytes,
                        byteCount);
                }
            }
        }

        return avaloniaBitmap;
    }
}


