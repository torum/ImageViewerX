using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ImageViewer.Helpers;
using ImageViewer.Models;
using ImageViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace ImageViewer.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timerPointerCursorHide;
    private Avalonia.Point _mousePosition;
    private readonly MainViewModel _mainViewModel;
    private string _lastOpenedDirectory = string.Empty;
    private bool _isFullyLoaded;
    // Window position and size
    private int _winRestoreWidth = 1024;
    private int _winRestoreHeight = 768;
    private int _winRestoreTop = 100;
    private int _winRestoreLeft = 100;

    private Avalonia.Platform.Screen? _currentScreen;
    private double _systemDpiScalingFactor = 1;

#pragma warning disable CS8618 
    public MainWindow() { }
#pragma warning restore CS8618

    public MainWindow(MainViewModel vm)
    {
        //_mainViewModel = App.GetService<MainViewModel>();
        _mainViewModel = vm;
        this.DataContext = vm;

        LoadSettings();

        InitializeComponent();

        UpdateThemeBackground(ActualThemeVariant);

        // Moved to Window_Loaded.
        //this.ContentFrame.Content = App.GetService<MainView>();

        // There is some issue showing sysmenu in AvaloniaUI.
        //TryRegisterWindowsMenu();

        this.PropertyChanged += this.OnWindow_PropertyChanged;
        this.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _mainViewModel.QueueHasBeenChanged += OnQueueHasBeenChanged;
        _mainViewModel.SlideshowStatusChanged += OnSlideshowStatusChanged;
        _mainViewModel.QueueLoaded += OnQueueLoaded;
        _mainViewModel.UpdateFullscreenState += (sender, arg) => { this.OnUpdateFullscreenState(arg); };
        _mainViewModel.HideMenuFlyout += OnHideMenuFlyout;
        _mainViewModel.SlideshowIntervalChanged += (sender, arg) => { this.OnSlideshowIntervalChanged(arg); };

        this.DetachedFromVisualTree += (s, e) =>
        {
            this.PropertyChanged -= this.OnWindow_PropertyChanged;
            this.ActualThemeVariantChanged -= OnActualThemeVariantChanged;
            _mainViewModel.QueueHasBeenChanged -= OnQueueHasBeenChanged;
            _mainViewModel.SlideshowStatusChanged -= OnSlideshowStatusChanged;
            _mainViewModel.QueueLoaded -= OnQueueLoaded;
            _mainViewModel.UpdateFullscreenState -= (sender, arg) => { this.OnUpdateFullscreenState(arg); };
            _mainViewModel.HideMenuFlyout -= OnHideMenuFlyout;
            _mainViewModel.SlideshowIntervalChanged -= (sender, arg) => { this.OnSlideshowIntervalChanged(arg); };
        };

        _timerPointerCursorHide = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _timerPointerCursorHide.Tick += OnPointerCursorHideTimerTick;

        // TODO: more
        InitKeyBindigs();

    }

    public async void SetStdin(string[] args)
    {
        if ((args is not null) && args.Length > 0)
        {
            // Too early?
            this.WelcomeMessageGrid.IsVisible = false;

            //await ProcessFiles([.. args]);
            await Task.Run(() => ProcessFiles([.. args]));
        }
        else
        {
            //_mainViewModel.IsWorking = false;
        }
    }

    private void OnQueueLoaded(object? sender, EventArgs e)
    {
        this.WelcomeMessageGrid.IsVisible = false;
    }

    private void OnPointerCursorHideTimerTick(object? sender, EventArgs e)
    {
        // This code runs on the UI thread, so it's safe to update UI elements.

        var flyout = FlyoutBase.GetAttachedFlyout(this);
        if (flyout is not null)
        {
            if (flyout.IsOpen)
            {
                return;
            }
        }

        // TODO: Is there any way to determine current cursor type?
        // Creating new cursor every time is weird.
        this.Cursor = new Cursor(StandardCursorType.None);
    }

    private void OnQueueHasBeenChanged(object? sender, int ind)
    {
        UpdateQueueListBoxImages(ind);
    }

    private void OnSlideshowStatusChanged(object? sender, EventArgs e)
    {
        if (this.WindowState != WindowState.FullScreen)
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (_mainViewModel.IsSlideshowOn)
            {
                Debug.WriteLine("SetThreadExecutionState set @OnSlideshowStatusChanged");
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);
            }
            else
            {
                Debug.WriteLine("SetThreadExecutionState off @OnSlideshowStatusChanged");
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            }
        }
    }

    private void OnWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is not Window)
        {
            return;
        }
        if (e.Property.Name != nameof(WindowState))
        {
            return;
        }

        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        // Let vm know WindowState changed.

        if (e.NewValue is WindowState.FullScreen)
        {
            Debug.WriteLine($"WindowState changed from {e.OldValue} to {e.NewValue}");
            vm.IsFullscreen = true;
        }
        else
        {
            vm.IsFullscreen = false;
        }
    }

    private void UpdateThemeBackground(ThemeVariant theme)
    {
        //(App.Current as App)!.RequestedThemeVariant
        //ActualThemeVariant

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            this.BackgroundLayerBorder.IsVisible = false;
            this.BackgroundLayerBorder.Opacity = 0.9;

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 1803))
            {
                // Get the window's platform handle
                var handle = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    this.Background = Brushes.Transparent;
                    this.TransparencyLevelHint = [WindowTransparencyLevel.None];

                    //this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                    EnableBlurBehind(handle);
                }
                else
                {
                    this.Background = Brushes.Transparent;
                    this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                    //this.TransparencyLevelHint = [WindowTransparencyLevel.Mica];
                }
            }
            else
            {
                this.Background = Brushes.Transparent;
                this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                //this.TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (theme == ThemeVariant.Dark)
            {
                //this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#121212"));
            }
            else if (theme == ThemeVariant.Light)
            {
                //this.TransparencyLevelHint = [WindowTransparencyLevel.Blur];
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#EEEEEE"));
            }
            else if (theme == ThemeVariant.Default)
            {
                //this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#121212"));
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                this.YoImageHereTextBlock.Text = "Sorry, Drag & Drop is not currently supported on thie platform.";
                this.DragDropTextBlock.TextDecorations = TextDecorations.Strikethrough;
            }
        }
        else
        {
            this.BackgroundLayerBorder.IsVisible = true;
            this.BackgroundLayerBorder.Opacity = 0.9;

            if (theme == ThemeVariant.Dark)
            {
                this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#222222"));
            }
            else if (theme == ThemeVariant.Light)
            {
                this.TransparencyLevelHint = [WindowTransparencyLevel.Blur];
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }
            else if (theme == ThemeVariant.Default)
            {
                this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#121212"));
            }
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        UpdateThemeBackground(ActualThemeVariant);
    }

    public void OnUpdateFullscreenState(bool on)
    {
        //WindowState = (WindowState == WindowState.FullScreen) ? WindowState.Normal : WindowState.FullScreen;

        if (on)
        {
            if (WindowState == WindowState.Normal)
            {
                SetWindowStateFullScreen();
            }
        }
        else
        {
            if (WindowState == WindowState.FullScreen)
            {
                SetWindowStateNormal();
            }
        }
    }

    public void OnSlideshowIntervalChanged(long interval)
    {
        this.Interval1SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval2SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval3SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval4SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval5SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval6SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval7SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval8SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval9SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval10SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval15SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval20SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval30SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval60SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval120SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval180SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval300SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;
        this.Interval600SecIconData.Data = _mainViewModel.DataUnCheckedCircleIcon;

        if (interval <= 1) 
        {
            this.Interval1SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 2) 
        {
            this.Interval2SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 3)
        {
            this.Interval3SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 4)
        {
            this.Interval4SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 5)
        {
            this.Interval5SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 6)
        {
            this.Interval6SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 7)
        {
            this.Interval7SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 8)
        {
            this.Interval8SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 9)
        {
            this.Interval9SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 10)
        {
            this.Interval10SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 15)
        {
            this.Interval15SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 20)
        {
            this.Interval20SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 30)
        {
            this.Interval30SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 60)
        {
            this.Interval60SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 120)
        {
            this.Interval120SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 180)
        {
            this.Interval180SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval == 300)
        {
            this.Interval300SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
        else if (interval >= 600)
        {
            this.Interval600SecIconData.Data = _mainViewModel.DataCheckedCircleIcon;
        }
    }

    public void OnHideMenuFlyout(object? sender, EventArgs e)
    {
        var flyout = FlyoutBase.GetAttachedFlyout(this);

        flyout?.Hide();
    }

    private void LoadSettings()
    {
        int windowTop = 0;
        int windowLeft = 0;
        double windowHeight = 300;
        double windowWidth = 300;
        WindowState windowState = WindowState.Normal;

        if (!System.IO.File.Exists(App.AppConfigFilePath))
        {
            Debug.WriteLine("(!System.IO.File.Exists(App.AppConfigFilePath)");
            return;
        }

        var xdoc = XDocument.Load(App.AppConfigFilePath);

        if (xdoc.Root is null)
        {
            Debug.WriteLine("Oops. xdoc.Root is null.");
            return;
        }

        #region == Window setting ==

        // Main Window element
        var mainWindow = xdoc.Root.Element("MainWindow");
        if (mainWindow is not null)
        {
            var hoge = mainWindow.Attribute("top");
            if (hoge is not null)
            {
                if (Int32.TryParse(hoge.Value, out var wY))
                {
                    windowTop = wY;
                }
            }

            hoge = mainWindow.Attribute("left");
            if (hoge is not null)
            {
                if (Int32.TryParse(hoge.Value, out var wX))
                {
                    windowLeft = wX;
                }
            }
            //w.Position = new PixelPoint(wX, wY);

            hoge = mainWindow.Attribute("height");
            if (hoge is not null)
            {
                if (!string.IsNullOrEmpty(hoge.Value))
                {
                    windowHeight = double.Parse(hoge.Value);
                }
            }

            hoge = mainWindow.Attribute("width");
            if (hoge is not null)
            {
                if (!string.IsNullOrEmpty(hoge.Value))
                {
                    windowWidth = double.Parse(hoge.Value);
                }
            }

            hoge = mainWindow.Attribute("state");
            if (hoge is not null)
            {
                if (hoge.Value == "FullScreen")
                {
                    windowState = WindowState.FullScreen;
                }
                else if (hoge.Value == "Maximized")
                {
                    // Since there is no restorebounds in AvaloniaUI, .....
                    windowState = WindowState.Maximized;
                }
                else if (hoge.Value == "Normal")
                {
                    windowState = WindowState.Normal;
                }
                else if (hoge.Value == "Minimized")
                {
                    // Ignore minimized.
                    windowState = WindowState.Normal;
                }
            }
        }

        if ((windowState == WindowState.FullScreen) || (windowState == WindowState.Maximized))
        {
            // When FullScreen or Maximized, don't set size and pos.
            // Currently _winRestore** are't used for much. It's a TODO;

            if (windowWidth >= 300)
            {
                _winRestoreWidth = (int)windowWidth;
            }
            if (windowHeight >= 300)
            {
                _winRestoreHeight = (int)windowHeight;
            }

            if ((windowLeft >= 0) && (windowTop >= 0))
            {
                _winRestoreLeft = windowLeft;
                _winRestoreTop = windowTop;
            }

            this.WindowState = windowState;

            if (windowState == WindowState.FullScreen)
            {
                // Somehow, initial state change does not get captured by window property changed event.
                
                SetWindowStateFullScreen();
                _mainViewModel.IsFullscreen = true;
            }
        }
        else
        {
            this.WindowState = windowState;

            if (windowWidth >= 300)
            {
                this.Width = windowWidth;
            }
            if (windowHeight >= 300)
            {
                this.Height = windowHeight;
            }

            if ((windowLeft >= 0) && (windowTop >= 0))
            {
                this.Position = new PixelPoint(windowLeft, windowTop);
                //Debug.WriteLine($"(windowLeft {windowLeft} >= 0) && (windowTop {windowTop} >= 0)");
            }
            else
            {
                Debug.WriteLine("Oops. (windowLeft >= 0) && (windowTop >= 0)");
            }
        }

        #endregion

        #region == Options ==

        var opts = xdoc.Root.Element("Options");
        if (opts is not null)
        {
            var attrs = opts.Attribute("lastOpenedDirectory");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    _lastOpenedDirectory = attrs.Value;
                }
            }

            attrs = opts.Attribute("isShuffleOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsShuffleOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsShuffleOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isRepeatOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsRepeatOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsRepeatOn = false;
                    }
                }
            }

            //#IsStayOnTop
            //#IsSlideshowOn
            //#IsFullscreenOn

            attrs = opts.Attribute("isOverrideSystemDpiScalingFactorOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsOverrideSystemDpiScalingFactorOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsOverrideSystemDpiScalingFactorOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isViewImageListOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsViewImageListOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsViewImageListOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isViewFilePathPopupOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsViewFilePathPopupOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsViewFilePathPopupOn = false;
                    }
                }
            }

            //SlideshowTimerInterval
            attrs = opts.Attribute("slideshowTimerInterval");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    _mainViewModel.SetSlideshowInterval(attrs.Value);
                }
            }

            attrs = opts.Attribute("isStretchInOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsStretchInOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsStretchInOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isStretchOutOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsStretchOutOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsStretchOutOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isEffectFadeInAndOutOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsEffectFadeInAndOutOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsEffectFadeInAndOutOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isEffectPageSlideOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsEffectPageSlideOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsEffectPageSlideOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isNoEffectsOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsNoEffectsOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsNoEffectsOn = false;
                    }
                }
            }

            attrs = opts.Attribute("isEffectCrossfadeOn");
            if (attrs is not null)
            {
                if (!string.IsNullOrEmpty(attrs.Value))
                {
                    if (attrs.Value == "True")
                    {
                        _mainViewModel.IsEffectCrossfadeOn = true;
                    }
                    else
                    {
                        _mainViewModel.IsEffectCrossfadeOn = false;
                    }
                }
            }

        }

        #endregion

        _isFullyLoaded = true;
    }

    private void InitKeyBindigs()
    {
        // TODO: more

        // 
        var ToggleSlideshowCommandKeyBinding = new KeyBinding
        {
            Gesture = new KeyGesture(Avalonia.Input.Key.Space, KeyModifiers.None),
            Command = _mainViewModel.ToggleSlideshowCommand
        };
        // set keybinding here instead of in Window_KeyDown.
        this.KeyBindings.Add(ToggleSlideshowCommandKeyBinding);

        // MenuItemStartSlideshow
        this.MenuItemStartSlideshow.InputGesture = new KeyGesture(Avalonia.Input.Key.Space, KeyModifiers.None);
        this.MenuItemQuit.KeyBindings.Add(ToggleSlideshowCommandKeyBinding);
        //
        var QuitCommandKeyBinding = new KeyBinding
        {
            Gesture = new KeyGesture(Avalonia.Input.Key.Q, KeyModifiers.Control),
            Command = _mainViewModel.QuitCommand
        };
        this.KeyBindings.Add(QuitCommandKeyBinding);

        // MenuItemQuit
        this.MenuItemQuit.InputGesture = new KeyGesture(Avalonia.Input.Key.Q, KeyModifiers.Control);
        this.MenuItemQuit.KeyBindings.Add(QuitCommandKeyBinding);
    }

    private void UpdateSystemDPIScalingFactor()
    {
        // SystemDPIScalingFactor
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            IPlatformHandle? platformHandle = this.TryGetPlatformHandle();
            if (platformHandle != null)
            {
                _systemDpiScalingFactor = DpiHelper.GetWindowScalingFactor(platformHandle.Handle);

                //Debug.WriteLine($"SystemDPIScalingFactor = {_systemDPIScalingFactor}");

                if (_systemDpiScalingFactor != 1)
                {
                    _mainViewModel.SystemDpiScalingFactor = _systemDpiScalingFactor;

                    this.MenuItemSystemDpiScalingFactor.IsVisible = true;
                    this.MenuItemSystemDpiScalingFactor.IsEnabled = true;
                    this.MenuItemSystemDpiScalingFactor.Header = $"Override DPI Scaling ({_systemDpiScalingFactor * 100}%)"; //Override System DPI Scaling Factor 

                    return;
                }
            }
        }
    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Too late to change window size etc. Move to constructor.
        //LoadSettings();

        this.ContentFrame.Content = App.GetService<MainView>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            this.BackgroundLayerBorder.IsVisible = true;
            this.BackgroundLayerBorder.StartFadeInAnimation(TimeSpan.FromSeconds(0.8), 0.9f);
        }

        // Update Slideshow Interval menu 
        OnSlideshowIntervalChanged(_mainViewModel.SlideshowTimerInterval);

        // SystemDpiScalingFactor - Set default.
        // don't _mainViewModel.IsOverrideSystemDpiScalingFactorOn = false;
        this.MenuItemSystemDpiScalingFactor.IsVisible = true;
        this.MenuItemSystemDpiScalingFactor.IsEnabled = false;
        this.MenuItemSystemDpiScalingFactor.Header = $"Override DPI Scaling (100%)"; //Override System DPI Scaling Factor 

        // SystemDpiScalingFactor - Windows Only
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Initial screen detection upon loading
            _currentScreen = GetCurrentScreen();

            //Debug.WriteLine($"Initial Screen: {_currentScreen?.DisplayName}");

            // Subscribe to position changes
            this.PositionChanged += MainWindow_PositionChanged;

            // Optional: Subscribe to global screen configuration changes (e.g., resolution change, monitor added/removed)
            if (this.Screens is { } screens)
            {
                screens.Changed += Screens_Changed;
            }

            UpdateSystemDPIScalingFactor();
        }
    }

    private void MainWindow_PositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        CheckAndNotifyDisplayChange();
    }

    private void Screens_Changed(object? sender, EventArgs e)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Screen configurations changed, check if the window is still on the same "logical" screen
        CheckAndNotifyDisplayChange();
    }

    private void CheckAndNotifyDisplayChange()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var newScreen = GetCurrentScreen();

        if (newScreen != null && _currentScreen != null && newScreen != _currentScreen)
        {
            // Compare a unique identifier, like the screen bounds or name if available
            if (newScreen.Bounds != _currentScreen.Bounds)
            {
                //System.Diagnostics.Debug.WriteLine($"Window moved from screen {_currentScreen.Bounds} to {newScreen.Bounds}");
                // *** Raise your custom event or execute your logic here ***
                OnDisplayChanged(newScreen);
                _currentScreen = newScreen;
            }
        }
        else if (_currentScreen == null && newScreen != null)
        {
            // Handle case where window was on no screen and moved to one
            _currentScreen = newScreen;
            OnDisplayChanged(newScreen);
        }
    }

    private Avalonia.Platform.Screen? GetCurrentScreen()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        if (this.Screens is not { } screens || screens.All.Count == 0)
        {
            return null;
        }

        // Get the screen that most contains the window
        // The window's position is its top-left corner in virtual screen coordinates
        var windowBounds = new PixelRect(Position, new PixelSize((int)Width, (int)Height));

        // Use ScreenFromBounds which is more robust than relying just on top-left pixel
        return screens.ScreenFromBounds(windowBounds);

        // Alternatively, use ScreenFromVisual (available on TopLevel)
        // return screens.ScreenFromVisual(this); 
    }

    private void OnDisplayChanged(Avalonia.Platform.Screen newScreen)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Add your custom logic here (e.g., adjust DPI specific settings, reload data, etc.)
        Debug.WriteLine($"Window has crossed to a new display: {newScreen.DisplayName}");

        UpdateSystemDPIScalingFactor();
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        _mainViewModel.CleanUp();

        SaveSettings();
    }

    private void SaveSettings()
    {
        // Make sure Window and settings have been fully loaded and not overriding with empty data.
        if (!_isFullyLoaded)
            return;

        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        // Config xml file
        XmlDocument doc = new();
        XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

        // Root Document Element
        XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
        doc.AppendChild(root);

        XmlAttribute attrs;

        #region == Window settings ==

        // Main Window element
        XmlElement mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

        //Window w = (sender as Window);
        // Main Window attributes
        attrs = doc.CreateAttribute("height");
        if (this.WindowState == WindowState.Normal)
        {
            attrs.Value = this.Height.ToString();
        }
        else
        {
            attrs.Value = _winRestoreHeight.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("width");
        if (this.WindowState == WindowState.Normal)
        {
            attrs.Value = this.Width.ToString();
        }
        else
        {
            attrs.Value = _winRestoreWidth.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("top");
        if (this.WindowState == WindowState.Normal)
        {
            //Debug.WriteLine("this.Position.Y.ToString() " + this.Position.Y.ToString());

            attrs.Value = this.Position.Y.ToString();
        }
        else
        {
            Debug.WriteLine("_winRestoreTop.ToString() " + _winRestoreTop.ToString());

            attrs.Value = _winRestoreTop.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("left");
        if (this.WindowState == WindowState.Normal)
        {
            attrs.Value = this.Position.X.ToString();
        }
        else
        {
            attrs.Value = _winRestoreLeft.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("state");
        if (this.WindowState == WindowState.FullScreen)
        {
            attrs.Value = "FullScreen";
        }
        else if (this.WindowState == WindowState.Maximized)
        {
            attrs.Value = "Maximized";
        }
        else if (this.WindowState == WindowState.Normal)
        {
            attrs.Value = "Normal";
        }
        else if (this.WindowState == WindowState.Minimized)
        {
            attrs.Value = "Minimized";
        }
        mainWindow.SetAttributeNode(attrs);

        // set MainWindow element to root.
        root.AppendChild(mainWindow);

        #endregion

        #region == Options ==

        XmlElement opts = doc.CreateElement(string.Empty, "Options", string.Empty);

        // LastOpenedDirectory
        attrs = doc.CreateAttribute("lastOpenedDirectory");
        if (!string.IsNullOrEmpty(_lastOpenedDirectory))
        {
            attrs.Value = _lastOpenedDirectory;
        }
        else
        {
            attrs.Value = string.Empty;
        }
        opts.SetAttributeNode(attrs);

        //IsShuffleOn
        attrs = doc.CreateAttribute("isShuffleOn");
        if (vm.IsShuffleOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsRepeatOn
        attrs = doc.CreateAttribute("isRepeatOn");
        if (vm.IsRepeatOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //#IsStayOnTop
        //#IsSlideshowOn
        //#IsFullscreenOn

        attrs = doc.CreateAttribute("isOverrideSystemDpiScalingFactorOn");
        if (vm.IsOverrideSystemDpiScalingFactorOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsViewImageListOn
        attrs = doc.CreateAttribute("isViewImageListOn");
        if (vm.IsViewImageListOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsViewFilePathPopupOn
        attrs = doc.CreateAttribute("isViewFilePathPopupOn");
        if (vm.IsViewFilePathPopupOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //SlideshowTimerInterval
        attrs = doc.CreateAttribute("slideshowTimerInterval");
        attrs.Value = vm.SlideshowTimerInterval.ToString();
        opts.SetAttributeNode(attrs);

        //IsStretchInOn
        attrs = doc.CreateAttribute("isStretchInOn");
        if (vm.IsStretchInOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsStretchOutOn
        attrs = doc.CreateAttribute("isStretchOutOn");
        if (vm.IsStretchOutOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsEffectFadeInAndOutOn
        attrs = doc.CreateAttribute("isEffectFadeInAndOutOn");
        if (vm.IsEffectFadeInAndOutOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsEffectPageSlideOn
        attrs = doc.CreateAttribute("isEffectPageSlideOn");
        if (vm.IsEffectPageSlideOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsNoEffectsOn
        attrs = doc.CreateAttribute("isNoEffectsOn");
        if (vm.IsNoEffectsOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);

        //IsEffectCrossfadeOn
        attrs = doc.CreateAttribute("isEffectCrossfadeOn");
        if (vm.IsEffectCrossfadeOn)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        opts.SetAttributeNode(attrs);


        /// 
        root.AppendChild(opts);

        #endregion

        try
        {
            if (!Directory.Exists(App.AppDataFolder))
            {
                Directory.CreateDirectory(App.AppDataFolder);
            }

            doc.Save(App.AppConfigFilePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception @OnWindowClosing: " + ex);

            App.AppendErrorLog("Exception @OnWindowClosing", ex.Message);
        }

        if (vm.IsSaveLog)
        {
            App.SaveErrorLog();
        }
    }

    private void Window_Closed(object? sender, System.EventArgs e)
    {
        _mainViewModel.Destroy();
    }

    private void Window_Resized(object? sender, Avalonia.Controls.WindowResizedEventArgs e)
    {
        UpdateQueueListBoxImages();

        if (this.WindowState == WindowState.Normal)
        {
            _winRestoreHeight = (int)this.Height;
            _winRestoreWidth = (int)this.Width;
            _winRestoreTop = (int)this.Position.X;
            _winRestoreLeft = (int)this.Position.X;
        }

        _mainViewModel.ClientAreaSizeChanged(this.ClientAreaGrid.Bounds.Width, this.ClientAreaGrid.Bounds.Height);
    }

    private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (this.WindowState == WindowState.FullScreen)
        {
            var position = e.GetPosition(this);

            if ((position.X == _mousePosition.X) && (position.Y == _mousePosition.Y))
            {
                //Debug.WriteLine($"Same pos, returning: sender {sender}, X {position.X}, Y {position.Y}, Source {e.Source}");
                return;
            }

            _mousePosition = position;

            if (_timerPointerCursorHide.IsEnabled)
            {
                _timerPointerCursorHide.Stop();
            }

            if (_mainViewModel.IsWorking)
            {
                this.Cursor = new Cursor(StandardCursorType.AppStarting);
            }
            else
            {
                this.Cursor = Cursor.Default;
            }

            _timerPointerCursorHide.Start();

            e.Handled = true;
        }
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // Call BeginMoveDrag to enable window dragging
                BeginMoveDrag(e);
            }
            else if (this.WindowState == WindowState.FullScreen)
            {
                //this.Cursor = new Cursor(StandardCursorType.None);
            }
        }

        // Right clicked on Window.
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (sender is Window target)
            {
                var flyout = FlyoutBase.GetAttachedFlyout(target);
                if (flyout is MenuFlyout menuFlyout)
                {
                    menuFlyout.Placement = PlacementMode.Pointer;
                }
                // Show the flyout using the 'Placement="Pointer"' property.
                flyout?.ShowAt(target);

                if (this.WindowState == WindowState.FullScreen)
                {
                    if (_mainViewModel.IsWorking)
                    {
                        this.Cursor = new Cursor(StandardCursorType.AppStarting);
                    }
                    else
                    {
                        this.Cursor = Cursor.Default;
                    }
                }
            }
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.DataTransfer is null)
        {
            return;
        }

        // Only allow copy effect for file drops
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;
        }

        // Deprecated.
        /*
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        */
    }

    private async void Window_Drop(object sender, DragEventArgs e)
    {
        //Debug.WriteLine("Window_Drop()");

        if (_mainViewModel.IsWorking)
        {
            // Already processing. 
            return;
        }

        if (e.DataTransfer is null)
        {
            return;
        }

        // Check if the dropped data contains file paths
        // Only allow copy effect for file drops
        if (e.DataTransfer.Contains(DataFormat.File))
        {
            e.DragEffects = DragDropEffects.Copy;
        }
        else
        {
            e.DragEffects = DragDropEffects.None;

            return;
        }

        _mainViewModel.IsWorking = true;

        var fileNames = e.DataTransfer.GetItems(DataFormat.File)?.ToList();
        if (fileNames is null || fileNames.Count == 0)
        {
            _mainViewModel.IsWorking = false;
            return;
        }

        var droppedFiles = new List<string>();
        foreach (var file in fileNames)
        {
            var filePath = file.TryGetFile()?.TryGetLocalPath(); //file.TryGetLocalPath(); Deprecated.
            if (string.IsNullOrEmpty(filePath))
            {
                continue;
            }

            droppedFiles.Add(filePath);
            Debug.WriteLine(filePath);
            //Debug.WriteLine(file);
        }

        if (droppedFiles.Count > 0)
        {
            // Remove dupe. Looks like a bug in Avalonia... when dropped from desktop for the first time.
            droppedFiles = [.. droppedFiles.Distinct()];

            //_ = ProcessFiles(droppedFiles);
            //await ProcessFiles(droppedFiles);
            await Task.Run(() => ProcessFiles(droppedFiles));
        }
        else
        {
            _mainViewModel.IsWorking = false;
        }

        /*
        // awaiting is bad right here.
        //await Task.Yield();

        //Debug.WriteLine("Getting GetDroppedItems @Window_Drop");

        var droppedFiles = await GetDroppedItems(e.DataTransfer);
        if (droppedFiles.Count > 0)
        {
            //Debug.WriteLine("Starting await ProcessFiles @Window_Drop");

            //await ProcessFiles(droppedFiles);
            await Task.Run(() => ProcessFiles(droppedFiles));
        }
        else
        {
            _mainViewModel.IsWorking = false;
        }
        */
    }

    private static Task<List<string>> GetDroppedItems(IDataTransfer data)
    {
        //Debug.WriteLine("GetDroppedItems()");

        //ATN: Do not await in here.

        var droppedFiles = new List<string>();
        // Check if the dropped data contains file paths
        //if (e.Data.Contains(Avalonia.Input.DataFormats.Files)) // Deprecated.
        if (data.Contains(DataFormat.File))
        {
            var fileNames = data.GetItems(DataFormat.File)?.ToList();
            if (fileNames is not null && fileNames.Count != 0)
            {
                foreach (var file in fileNames)
                {
                    var filePath = file.TryGetFile()?.TryGetLocalPath(); //file.TryGetLocalPath(); Deprecated.
                    if (filePath != null)
                    {
                        droppedFiles.Add(filePath);
                    }
                }

                if (droppedFiles.Count > 0)
                {
                    //ProcessFiles(droppedFiles);
                }
            }
        }

        return Task.FromResult(droppedFiles);
    }

    private static void RecursivelyProcessFiles(List<string> fileNames, List<FileSystemInfo> allItems)
    {
        // On Linux.
        //Sort
        IComparer<string> _naturalSortComparer = new NaturalSortComparer();
        fileNames = [.. fileNames.OrderBy(x => x, _naturalSortComparer)];//StringComparer.Ordinal

        // File first
        List<FileSystemInfo> files = [];
        foreach (var path in fileNames)
        {
            if (System.IO.File.Exists(path))
            {
                files.Add(new FileInfo(path));
            }
        }

        allItems.AddRange(files);

        // Dir next.
        foreach (var path in fileNames)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo directory = new(path);
                var folderFiles = directory.GetFileSystemInfos("*", SearchOption.TopDirectoryOnly).ToList();

                if (folderFiles is not null)
                {
                    List<string> folderFileNames = [.. folderFiles.Select(x => x.FullName)];

                    if (folderFileNames.Count > 0)
                    {
                        RecursivelyProcessFiles(folderFileNames, allItems);
                    }
                }
            }
        }
    }

    private async Task ProcessFiles(List<string> fileNames)
    {
        //Debug.WriteLine("ProcessFiles()");

        _mainViewModel.IsWorking = true;

        // Don't await. FIRE and FORGET! Otherwise GUI would freeze or be 100x slower.
        // Don't _ =, nor await = . at all. 
        await Task.Run(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _mainViewModel.IsWorking = true;
            });

            var validExt = _mainViewModel.ValidExtensions;

            try
            {
                if (fileNames.Count > 0)
                {
                    if (System.IO.File.Exists(fileNames[0]))//.Path.LocalPath
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            string? parentFolderPath = System.IO.Path.GetDirectoryName(fileNames[0]);
                            if (parentFolderPath is not null)
                            {
                                // Writes to Window title bar.
                                this.Title = System.IO.Path.GetFileName(parentFolderPath);
                            }
                        });
                    }
                    else if (Directory.Exists(fileNames[0]))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            // Writes to Window title bar.
                            this.Title = System.IO.Path.GetFileName(fileNames[0]);
                        });
                    }
                }

                // 
                var droppedImages = new List<ImageInfo>();
                string singleSelectedOriginalFile = string.Empty;

                // Linux for sort
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) //RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                {
                    //Debug.WriteLine("Linux sort mode @ProcessFiles()");

                    List<string> IncludeSiblingsFileNames = [];

                    // Single file dropped, in that case, get all siblings.
                    if (fileNames.Count == 1)
                    {
                        if (System.IO.File.Exists(fileNames[0]))
                        {
                            singleSelectedOriginalFile = fileNames[0];

                            // Get parent dir.
                            string? parentFolderPath = System.IO.Path.GetDirectoryName(singleSelectedOriginalFile);
                            if (parentFolderPath is not null)
                            {
                                if (Directory.Exists(parentFolderPath))
                                {
                                    // NON-Recursively get all files from the folder
                                    var filesInFolder = Directory.GetFiles(parentFolderPath, "*", SearchOption.TopDirectoryOnly);

                                    // Sort
                                    //filesInFolder = [.. filesInFolder.OrderBy(f => f)];

                                    IncludeSiblingsFileNames.AddRange(filesInFolder);

                                    if (IncludeSiblingsFileNames.Count > 1)
                                    {
                                        IComparer<string> _naturalSortComparer = new NaturalSortComparer();
                                        //IncludeSiblingsFileNames = [.. IncludeSiblingsFileNames.OrderBy(x => x, _naturalSortComparer)];
                                        IncludeSiblingsFileNames = [.. IncludeSiblingsFileNames.OrderBy(x => x, _naturalSortComparer)];
                                    }
                                }
                                else
                                {
                                    IncludeSiblingsFileNames.Add(singleSelectedOriginalFile);
                                }
                            }
                            else
                            {
                                IncludeSiblingsFileNames.Add(singleSelectedOriginalFile);
                            }
                        }
                        else if (System.IO.Directory.Exists(fileNames[0]))
                        {
                            IncludeSiblingsFileNames.AddRange(fileNames);
                        }
                    }
                    else
                    {
                        IncludeSiblingsFileNames.AddRange(fileNames);
                    }

                    List<FileSystemInfo> allItems = [];

                    RecursivelyProcessFiles(IncludeSiblingsFileNames, allItems);

                    foreach (var item in allItems)
                    {
                        //Debug.WriteLine(item.FullName);

                        if (item is DirectoryInfo)
                        {
                            //Debug.WriteLine($"[FOLDER] Full Path: {item.FullName}, Name: {item.Name}");
                        }
                        else if (item is FileInfo)
                        {
                            //Debug.WriteLine($"[FILE]   Full Path: {item.FullName}, Name: {item.Name}");

                            if (!MainViewModel.HasImageExtension(item.Name, validExt))
                            {
                                continue;
                            }

                            // Avoid MacOS's garbage. Use char overload for faster comp if possible.
                            if ((item.Name.StartsWith('.')) || (item.Name.StartsWith("._")))
                            {
                                continue;
                            }

                            var img = new ImageInfo
                            {
                                ImageFilePath = item.FullName
                            };

                            droppedImages.Add(img);
                        }
                    }

                    // Sort originaly selected single file to first position.
                    //if ((droppedImages.Count > 0) && (!string.IsNullOrEmpty(singleSelectedOriginalFile)))
                    //{
                    // Sort to move the first instance of 'originalFile' to the front, followed by other files.
                    // Using `Distinct()` will remove the remaining duplicates.
                    //droppedImages = droppedImages.OrderBy(x => x.ImageFilePath == singleSelectedOriginalFile ? 0 : 1).Distinct().ToList();
                    //}
                }
                else // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    //Debug.WriteLine("None-Linux sort mode @ProcessFiles()");

                    var droppedFiles = new List<string>();

                    // Get all files recursively.
                    foreach (var item in fileNames)
                    {
                        if (System.IO.File.Exists(item))//.Path.LocalPath
                        {
                            // Add single files
                            droppedFiles.Add(item);//.Path.LocalPath
                        }
                        else if (Directory.Exists(item))//.Path.LocalPath
                        {
                            // Recursively get all files from a dropped folder
                            var filesInFolder = Directory.GetFiles(item, "*", SearchOption.AllDirectories);//.Path.LocalPath
                                                                                                           //filesInFolder = [.. filesInFolder.OrderBy(f => f)];
                            droppedFiles.AddRange(filesInFolder);
                        }

                        //Debug.WriteLine(item);
                    }

                    // Single file dropped, in that case, get all siblings.
                    if (droppedFiles.Count == 1)
                    {
                        if (System.IO.File.Exists(droppedFiles[0]))
                        {
                            //var originalFile = droppedFiles[0];
                            singleSelectedOriginalFile = droppedFiles[0];

                            // Get parent dir.
                            string? parentFolderPath = System.IO.Path.GetDirectoryName(droppedFiles[0]);
                            if (parentFolderPath is not null)
                            {
                                if (Directory.Exists(parentFolderPath))
                                {
                                    // NON-Recursively get all files from the folder
                                    var filesInFolder = Directory.GetFiles(parentFolderPath, "*", SearchOption.TopDirectoryOnly);

                                    // Sort
                                    //filesInFolder = [.. filesInFolder.OrderBy(f => f)];

                                    droppedFiles.AddRange(filesInFolder);

                                    if (droppedFiles.Count > 1)
                                    {
                                        // Sort to move the first instance of 'originalFile' to the front, followed by other files.
                                        // Using `Distinct()` will remove the remaining duplicates.
                                        //droppedFiles = droppedFiles.OrderBy(x => x == singleSelectedOriginalFile ? 0 : 1).Distinct().ToList();

                                        // removes now duplicated image file path.
                                        droppedFiles.RemoveAt(0);
                                    }
                                }
                            }
                        }
                    }

                    foreach (var fileFullPath in droppedFiles)
                    {
                        if (!MainViewModel.HasImageExtension(fileFullPath, validExt))
                        {
                            continue;
                        }

                        // Avoid MacOS's garbage. Use char overload for faster comp if possible.
                        string fileName = System.IO.Path.GetFileName(fileFullPath);

                        if (fileName.StartsWith('.') || fileName.StartsWith("._"))
                        {
                            continue;
                        }

                        var img = new ImageInfo
                        {
                            ImageFilePath = fileFullPath
                        };

                        droppedImages.Add(img);
                    }
                }

                if (droppedImages.Count < 1)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _mainViewModel.IsWorking = false;
                    });
                    return;
                }

                //_mainViewModel.DroppedFiles(droppedImages);
                Dispatcher.UIThread.Post(async () =>
                {
                    //Debug.WriteLine("Calling DroppedFiles in ViewModel @ProcessFiles()");

                    await _mainViewModel.DroppedFiles(droppedImages, singleSelectedOriginalFile);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _mainViewModel.IsWorking = false;
                });
                // TODO: log error and show error message.
                Debug.WriteLine(ex);
            }
        });
    }

    private void Window_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        //WindowState = (WindowState == WindowState.FullScreen) ? WindowState.Normal : WindowState.FullScreen;
        /*
        if (WindowState == WindowState.FullScreen)
        {
            SetWindowStateNormal();
        }
        else if (WindowState == WindowState.Normal)
        {
            SetWindowStateFullScreen();
        }
        */
        if (WindowState == WindowState.FullScreen)
        {
            OnUpdateFullscreenState(false);
        }
        else if (WindowState == WindowState.Normal)
        {
            OnUpdateFullscreenState(true);
        }
    }

    private void SetWindowStateFullScreen()
    {
        //this.QueueListBox.IsVisible = false;
        _mainViewModel.IsQueueListBoxVisible = false;
        _mainViewModel.IsFilePathPopupVisible = false;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // hack for CaptionButtons not dissapearing fast enough problem.
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;

            Dispatcher.UIThread.Post(async () =>
            {
                await Task.Delay(20);
                //await Task.Yield();

                this.WindowState = WindowState.FullScreen;

                //this.Cursor = new Cursor(StandardCursorType.None);
                if (_timerPointerCursorHide.IsEnabled)
                {
                    _timerPointerCursorHide.Stop();
                }
                _timerPointerCursorHide.Start();

                if (_mainViewModel.IsSlideshowOn)
                {
                    Debug.WriteLine("SetThreadExecutionState set @SetWindowStateFullScreen");
                    NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);
                }
            });
        }
        else
        {
            this.WindowState = WindowState.FullScreen;

            //this.Cursor = new Cursor(StandardCursorType.None);
            if (_timerPointerCursorHide.IsEnabled)
            {
                _timerPointerCursorHide.Stop();
            }
            _timerPointerCursorHide.Start();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (_mainViewModel.IsSlideshowOn)
                {
                    Debug.WriteLine("SetThreadExecutionState set @SetWindowStateFullScreen");
                    NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);
                }
            }
        }
    }

    private void SetWindowStateNormal()
    {
        if (_mainViewModel.IsWorking)
        {
            this.Cursor = new Cursor(StandardCursorType.AppStarting);
        }
        else
        {
            this.Cursor = Cursor.Default;
        }

        this.WindowState = WindowState.Normal;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
        }

        if (_timerPointerCursorHide.IsEnabled)
        {
            _timerPointerCursorHide.Stop();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (_mainViewModel.IsSlideshowOn)
            {
                Debug.WriteLine("SetThreadExecutionState off @SetWindowStateNormal()");
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
            }
        }

        // restore visibility
        if (_mainViewModel.IsViewImageListOn)
        {
            // queue count more than one, show image list.
            _mainViewModel.IsQueueListBoxVisible = _mainViewModel.Queue.Count != 1;
        }
        // restore visibility
        if (_mainViewModel.IsViewFilePathPopupOn)
        {
            if (_mainViewModel.Queue.Count > 0)
            {
                _mainViewModel.IsFilePathPopupVisible = true;
            }
            else
            {
                _mainViewModel.IsFilePathPopupVisible = false;
            }
        }
    }

    private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape)
        {
            if (WindowState == WindowState.FullScreen)
            {
                SetWindowStateNormal();

                e.Handled = true;
            }
        }
        else if (e.Key == Avalonia.Input.Key.Space)
        {
            //_mainViewModel.SpaceKeyPressed();

            //e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.Right)
        {
            _ = _mainViewModel.NextKeyPressed();

            e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.Left)
        {
            _ = _mainViewModel.PrevKeyPressed();

            e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.F)
        {
            if (WindowState == WindowState.FullScreen)
            {
                SetWindowStateNormal();
            }
            else if (WindowState == WindowState.Normal)
            {
                SetWindowStateFullScreen();
            }

            e.Handled = true;
        }
        else if (e.Key == Avalonia.Input.Key.Tab)
        {
            //e.Handled = true;
        }
        else if (e.PhysicalKey == PhysicalKey.ContextMenu)
        {
            var flyout = FlyoutBase.GetAttachedFlyout(this);

            if (flyout is MenuFlyout menuFlyout)
            {
                menuFlyout.Placement = PlacementMode.Center;
                flyout?.ShowAt(this);
            }

            e.Handled = true;
        }
    }

    private void Window_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape)
        {
            if (WindowState == WindowState.FullScreen)
            {
                e.Handled = true;
            }
        }
        else if (e.Key == Avalonia.Input.Key.Space)
        {
            if (this.DataContext is MainViewModel)
            {
                //vm.SpaceKeyPressed();

                //e.Handled = true;
            }
        }
        else if (e.Key == Key.Right)
        {
            if (this.DataContext is MainViewModel)
            {
                //vm.NextKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Left)
        {
            if (this.DataContext is MainViewModel)
            {
                //vm.PrevKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.F)
        {
            if (this.DataContext is MainViewModel)
            {
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Tab)
        {
            //e.Handled = true;
        }
        else if (e.PhysicalKey == PhysicalKey.ContextMenu)
        {
            e.Handled = true;
        }
    }

    private void Window_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (e.Delta.Y > 0) // Scroll up
        {
            _ = _mainViewModel.PrevKeyPressed();
        }
        else if (e.Delta.Y < 0) // Scroll down
        {
            //
            _ = _mainViewModel.NextKeyPressed();
        }

        e.Handled = true;
    }

    private void ListBox_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void ListBox_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;

        if (sender is not ListBox lb)
        {
            return;
        }

        var element = e.Source as Visual;

        var listBoxItem = element?.FindAncestorOfType<ListBoxItem>();
        if (listBoxItem == null)
        {
            return;
        }
        var dataItem = listBoxItem.DataContext;

        if (dataItem == null)
        {
            return;
        }
        if (dataItem is not ImageInfo Item)
        {
            return;
        }

        lb.SelectedItem = Item;

        _ = _mainViewModel.ListBoxItemSelected(Item);
    }

    private void ListBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            this.Focus();
            e.Handled = true;
        }
        /*
        else if (e.Key == Key.Right)
        {
            _mainViewModel.NextKeyPressed();

            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            _mainViewModel.PrevKeyPressed();

            e.Handled = true;
        }
        */
    }

    private void ListBox_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            e.Handled = true;
        }
        /*
        else if (e.Key == Key.Right)
        {
            //vm.NextKeyPressed();

            e.Handled = true;
        }
        else if (e.Key == Key.Left)
        {
            //vm.PrevKeyPressed();

            e.Handled = true;
        }
        */
    }

    private void ListBox_GotFocus(object? sender, Avalonia.Input.GotFocusEventArgs e)
    {
        this.ListBoxBackgroundLayerBorder.IsVisible = true;
    }

    private void ListBox_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        e.Handled = true;
    }

    private void ListBox_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.ListBoxBackgroundLayerBorder.IsVisible = false;
    }

    private void ListBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not ListBox listBox)
        {
            return;
        }
        //var listBox = sender;

        if (listBox.Tag != null)
        {
            //Debug.WriteLine("(listBox.Tag != null) @ListBoxStackPanelBehaviors");
            return;
        }

        listBox.Tag = "ListBox_Loaded_ListBoxStackPanelBehaviors";

        var scrollViewer = listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

        var virtualPanel = listBox.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();

        if ((scrollViewer != null) && (virtualPanel != null))
        {
            // Subscribe to the scroll event to update the visible items.
            scrollViewer.ScrollChanged += (s, args) => UpdateVisibleItems(scrollViewer, virtualPanel);
            // .. size changed event too.
            scrollViewer.SizeChanged += (s, args) => UpdateVisibleItems(scrollViewer, virtualPanel);

            scrollViewer.DetachedFromVisualTree += (s, e) =>
            {
                scrollViewer.ScrollChanged -= (s, args) => UpdateVisibleItems(scrollViewer, virtualPanel);
                scrollViewer.SizeChanged -= (s, args) => UpdateVisibleItems(scrollViewer, virtualPanel);
            };

            // Call it once initially to set the property.
            UpdateVisibleItems(scrollViewer, virtualPanel);
        }
    }

    private void UpdateVisibleItems(ScrollViewer scrollViewer, VirtualizingStackPanel virtualPanel)
    {
        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        //var _scrollViewer = listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scrollViewer == null) return;

        //var _virtualPanel = listBox.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();
        if (virtualPanel == null) return;

        var viewportRect = new Rect(new Point(scrollViewer.Offset.X, scrollViewer.Offset.Y), scrollViewer.Viewport);

        var visibleObjects = virtualPanel.Children.Where(child => child.Bounds.Intersects(viewportRect)).ToList();

        var visibleItems = new List<object>();

        foreach (var itemContainer in visibleObjects)
        {
            var dataItem = itemContainer.DataContext;
            if (dataItem != null)
            {
                visibleItems.Add(dataItem);
            }
        }

        // Set the new value of the attached property.
        //listBox.SetValue(VisibleItemsProperty, visibleItems);
        vm.VisibleItemsImageInfo = visibleItems;
    }

    private async void UpdateQueueListBoxImages(int selectedIndex)
    {
        if (this.QueueListBox.IsVisible == false)
        {
            return;
        }

        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if ((selectedIndex > (vm.Queue.Count - 1)) && (selectedIndex < 0))
        {
            Debug.WriteLine("if ((selectedIndex > c) && (selectedIndex < 0))");
            return;
        }

        await Task.Yield();
        //await Task.Delay(300); // Need to wait for UI to update
        Dispatcher.UIThread.Post(() =>
        {
            if (this.QueueListBox is not ListBox lb)
            {
                return;
            }

            if (lb.ItemCount == 0)
            {
                return;
            }

            if (lb.ItemCount < (selectedIndex + 1))
            {
                Debug.WriteLine($"if ({lb.ItemCount} < ({selectedIndex + 1}))");
                return;
            }

            //lb.ScrollIntoView(selectedIndex);

            if (DataContext is not MainViewModel vm)
            {
                return;
            }

            var scrollViewer = lb.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (scrollViewer is null)
            {
                return;
            }
            var virtualPanel = lb.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();
            if (virtualPanel is null)
            {
                return;
            }

            var test = vm?.Queue[selectedIndex];
            if (test != null)
            {
                lb.SelectedItem = test;
            }

            lb.ScrollIntoView(selectedIndex);

            // Center selected.
            if (lb.SelectedItem is not null)
            {
                if (lb.ContainerFromItem(lb.SelectedItem) is ListBoxItem item)
                {
                    BringIntoViewCenter(item, scrollViewer);
                }
            }

            UpdateVisibleItems(scrollViewer, virtualPanel);

        }, DispatcherPriority.Loaded);//.Background//.Loaded//.Default
    }

    private async void UpdateQueueListBoxImages()
    {
        await Task.Yield();
        //await Task.Delay(800); // Need to wait for UI to update

        Dispatcher.UIThread.Post(() =>
        {
            if (this.QueueListBox is not ListBox lb)
            {
                return;
            }

            if (DataContext is not MainViewModel vm)
            {
                return;
            }
            var item = lb.SelectedItem;
            if (item is not ImageInfo img)
            {
                return;
            }

            var ind = vm.Queue.IndexOf(img);
            if (ind < 0)
            {
                return;
            }

            lb.ScrollIntoView(ind);

            var scrollViewer = lb.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            if (scrollViewer is null)
            {
                return;
            }

            var virtualPanel = lb.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();
            if (virtualPanel is null)
            {
                return;
            }

            // Center selected.
            if (lb.SelectedItem is not null)
            {
                if (lb.ContainerFromItem(lb.SelectedItem) is ListBoxItem listitem)
                {
                    BringIntoViewCenter(listitem, scrollViewer);
                }
            }

            UpdateVisibleItems(scrollViewer, virtualPanel);

        }, DispatcherPriority.Loaded);////.Default//.Background
    }

    // EnableBlurBehind: Use NativeMemory.Alloc, instead of Marshal.AllocHGlobal
    public static unsafe void EnableBlurBehind(IntPtr handle)
    {
        // The pointer for the unmanaged memory.
        void* accentPtr = null;

        try
        {
            uint darkGrayTint = 0;

            var accent = new NativeMethods.AccentPolicy
            {
                AccentState = NativeMethods.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                AccentFlags = 0,
                GradientColor = darkGrayTint,
                AnimationId = 0
            };

            // Get the size of the structure.
            nuint sizeOfAccent = (nuint)sizeof(NativeMethods.AccentPolicy);

            // Allocate and zero the unmanaged memory.
            accentPtr = NativeMemory.AllocZeroed(sizeOfAccent);

            // Copy the managed struct to the unmanaged memory.
            Unsafe.Copy(accentPtr, ref accent);

            var data = new NativeMethods.WindowCompositionAttributeData
            {
                Attribute = NativeMethods.WindowCompositionAttribute.WCA_ACCENT_POLICY,
                SizeOfData = (int)sizeOfAccent,
                // Convert the void* pointer to an IntPtr for the interop call.
                Data = (IntPtr)accentPtr
            };

            NativeMethods.SetWindowCompositionAttribute(handle, ref data);
        }
        finally
        {
            // Free the unmanaged memory if it was successfully allocated.
            if (accentPtr != null)
            {
                NativeMemory.Free(accentPtr);
            }
        }
    }
    // EnableBlurBehind: Marshal.AllocHGlobal way.
    /*
    private static void EnableBlurBehind(IntPtr handle)
    {
        // Hexadecimal BGR color value
        uint darkGrayTint = 0;//0 default //0xAA222222; 67% transparent, dark gray // 0x99FF99CC; 60% transparent, light blue

        var accent = new NativeMethods.AccentPolicy
        {
            AccentState = NativeMethods.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND, //ACCENT_ENABLE_BLURBEHIND,
            AccentFlags = 0,
            GradientColor = darkGrayTint,//0,
            AnimationId = 0
        };

        var accentPtr = Marshal.AllocHGlobal(Marshal.SizeOf(accent));
        Marshal.StructureToPtr(accent, accentPtr, false);

        var data = new NativeMethods.WindowCompositionAttributeData
        {
            Attribute = NativeMethods.WindowCompositionAttribute.WCA_ACCENT_POLICY,
            SizeOfData = Marshal.SizeOf(accent),
            Data = accentPtr
        };

        NativeMethods.SetWindowCompositionAttribute(handle, ref data);

        Marshal.FreeHGlobal(accentPtr);
    }
    */

    private async void Button_FilePick_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await OpenFilePicker();
    }

    public async Task OpenFilePicker()
    {
        if (_mainViewModel.IsWorking)
        {
            // Already processing. 
            return;
        }

        // TODO: Make/Move this to DialogService.

        // Get the IStorageProvider for the current window
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

        if (storageProvider == null) return; // Storage provider not available on this platform

        var options = new FilePickerOpenOptions
        {
            Title = "Select Image Files",
            AllowMultiple = true, // Set to true to allow multiple selections
            /*
            FileTypeFilter = new[]
            {
                FilePickerFileTypes.ImageAll // All image types
            }
            */
            FileTypeFilter =
            [
                new FilePickerFileType("All Supported Images")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.gif", "*.png", "*.webp", "*.bmp"],
                    AppleUniformTypeIdentifiers = ["public.jpg", "public.jpeg", "public.gif", "public.png", "public.webp", "public.bmp"],
                    MimeTypes = ["image/jpg", "image/jpeg", "image/gif", "image/png", "image/webp", "image/bmp"]
                },
                FilePickerFileTypes.ImagePng,
                FilePickerFileTypes.ImageJpg,
                FilePickerFileTypes.ImageWebp,
                new FilePickerFileType("Other Images")
                {
                    Patterns = ["*.gif", "*.bmp"],
                    AppleUniformTypeIdentifiers = ["public.gif", "public.bmp"],
                    MimeTypes = ["image/gif", "image/bmp"]
                }
            ]
        };

        if (!string.IsNullOrEmpty(_lastOpenedDirectory))
        {
            var folderUri = new Uri(_lastOpenedDirectory);

            var suggestedFolder = await storageProvider.TryGetFolderFromPathAsync(folderUri);

            options.SuggestedStartLocation = suggestedFolder;
        }

        var files = await storageProvider.OpenFilePickerAsync(options);

        if (files != null && files.Any())
        {
            var droppedFiles = new List<string>();
            foreach (var file in files)
            {
                var filePath = file.TryGetLocalPath();
                if (filePath != null)
                {
                    droppedFiles.Add(filePath);
                }
            }

            if (droppedFiles.Count > 0)
            {
                string? parentFolderPath = System.IO.Path.GetDirectoryName(droppedFiles[0]);
                if (parentFolderPath is not null)
                {
                    _lastOpenedDirectory = parentFolderPath;
                }

                //await ProcessFiles(droppedFiles);
                await Task.Run(() => ProcessFiles(droppedFiles));
            }
        }
    }

    private async void Button_FolderPick_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SelectFolder();
    }

    public async Task SelectFolder()
    {
        if (_mainViewModel.IsWorking)
        {
            // Already processing. 
            return;
        }

        // Get the IStorageProvider for the current window
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

        if (storageProvider == null) return; // Storage provider not available on this platform

        // Configure the options for the folder picker
        var options = new FolderPickerOpenOptions
        {
            Title = "Select Folders",
            AllowMultiple = true // Set to true to allow multiple folders
        };

        if (!string.IsNullOrEmpty(_lastOpenedDirectory))
        {
            var folderUri = new Uri(_lastOpenedDirectory);

            var suggestedFolder = await storageProvider.TryGetFolderFromPathAsync(folderUri);

            options.SuggestedStartLocation = suggestedFolder;
        }

        // Display the folder selection dialog
        var folders = await storageProvider.OpenFolderPickerAsync(options);

        // Process the result
        if (folders.Any())
        {
            var droppedFiles = new List<string>();

            foreach (var item in folders)
            {
                var filePath = item.TryGetLocalPath();
                if (filePath is not null)
                {
                    droppedFiles.Add(filePath);
                }
            }

            if (droppedFiles.Count > 0)
            {
                _lastOpenedDirectory = droppedFiles[0];

                //await ProcessFiles(droppedFiles);
                await Task.Run(() => ProcessFiles(droppedFiles));
            }
        }
    }

    private void Button1_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private void Button2_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    private static void BringIntoViewCenter(Control control, ScrollViewer scrollViewer)
    {
        /*
        var scrollViewer = control.GetVisualAncestors()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
        */

        if (scrollViewer == null)
        {
            Debug.WriteLine("if (scrollViewer == null)");
            return;
        }

        var bounds = control.Bounds;
        var scrollViewerBounds = scrollViewer.Bounds;

        // Calculate the center of the control and the ScrollViewer's viewport
        var controlCenterY = bounds.Y + bounds.Height / 2;
        var viewportCenterY = scrollViewer.Offset.Y + scrollViewerBounds.Height / 2;

        // Calculate the difference between the centers
        var diff = controlCenterY - viewportCenterY;

        // Set the new scroll offset
        var newOffset = new Vector(scrollViewer.Offset.X, scrollViewer.Offset.Y + diff);
        scrollViewer.Offset = newOffset;
    }

    private void Window_Activated(object? sender, System.EventArgs e)
    {
        // TODO: dim caption button color only when deactivated.
        //AppTitleBarIcon.Opacity = args.WindowActivationState == WindowActivationState.Deactivated ? 0.4 : 0.8;

        //this.Opacity = 1;

        // Restore Popup.
        if (_mainViewModel.IsViewFilePathPopupOn)
        {
            if (_mainViewModel.CanToggleViewFilePath())
            {
                if ((_mainViewModel.Queue.Count > 0) && (!_mainViewModel.IsFullscreen))
                {
                    this.PopupFilePath.IsOpen = true;
                }
            }
        }
    }

    private void Window_Deactivated(object? sender, System.EventArgs e)
    {
        // TODO: dim caption button color only when deactivated.
        //this.Opacity = 0.8;

        // Popup stays optop, so hide it.
        this.PopupFilePath.IsOpen = false;
    }

    private void ClientAreaGrid_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        _mainViewModel.ClientAreaSizeChanged(this.ClientAreaGrid.Bounds.Width, this.ClientAreaGrid.Bounds.Height);
    }
}

public static partial class NativeMethods
{
    #region == Blur Background (SetWindowCompositionAttribute) ==

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

    [StructLayout(LayoutKind.Sequential)]
    public struct WindowCompositionAttributeData
    {
        public WindowCompositionAttribute Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    public enum WindowCompositionAttribute
    {
        // ... other attributes ...
        WCA_ACCENT_POLICY = 19
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AccentPolicy
    {
        public AccentState AccentState;
        public uint AccentFlags;
        public uint GradientColor;
        public uint AnimationId;
    }

    public enum AccentState
    {
        ACCENT_DISABLED = 0,
        ACCENT_ENABLE_GRADIENT = 1,
        ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
        ACCENT_ENABLE_BLURBEHIND = 3,
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, // Windows 10 v1803+
        ACCENT_INVALID_STATE = 5
    }

    #endregion

    /*
    // For Alt+Space sys menu.
    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    */

    #region == Prevent from sleep (SetThreadExecutionState) ==

    // Define the execution state flags
    public const uint ES_CONTINUOUS = 0x80000000;
    public const uint ES_SYSTEM_REQUIRED = 0x00000001;
    public const uint ES_DISPLAY_REQUIRED = 0x00000002;
    public const uint ES_AWAYMODE_REQUIRED = 0x00000040;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial uint SetThreadExecutionState(uint esFlags);

    #endregion

}

// TryRegisterWindowsMenu() for sys menu (alt+space)
/*
private void TryRegisterWindowsMenu()
{
    if (!OperatingSystem.IsWindows()) return;
    if (GetTopLevel(this)?.TryGetPlatformHandle() is not { } handle) return;
    const uint wmSysCommandMessage = 0x0112;
    const nint sysCommandsScKeyMenu = 0xF100;
    const nint spaceChar = ' ';
    AddHandler(KeyUpEvent, (sender, args) =>
    {
        if (args.Key != Key.Space || args.KeyModifiers == KeyModifiers.None) return;
        NativeMethods.DefWindowProc(handle.Handle, wmSysCommandMessage, sysCommandsScKeyMenu, spaceChar);
    }, RoutingStrategies.Tunnel);
}
*/

public partial class DpiHelper
{
    // Define the P/Invoke signature for GetDpiForWindow
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetDpiForWindow(IntPtr hWnd);

    // Baseline DPI value (100% scale)
    private const uint DpiBase = 96;

    /// <summary>
    /// Calculates the scaling factor for a given window handle.
    /// </summary>
    /// <param name="hWnd">The handle to the window.</param>
    /// <returns>The scaling factor (e.g., 1.0 for 100%, 1.25 for 125%).</returns>
    public static double GetWindowScalingFactor(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
        {
            // Handle cases where there is no window (e.g., pure console app)
            // Fallback to system metrics if necessary, or return a default.
            // Note: This fallback is less accurate for per-monitor scaling.
            return 1;
        }

        uint dpi = GetDpiForWindow(hWnd);

        if (dpi == 0)
        {
            // Fallback if the API call fails
            return 1;
        }

        return (double)dpi / DpiBase;
    }


}
