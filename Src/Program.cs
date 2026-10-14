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
                .With(new SkiaOptions
                {
                    MaxGpuResourceSizeBytes = 256 * 1024 * 1024 // Set cache to 256MB
                })
                .WithInterFont();
                //.With(new AvaloniaNativePlatformOptions { OverlayPopups = true })
                //.With(new Win32PlatformOptions { OverlayPopups = true })
                //.With(new X11PlatformOptions { OverlayPopups = true })
                //.LogToTrace();
    }
}
