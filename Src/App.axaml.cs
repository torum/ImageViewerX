using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ImageViewer.Services;
using ImageViewer.Services.Contracts;
using ImageViewer.ViewModels;
using ImageViewer.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageViewer;

internal sealed partial class App : Application
{
    public const string AppName = "ImageViewer2";
    private const string AppDeveloper = "torum";

    // Log file.
    private static readonly StringBuilder _errortxt = new();
    private static readonly Lock _logLock = new();
    private static readonly string _logFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + AppName + "_errors.txt";

    // Data folder and Config file path.
    private static readonly string _envDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string AppDataFolder { get; } = System.IO.Path.Combine(System.IO.Path.Combine(_envDataFolder, AppDeveloper), AppName);
    public static string AppConfigFilePath { get; } = System.IO.Path.Combine(AppDataFolder, AppName + ".config");

    public App()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainView>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Build the provider
        ServiceProvider = services.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; private set; } = null!;

    public static T GetService<T>() where T : class
    {
        return (App.Current as App)!.ServiceProvider.GetRequiredService<T>();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async override void OnFrameworkInitializationCompleted()
    {
        // Testing only
        //Assets.Resources.Culture = new CultureInfo("ja-JP");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //Dispatcher.UIThread.UnhandledException += OnUnhandledException;
            RegisterGlobalExceptionHandlers();

            // Set custom font for non-Windows platforms.
            if (Application.Current != null)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Application.Current.Resources["ContentControlThemeFontFamily"] = new FontFamily("fonts:Inter#Inter");
                }
            }

            var config = ServiceProvider.GetRequiredService<IConfigurationService>();
            config.Initialize(AppConfigFilePath);
            await config.LoadConfigurationAsync();

            var mainWin = App.GetService<MainWindow>();

            // get Stdin, set it for parsing in MainWindow.
            var args = desktop.Args;
            if ((args is not null) && (args.Length > 0))
            {
                mainWin.SetStdin(args);
            }

            desktop.MainWindow = mainWin;
            // Don't do Show() in Avalonia UI. Let the framework handle it.
            //desktop.MainWindow.ShowActivated = true;
            //desktop.MainWindow.Show();

            //desktop.Exit += OnDesktopExit;
            //_appHostStartTask = StartAppHostAsync();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = App.GetService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /*
    private async Task StartAppHostAsync()
    {
        try
        {
            await AppHost.StartAsync();
        }
        catch (Exception ex)
        {
            AppendErrorLog("AppHost.StartAsync", ex.ToString());
            SaveErrorLog();
        }
    }

    private async void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            if (_appHostStartTask is not null)
            {
                await _appHostStartTask;
            }

            await AppHost.StopAsync();
        }
        finally
        {
            AppHost.Dispose();
        }
    }
    */

    private void RegisterGlobalExceptionHandlers()
    {
        // UI thread.
        Dispatcher.UIThread.UnhandledException += OnUnhandledException;

        // Any thread, process is about to terminate.
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        // Faulted tasks nobody awaited.
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Cancellation is normal (e.g. shutdown), not an error.
        if (e.Exception is OperationCanceledException)
        {
            e.Handled = true;
            return;
        }

        System.Diagnostics.Debug.WriteLine($"An unhandled exception occurred: {e.Exception}");
        AppendErrorLog("DispatcherUnhandledException", e.Exception.ToString());
        SaveErrorLog();

        // Keep the app alive for recoverable errors, but never for out-of-memory.
        e.Handled = e.Exception is not OutOfMemoryException;
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        AppendErrorLog("AppDomain.UnhandledException", e.ExceptionObject?.ToString() ?? "(null)");
        SaveErrorLog(); // Last chance: the process terminates after this.
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (e.Exception.InnerExceptions.All(ex => ex is OperationCanceledException))
        {
            e.SetObserved();
            return;
        }

        AppendErrorLog("TaskScheduler.UnobservedTaskException", e.Exception.ToString());
        SaveErrorLog();
        e.SetObserved();
    }

    public static void AppendErrorLog(string source, string details)
    {
        var nowString = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

        lock (_logLock)
        {
            _errortxt.AppendLine(nowString + " - " + source + " - " + details);
        }
    }

    public static void SaveErrorLog()
    {
        if (!App.GetService<MainViewModel>()?.IsSaveLog ?? false)
            return;

        if (string.IsNullOrEmpty(_logFilePath))
            return;

        try
        {
            lock (_logLock)
            {
                if (_errortxt.Length == 0)
                {
                    return;
                }

                Directory.CreateDirectory(AppDataFolder);
                File.WriteAllText(_logFilePath, _errortxt.ToString());
            }
        }
        catch (Exception ex)
        {
            // Logging must never take the app down.
            System.Diagnostics.Debug.WriteLine($"Could not save error log: {ex.Message}");
        }
    }
    /*
    // Log file.
    private static readonly StringBuilder Errortxt = new();
    private static readonly string LogFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + AppName + "_errors.txt";

    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Well, no -> Prevent the application from crashing
        //e.Handled = true;

        // Log the exception for debugging
        AppendErrorLog("DispatcherUnhandledException", e.Exception.ToString());

        // Don't save logs unless IsSaveLog is true in vm.
        SaveErrorLog();
    }

    public static void AppendErrorLog(string errorTxt, string detailedErrorMessageTxt)
    {
        var dt = DateTime.Now;
        var nowString = dt.ToString("yyyy/MM/dd HH:mm:ss");

        Errortxt.AppendLine(nowString + " - " + errorTxt + " - " + detailedErrorMessageTxt + Environment.NewLine);
    }

    public static void SaveErrorLog()
    {
        if (!App.GetService<MainViewModel>().IsSaveLog)
            return;

        if (string.IsNullOrEmpty(LogFilePath))
            return;

        var s = Errortxt.ToString();
        if (!string.IsNullOrEmpty(s))
            File.WriteAllText(LogFilePath, s);
    }
    */
}