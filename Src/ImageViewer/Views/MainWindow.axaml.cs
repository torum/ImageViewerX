using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ImageViewer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        this.Loaded += OnLoaded;

        //this.ContentFrame.Content = (new MainView() as UserControl);

        this.PointerPressed += Window_PointerPressed;

        this.DoubleTapped += (_, _) =>
        {
            WindowState = (WindowState == WindowState.FullScreen) ? WindowState.Normal : WindowState.FullScreen;
        };

    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Get the window's platform handle
            var handle = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

            if (handle != IntPtr.Zero)
            {
                EnableBlurBehind(handle);
            }
        }

        Task.Run(Test);
    }

    private async Task Test()
    {
        await Task.Delay(500);

        Dispatcher.UIThread.Post(() =>
        {
            this.ContentFrame.Content = (new MainView() as UserControl);
        });
    }

    private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Call BeginMoveDrag to enable window dragging
            BeginMoveDrag(e);
        }
        else
        {

        }

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
}
