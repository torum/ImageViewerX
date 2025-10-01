using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ImageViewer.Models;
using ImageViewer.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ImageViewer.Views;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _timerPointerCursorHide;

    public MainWindow()
    {
        this.DataContext = App.GetService<MainViewModel>();

        LoadSettings();

        InitializeComponent();

        InitBackground();

        this.ContentFrame.Content = App.GetService<MainView>();

        this.PropertyChanged += this.OnWindow_PropertyChanged;

        (this.DataContext as MainViewModel)!.QueueHasBeenChanged += OnQueueHasBeenChanged;
        (this.DataContext as MainViewModel)!.SlideshowStatusChanged += OnSlideshowStatusChanged;
        (this.DataContext as MainViewModel)!.QueueLoaded += OnQueueLoaded;

        this.ActualThemeVariantChanged += OnActualThemeVariantChanged;

        _timerPointerCursorHide = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        _timerPointerCursorHide.Tick += OnTimerTick;

        // TODO: more
        InitKeyBindigs();
    }
    private void OnQueueLoaded(object? sender, EventArgs e)
    {
        this.WelcomeMessageGrid.IsVisible = false;
    }
    
    private void OnTimerTick(object? sender, EventArgs e)
    {
        // This code runs on the UI thread, so it's safe to update UI elements.

        this.Cursor = new Cursor(StandardCursorType.None);
    }

    private void OnQueueHasBeenChanged(object? sender, int ind)
    {
        UpdateQueueListBoxImages(ind);
    }

    private void OnSlideshowStatusChanged(object? sender, EventArgs e)
    {
        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        if (this.WindowState != WindowState.FullScreen)
        {
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (vm.IsSlideshowOn)
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
            //Debug.WriteLine($"WindowState changed from {e.OldValue} to {e.NewValue}");
            vm.IsFullscreen = true;
        }
        else if (e.NewValue is WindowState.Normal)
        {
            vm.IsFullscreen = false;
        }
        else if (e.NewValue is WindowState.Minimized)
        {
            vm.IsFullscreen = false;
        }
        else
        {
            vm.IsFullscreen = false;
        }
    }

    private void InitBackground()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // There is some issue showing sysmenu in AvaloniaUI.
            //TryRegisterWindowsMenu();

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 1803))
            {
                // Get the window's platform handle
                var handle = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                if (handle != IntPtr.Zero)
                {
                    this.Background = Brushes.Transparent;
                    this.TransparencyLevelHint = [WindowTransparencyLevel.None];

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
                this.TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur];//"AcrylicBlur"
                //this.TransparencyLevelHint = [WindowTransparencyLevel.Mica];
            }
        }
        else
        {
            //this.Background = new SolidColorBrush(Color.Parse("#131313"));

            this.BackgroundLayerBorder.IsVisible = true;
            if ((App.Current as App)!.RequestedThemeVariant == ThemeVariant.Dark)
            {
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#222222"));
            }
            else if ((App.Current as App)!.RequestedThemeVariant == ThemeVariant.Light)
            {
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        var newTheme = ActualThemeVariant;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {

        }
        else
        {
            this.BackgroundLayerBorder.IsVisible = true;
            if (newTheme == ThemeVariant.Dark)
            {
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#222222"));
            }
            else if (newTheme == ThemeVariant.Light)
            {
                this.BackgroundLayerBorder.Background = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }
        }
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
                if (hoge.Value == "Maximized")
                {
                    // Since there is no restorebounds in AvaloniaUI.
                    windowState = WindowState.Normal;
                }
                else if (hoge.Value == "Normal")
                {
                    windowState = WindowState.Normal;
                }
                else if (hoge.Value == "Minimized")
                {
                    windowState = WindowState.Normal;
                }
            }
        }

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
        }

        #endregion
    }

    private void InitKeyBindigs()
    {
        // TODO: more

        var ToggleSlideshowCommandKeyBinding = new KeyBinding
        {
            Gesture = new KeyGesture(Avalonia.Input.Key.Space, KeyModifiers.None),
            Command = (this.DataContext as MainViewModel)!.ToggleSlideshowCommand
        };

        // InputGesture only. Command binding is done already in xaml.
        //this.MenuItemStartSlideshow.KeyBindings.Add(SpaceKeyBinding);
        this.MenuItemStartSlideshow.InputGesture = new KeyGesture(Avalonia.Input.Key.Space, KeyModifiers.None);

        // set keybinding here instead of in Window_KeyDown.
        this.KeyBindings.Add(ToggleSlideshowCommandKeyBinding);

    }

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        // Too late to change window size etc. Move to constructor.
        //LoadSettings();
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        SaveSettings();
    }

    private void SaveSettings()
    {
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
        if (this.WindowState == WindowState.Maximized)
        {
            //attrs.Value = w.RestoreBounds.Height.ToString();
        }
        else
        {
            attrs.Value = this.Height.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("width");
        if (this.WindowState == WindowState.Maximized)
        {
            //attrs.Value = w.RestoreBounds.Width.ToString();
            //windowWidth = w.RestoreBounds.Width;
        }
        else
        {
            attrs.Value = this.Width.ToString();
            //windowWidth = this.Width;

        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("top");
        if (this.WindowState == WindowState.Maximized)
        {
            //attrs.Value = w.RestoreBounds.Top.ToString();
        }
        else
        {
            //attrs.Value = w.Top.ToString();
            attrs.Value = this.Position.Y.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("left");
        if (this.WindowState == WindowState.Maximized)
        {
            //attrs.Value = w.RestoreBounds.Left.ToString();
        }
        else
        {
            //attrs.Value = w.Left.ToString();
            attrs.Value = this.Position.X.ToString();
        }
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("state");
        if (this.WindowState == WindowState.Maximized)
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

        try
        {
            if (!Directory.Exists(App.AppDataFolder))
            {
                Directory.CreateDirectory(App.AppDataFolder);
            }

            doc.Save(App.AppConfigFilePath);
        }
        //catch (System.IO.FileNotFoundException) { }
        catch (Exception ex)
        {
            if (vm.IsSaveLog)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@OnWindowClosing", ex.Message);
                });
            }
        }
    }

    private void Window_Closed(object? sender, System.EventArgs e)
    {
        // Cleanup?
    }

    private void Window_Resized(object? sender, Avalonia.Controls.WindowResizedEventArgs e)
    {
        UpdateQueueListBoxImages();
    }

    private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
    {
        if (this.WindowState == WindowState.FullScreen)
        {
            if (_timerPointerCursorHide.IsEnabled)
            {
                _timerPointerCursorHide.Stop();
            }

            if (this.DataContext is MainViewModel vm)
            {
                if (vm.IsWorking)
                {
                    this.Cursor = new Cursor(StandardCursorType.AppStarting);
                }
                else
                {
                    this.Cursor = Cursor.Default;
                }
            }
            else
            {
                this.Cursor = Cursor.Default;
            }

             _timerPointerCursorHide.Start();
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
                this.Cursor = new Cursor(StandardCursorType.None);
            }
        }

        // Right clicked on Window.
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (sender is Control target)
            {
                var flyout = FlyoutBase.GetAttachedFlyout(target);

                // Show the flyout using the 'Placement="Pointer"' property.
                flyout?.ShowAt(target);

                if (this.WindowState == WindowState.FullScreen)
                {
                    if (this.DataContext is MainViewModel vm)
                    {
                        if (vm.IsWorking)
                        {
                            this.Cursor = new Cursor(StandardCursorType.AppStarting);
                        }
                        else
                        {
                            this.Cursor = Cursor.Default;
                        }
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

        // Only allow copy effect for file drops
        e.DragEffects = e.Data.Contains(DataFormats.Files)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        if (vm.IsWorking)
        {
            // Already processing. 
            return;
        }

        // Check if the dropped data contains file paths
        if (e.Data.Contains(Avalonia.Input.DataFormats.Files))
        {
            var fileNames = e.Data.GetFiles()?.ToList();
            if (fileNames is not null && fileNames.Count != 0)
            {
                var droppedFiles = new List<string>();
                foreach (var file in fileNames)
                {
                    var filePath = file.TryGetLocalPath();
                    if (filePath != null) 
                    {
                        droppedFiles.Add(filePath);
                    }
                }

                if (droppedFiles.Count > 0)
                {
                    //_ = ProcessFiles(droppedFiles);
                    ProcessFiles(droppedFiles);
                }

                // Fire and forget.
                //_ = ProcessFiles(fileNames);
                ////ProcessFiles(fileNames.Select(x => x.Path.LocalPath).ToList);
            }
        }
    }

    private void ProcessFiles(List<string> fileNames)
    {
        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        vm.IsWorking = true;
        //await Task.Yield();

        // Don't await. FIRE and FORGET! Otherwise GUI would freeze or be 100x slower.
        // Don't _ =, nor await = . at all. 
        Task.Run(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                vm.IsWorking = true;
                //await Task.Yield();
            });

            var validExt = vm.ValidExtensions;

            try
            {
                var droppedFiles = new List<string>();
                var isSingleFileDropped = false;

                if (fileNames.Count == 1)
                {
                    if (System.IO.File.Exists(fileNames[0]))//.Path.LocalPath
                    {
                        isSingleFileDropped = true;
                    }
                }

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
                        droppedFiles.AddRange(filesInFolder);
                    }
                    else
                    {
                        Debug.WriteLine("else: " + item);//.Path.LocalPath
                    }
                }

                // Single file dropped, in that case, get all siblings.
                if ((droppedFiles.Count == 1) && isSingleFileDropped)
                {
                    if (System.IO.File.Exists(droppedFiles[0]))
                    {
                        var originalFile = droppedFiles[0];

                        // Get parent dir.
                        string? parentFolderPath = System.IO.Path.GetDirectoryName(droppedFiles[0]);
                        if (parentFolderPath is not null)
                        {
                            if (Directory.Exists(parentFolderPath))
                            {
                                // NON-Recursively get all files from the folder
                                var filesInFolder = Directory.GetFiles(parentFolderPath, "*", SearchOption.TopDirectoryOnly);
                                droppedFiles.AddRange(filesInFolder);

                                if (droppedFiles.Count > 1)
                                {
                                    // Sort to move the first instance of 'originalFile' to the front, followed by other files.
                                    // Using `Distinct()` will remove the remaining duplicates.
#pragma warning disable IDE0305
                                    droppedFiles = droppedFiles.OrderBy(x => x == originalFile ? 0 : 1).Distinct().ToList();
#pragma warning restore IDE0305
                                }
                            }
                        }
                    }
                }

                var droppedImages = new List<ImageInfo>();

                foreach (var file in droppedFiles)
                {
                    if (!MainViewModel.HasImageExtension(file, validExt))
                    {
                        continue;
                    }

                    // Avoid MacOS's garbage. Use char overload for faster comp if possible.
                    if ((file.StartsWith('.')) || (file.StartsWith("._")))
                    {
                        continue;
                    }

                    var img = new ImageInfo
                    {
                        ImageFilePath = file
                    };

                    droppedImages.Add(img);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    vm.DroppedFiles(droppedImages);
                });
            }
            catch
            {
                // TODO: log error and show error message.
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    vm.IsWorking = false;
                    //await Task.Yield();
                });
            }
        });
    } //Avalonia.Platform.Storage.IStorageItem

    private void Window_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        //WindowState = (WindowState == WindowState.FullScreen) ? WindowState.Normal : WindowState.FullScreen;

        if (WindowState == WindowState.FullScreen)
        {
            SetWindowStateNormal();
        }
        else if (WindowState == WindowState.Normal)
        {
            SetWindowStateFullScreen();
        }
    }

    private void SetWindowStateFullScreen()
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (this.DataContext is MainViewModel vm)
                {
                    if (vm.IsSlideshowOn)
                    {
                        Debug.WriteLine("SetThreadExecutionState set @SetWindowStateFullScreen");
                        NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED | NativeMethods.ES_DISPLAY_REQUIRED);
                    }
                }
            }
        });
    }

    private void SetWindowStateNormal()
    {
        if (this.DataContext is MainViewModel vm)
        {
            if (vm.IsWorking)
            {
                this.Cursor = new Cursor(StandardCursorType.AppStarting);
            }
            else
            {
                this.Cursor = Cursor.Default;
            }
        }
        else
        {
            this.Cursor = Cursor.Default;
        }

        this.WindowState = WindowState.Normal;

        this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;

        if (_timerPointerCursorHide.IsEnabled)
        {
            _timerPointerCursorHide.Stop();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (this.DataContext is not MainViewModel vms)
            {
                return;
            }

            if (vms.IsSlideshowOn)
            {
                Debug.WriteLine("SetThreadExecutionState off @SetWindowStateNormal()");
                NativeMethods.SetThreadExecutionState(NativeMethods.ES_CONTINUOUS);
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
            if (this.DataContext is MainViewModel)
            {
                //vm.SpaceKeyPressed();

                //e.Handled = true;
            }
        }
        else if (e.Key == Avalonia.Input.Key.Right)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.NextKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Avalonia.Input.Key.Left)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.PrevKeyPressed();

                e.Handled = true;
            }
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
        } else if (e.Key == Key.Right)
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
                //vm.NextKeyPressed();

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
    }

    private void Window_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        if (this.DataContext is not MainViewModel vm)
        {
            return;
        }

        if (e.Delta.Y > 0) // Scroll up
        {
            vm.PrevKeyPressed();
        }
        else if (e.Delta.Y < 0) // Scroll down
        {
            //
            vm.NextKeyPressed();
        }

        e.Handled = true;
    }

    private void ListBox_PointerWheelChanged(object? sender, Avalonia.Input.PointerWheelEventArgs e)
    {
        e.Handled = true;
    }

    private void ListBox_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is not ListBox lb)
        {
            return;
        }

        if (lb.SelectedItem is not ImageInfo item)
        {
            return;
        }

        if (this.DataContext is MainViewModel vm)
        {
            vm.ListBoxItemSelected(item);
        }
    }

    private void ListBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            this.Focus();
            e.Handled = true;
        }
    }

    private void ListBox_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            e.Handled = true;
        }
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
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var c = vm.Queue.Count;

        if ((selectedIndex > c) && (selectedIndex < 0))
        {
            return;
        }

        await Task.Yield();
        //await Task.Delay(800); // Need to wait for UI to update
        Dispatcher.UIThread.Post(() =>
        {
            if (this.QueueListBox is ListBox lb)
            {
                if (lb.ItemCount < (selectedIndex + 1))
                {
                    return;
                }

                lb.ScrollIntoView(selectedIndex);

                if (DataContext is not MainViewModel vm)
                {
                    return;
                }

                var test = vm?.Queue[selectedIndex];
                if (test != null)
                {
                    lb.SelectedItem = test;
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
                UpdateVisibleItems(scrollViewer, virtualPanel);
            }
        });
    }

    private async void UpdateQueueListBoxImages()
    {
        await Task.Yield();
        //await Task.Delay(800); // Need to wait for UI to update

        Dispatcher.UIThread.Post(() =>
        {
            if (this.QueueListBox is ListBox lb)
            {
                if (DataContext is not MainViewModel vm)
                {
                    return;
                }
                var item = lb.SelectedItem;
                if (item is ImageInfo img)
                {
                    var ind = vm.Queue.IndexOf(img);
                    if (ind > 0)
                    {
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
                        UpdateVisibleItems(scrollViewer, virtualPanel);
                    }
                }

            }
        });
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
        // TODO: remember the last picked folder path.

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
                FilePickerFileTypes.ImageAll // All image types
            ]
        };

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
                ProcessFiles(droppedFiles);
            }
        }
    }

    private async void Button_FolderPick_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await SelectFolder();
    }

    public async Task SelectFolder()
    {
        // TODO: remember the last picked folder.

        // Get the IStorageProvider for the current window
        var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;

        if (storageProvider == null) return; // Storage provider not available on this platform

        // Configure the options for the folder picker
        var options = new FolderPickerOpenOptions
        {
            Title = "Select Folders",
            AllowMultiple = true // Set to true to allow multiple folders
        };

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
                ProcessFiles(droppedFiles);
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
