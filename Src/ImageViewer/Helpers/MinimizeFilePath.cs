using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Helpers;

public static class PathHelper
{
    /// <summary>
    /// Returns a shortened version of a file name so it fits within a maximum width.
    /// e.g. C:\Documents and Settings\User\Application Data\Microsoft\Word\custom.dic
    ///      would become something like: C:\...\Word\custom.dic
    /// </summary>
    /// <param name="fileName">The full file path.</param>
    /// <param name="fontFamily">The font family used for measuring text.</param>
    /// <param name="fontSize">The font size used for measuring text.</param>
    /// <param name="fontStyle">The font style used for measuring text.</param>
    /// <param name="fontWeight">The font weight used for measuring text.</param>
    /// <param name="maxWidth">The maximum width in device-independent pixels.</param>
    /// <param name="margin">The margin width in device-independent pixels.</param>
    /// <returns>The minimized file path string.</returns>
    public static string MinimizeName(string fileName, FontFamily fontFamily, double fontSize, FontStyle fontStyle, FontWeight fontWeight, double maxWidth, double margin)
    {
        // Helper function to measure text width using Avalonia's FormattedText
        static double MeasureTextWidth(string text, FontFamily font, double size, FontStyle style, FontWeight weight)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0.0;
            }

            var formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(font, style, weight),//Typeface.Default,//
                size,
                null);

            return formattedText.Width;
        }

        string fullPath = Path.GetFullPath(fileName);

        // If filename has no subdirectories, return it
        if (Path.GetDirectoryName(fullPath) == null || Path.GetDirectoryName(fullPath)?.Length == 0)
        {
            //Debug.WriteLine($"MinimizeName1 {fullPath}");
            return fullPath;
        }

        // If filename fits, no need to do anything
        if (MeasureTextWidth(fullPath, fontFamily, fontSize, fontStyle, fontWeight) <= (maxWidth - margin))
        {
            //Debug.WriteLine($"MinimizeName2 {fullPath}");
            return fullPath;
        }

        string? drive = Path.GetPathRoot(fullPath);
        string fn = Path.GetFileName(fullPath);
        string? dir = Path.GetDirectoryName(fullPath);

        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(drive) || dir == drive)
        {
            //Debug.WriteLine($"MinimizeName3 {fn}");
            return fn;
        }

        var dirParts = new List<string>(
            dir[drive.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], System.StringSplitOptions.RemoveEmptyEntries));

        string composedName;
        double textWidth;

        do
        {
            if (dirParts.Count > 0)
            {
                dirParts.RemoveAt(0);
            }

            string middle = dirParts.Count > 0 ? string.Join(Path.DirectorySeparatorChar.ToString(), dirParts) : "";

            composedName = Path.Combine(drive, "...", middle, fn);
            textWidth = MeasureTextWidth(composedName, fontFamily, fontSize, fontStyle, fontWeight);

        } while (dirParts.Count > 0 && textWidth > (maxWidth - margin));

        if (textWidth <= (maxWidth - margin))
        {
            //Debug.WriteLine($"MinimizeName4 {composedName}");
            return composedName;
        }
        else
        {
            //Debug.WriteLine($"MinimizeName5 {fn}");
            return fn;
        }
    }
}
