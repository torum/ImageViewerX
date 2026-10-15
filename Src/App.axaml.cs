using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ImageViewer.ViewModels;
using ImageViewer.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer;

internal sealed partial class App : Application
{
    public const string AppName = "ImageViewer2";
    private const string AppDeveloper = "torum";

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

    public override void OnFrameworkInitializationCompleted()
    {
        // Testing only
        //Assets.Resources.Culture = new CultureInfo("ja-JP");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.UnhandledException += OnUnhandledException;

            // Set custom font for non-Windows platforms.
            if (Application.Current != null)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Application.Current.Resources["ContentControlThemeFontFamily"] = new FontFamily("fonts:Inter#Inter");
                }
            }

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
                DataContext = new MainViewModel()
            };
        }

        /*
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }
        */
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
}