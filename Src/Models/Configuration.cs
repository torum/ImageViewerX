using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageViewer.Models;

public class Configuration
{
    public class WindowSettings
    {
        public double Width { get; set; } = 780;
        public double Height { get; set; } = 600;
        public int Left { get; set; } = 0;
        public int Top { get; set; } = 0;
        public WindowState WindowState { get; set; } = WindowState.Normal;

        public double RestoreWidth { get; set; } = 780;
        public double RestoreHeight { get; set; } = 600;
        public int RestoreLeft { get; set; } = 0;
        public int RestoreTop { get; set; } = 0;
    }

    public WindowSettings MainWindow { get; set; } = new WindowSettings();
    public bool IsSaveLog { get; set; }

    public string LastOpenedDirectory { get; set; } = string.Empty;

    public bool IsShuffleOn { get; set; } = false;
    public bool IsRepeatOn { get; set; } = false;
    public bool IsOverrideSystemDpiScalingFactorOn { get; set; } = false;
    public bool IsViewImageListOn { get; set; } = false;
    public bool IsViewFilePathPopupOn { get; set; } = false;
    public long SlideshowTimerInterval { get; set; } = 5;
    public bool IsStretchInOn { get; set; } = false;
    public bool IsStretchOutOn { get; set; } = false;
    public bool IsEffectFadeInAndOutOn { get; set; } = false;
    public bool IsEffectPageSlideOn { get; set; } = false;
    public bool IsNoEffectsOn { get; set; } = false;
    public bool IsEffectCrossfadeOn { get; set; } = false;



}
