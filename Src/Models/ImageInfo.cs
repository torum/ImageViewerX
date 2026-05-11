using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageViewer.Models;

public sealed partial class ImageInfo : ObservableObject
{
    public bool IsLoading { get; set; } = false;
    
    public bool IsAcquired { get; set; } = false;

    public string? ImageFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Bitmap? ImageSource { get; set; }

    [ObservableProperty]
    public partial double ImageWidth { get; set; }

    [ObservableProperty]
    public partial double ImageMaxWidth { get; set; }

    [ObservableProperty]
    public partial double ImageHeight { get; set; }

    [ObservableProperty]
    public partial double ImageMaxHeight { get; set; }

    [ObservableProperty]
    public partial Avalonia.Media.Stretch ImageStretch { get; set; } = Stretch.Uniform;

    [ObservableProperty]
    public partial Avalonia.Media.StretchDirection ImageStretchDirection { get; set; } = StretchDirection.DownOnly;
}


