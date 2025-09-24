using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using ImageViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ImageViewer.Views;

public partial class MainWindow : Window
{
    // Save "None" from xaml(Cursor="None").
    private readonly Cursor? _cursorNone;

    public MainWindow()
    {
        this.DataContext = App.GetService<MainViewModel>();

        InitializeComponent();

        if (this.Cursor is null)
        {
            Debug.WriteLine("null");
        }
        else
        {
            Debug.WriteLine($"Cursor: {this.Cursor}");  
        }

        // Save "None" from xaml(Cursor="None").
        _cursorNone = this.Cursor;
        // Set default cursor.
        this.Cursor = Cursor.Default;

        this.ContentFrame.Content = (new MainView() as UserControl);

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
            this.Background = new SolidColorBrush(Color.Parse("#131313"));
        }


        this.PropertyChanged += this.OnWindow_PropertyChanged;

    }

    /*

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if(change.Property.Name == nameof(WindowState))
        {
            Debug.WriteLine($"WindowState changed from {change.OldValue} to {change.NewValue}");
        }
        base.OnPropertyChanged(change);
    }

    */
    private void OnWindow_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (sender is Window)
        {
            //var oldState = (WindowState)e.OldValue;
            //var newState = (WindowState)e.NewValue;

            if (e.Property.Name == nameof(WindowState))
            {
                if (this.DataContext is not  MainViewModel vm)
                {
                    return;
                }

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
        }
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (WindowState == WindowState.Normal)
            {
                // Call BeginMoveDrag to enable window dragging
                BeginMoveDrag(e);
            }
        }
        else
        {

        }

        // Right clicked on Window.
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            if (sender is Control target)
            {
                var flyout = FlyoutBase.GetAttachedFlyout(target);

                // Show the flyout using the 'Placement="Pointer"' property.
                flyout?.ShowAt(target);
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
        // Check if the dropped data contains file paths
        if (e.Data.Contains(DataFormats.Files))
        {
            var fileNames = e.Data.GetFiles()?.ToList();
            if (fileNames is not null && fileNames.Count != 0)
            {
                var droppedFiles = new List<string>();

                foreach (var item in fileNames)
                {
                    if (File.Exists(item.Path.LocalPath))
                    {
                        // Add single files
                        droppedFiles.Add(item.Path.LocalPath);
                    }
                    else if (Directory.Exists(item.Path.LocalPath))
                    {
                        // Recursively get all files from a dropped folder
                        var filesInFolder = Directory.GetFiles(item.Path.LocalPath, "*", SearchOption.AllDirectories);
                        droppedFiles.AddRange(filesInFolder);
                    }
                    else 
                    {
                        Debug.WriteLine("else: " + item.Path.LocalPath);
                    }
                }
                
                /*
                foreach (var file in droppedFiles)
                {
                    //Debug.WriteLine(file);
                }
                */
                if (this.DataContext is MainViewModel vm)
                {
                    vm.DroppedFiles(droppedFiles);
                }
            }
        }
    }

    // Marshal.AllocHGlobal way.
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

    // Use NativeMemory.Alloc, instead of Marshal.AllocHGlobal
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

    private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //Task.Run(Test);
    }

    private void SetWindowStateFullScreen()
    {
        // set "None"
        this.Cursor = _cursorNone;

        // hack for CaptionButtons not dissapearing fast enough problem.
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
        Dispatcher.UIThread.Post(async () =>
        {
            await Task.Delay(20);
            //await Task.Yield();
            WindowState = WindowState.FullScreen;
        });
    }

    private void SetWindowStateNormal()
    {
        this.Cursor = Cursor.Default;

        WindowState = WindowState.Normal;
        ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
    }

    private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (WindowState == WindowState.FullScreen)
            {
                SetWindowStateNormal();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Space)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.SpaceKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Right)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.NextKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Left)
        {
            if (this.DataContext is MainViewModel vm)
            {
                vm.PrevKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.F)
        {
            if (this.DataContext is MainViewModel vm)
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
        }
    }

    private void Window_KeyUp(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Right)
        {
            if (this.DataContext is MainViewModel vm)
            {
                //vm.NextKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.Left)
        {
            if (this.DataContext is MainViewModel vm)
            {
                //vm.NextKeyPressed();

                e.Handled = true;
            }
        }
        else if (e.Key == Key.F)
        {
            if (this.DataContext is MainViewModel vm)
            {
                e.Handled = true;
            }
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
}

public static partial class NativeMethods
{
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

    /*
    // For Alt+Space sys menu.
    [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
    public static partial IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    */
}
