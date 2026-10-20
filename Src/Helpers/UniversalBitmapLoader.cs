using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia;

namespace ImageViewer.Helpers;

public static class UniversalBitmapLoader
{
    public static Bitmap? LoadAnyImage(string filePath, Vector displayDpi, int hardwareDisplayWidth)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException("Image file not found.", filePath);

        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        // 1. Native Avalonia Route (JPEG, PNG, WebP, GIF, BMP)
        if (extension == ".png" || extension == ".jpg" || extension == ".jpeg" || extension == ".gif" || extension == ".bmp" || extension == ".webp")
        {
            //return new Bitmap(filePath);

            Bitmap finalBitmap;

            // Open the file stream once
            using (var stream = File.OpenRead(filePath))
            {
                // First, instantiate a standard Bitmap context. 
                // This reads only metadata info or maps the layout array without fully uncompressing.
                var nativeMetaInfo = new Bitmap(stream);

                // Check if the image width is physically larger than your active screen resolution
                if (nativeMetaInfo.Size.Width > hardwareDisplayWidth)
                {
                    // LARGE IMAGE DETECTED -> Downscale to match display width smoothly
                    // We reset the stream index position to read it cleanly from the beginning
                    stream.Seek(0, SeekOrigin.Begin);
                    finalBitmap = Bitmap.DecodeToWidth(stream, hardwareDisplayWidth);

                    // Dispose of the initial placeholder data now that we have our optimized copy
                    nativeMetaInfo.Dispose();
                }
                else
                {
                    // SMALL IMAGE DETECTED -> Keep original size to preserve quality
                    // Since nativeMetaInfo already holds the image at 100% true layout scale, use it!
                    finalBitmap = nativeMetaInfo;
                }
            }

            return finalBitmap;
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