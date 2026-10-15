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

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions
            {
                // Tries GPU rendering options first, automatically falls back to Software if they fail
                RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Software]
            })
            .UseWaylandWithFallback()
            .With(new WaylandPlatformOptions()
            {
                AppId = "ImageViewerX",  // Matches your .desktop launcher file
                UseDmabufSwapchain = true,     // Enables high-performance rendering
                UseGLibMainLoop = true         // Integrates with Linux desktop loops
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
    }
}
