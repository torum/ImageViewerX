using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ImageViewer.ViewModels;
using ImageViewer.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ImageViewer;

public partial class App : Application
{
    public static readonly string AppName = "ImageViewer2";
    private static readonly string _appDeveloper = "torum";

    // Data folder and Config file path.
    private static readonly string _envDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string AppDataFolder { get; } = System.IO.Path.Combine(System.IO.Path.Combine(_envDataFolder, _appDeveloper), AppName);
    public static string AppConfigFilePath { get; } = System.IO.Path.Combine(AppDataFolder, AppName + ".config");

    public IHost AppHost { get; private set; }

    public App()
    {
        AppHost = Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainView>();
                    services.AddSingleton<MainViewModel>();
                })
                .Build();
    }

    public static T GetService<T>() where T : class
    {
        if ((App.Current as App)!.AppHost!.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Testing only
        //Assets.Resources.Culture = new CultureInfo("ja-JP");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.UnhandledException += OnUnhandledException;

            /*
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            */
            var mainWin = App.GetService<MainWindow>();

            // get Stdin, set it for parsing in MainWindow.
            var args = desktop.Args;
            if ((args is not null) && (args.Length > 0))
            {
                mainWin.SetStdin(args);
            }

            desktop.MainWindow = mainWin;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Log file.
    private static readonly StringBuilder _errortxt = new();
    private static readonly string _logFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + AppName + "_errors.txt";

    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Prevent the application from crashing
        e.Handled = true;

        // Log the exception for debugging
        AppendErrorLog("DispatcherUnhandledException", e.Exception.ToString());

        // Don't save logs unless IsSaveLog is true in vm.
        //SaveErrorLog();
    }

    public static void AppendErrorLog(string errorTxt, string detailedErrorMessageTxt)
    {
        var dt = DateTime.Now;
        var nowString = dt.ToString("yyyy/MM/dd HH:mm:ss");

        _errortxt.AppendLine(nowString + " - " + errorTxt + " - " + detailedErrorMessageTxt);
    }

    public static void SaveErrorLog()
    {
        if (string.IsNullOrEmpty(_logFilePath))
            return;

        var s = _errortxt.ToString();
        if (!string.IsNullOrEmpty(s))
            File.WriteAllText(_logFilePath, s);
    }
}
