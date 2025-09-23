using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ImageViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.ContentFrame.Content = (new MainView() as UserControl);

        this.Loaded += OnLoaded;

        this.PointerPressed += Window_PointerPressed;

        this.DoubleTapped += (_, _) =>
        {
            //WindowState = (WindowState == WindowState.FullScreen) ? WindowState.Normal : WindowState.FullScreen;

            if (WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            }
            else if (WindowState == WindowState.Normal)
            {
                // hack for CaptionButtons not dissapearing fast enough problem.
                ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
                Dispatcher.UIThread.Post(async() =>
                {
                    await Task.Delay(20);
                    //await Task.Yield();
                    WindowState = WindowState.FullScreen;
                });
            }
        };

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
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        //Task.Run(Test);
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
