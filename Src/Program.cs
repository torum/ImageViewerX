using Avalonia;
using System;

namespace ImageViewer
{
    internal sealed class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Suppress the experimental warning required for ForceDrawnDecorations
#pragma warning disable AVALONIA_WAYLAND_FORCE_CSD
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseWaylandWithFallback()
            .With(new WaylandPlatformOptions()
            {
                AppId = "ImageViewerX",  // Matches your .desktop launcher file
                UseDmabufSwapchain = null,     // Enables high-performance rendering
                UseGLibMainLoop = false,         // Integrates with Linux desktop loops
                ForceDrawnDecorations = true    // Enables custom window decorations especially for KDE environments where they prefer server-side decorations.
            })
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 256 * 1024 * 1024 // Set cache to 256MB. Without this, there are issues with large images.
            })
            .WithInterFont();
            //.With(new AvaloniaNativePlatformOptions { OverlayPopups = true })
            //.With(new Win32PlatformOptions { OverlayPopups = true })
            //.With(new X11PlatformOptions { OverlayPopups = true })
            //.LogToTrace();
#pragma warning restore AVALONIA_WAYLAND_FORCE_CSD
    }
}
