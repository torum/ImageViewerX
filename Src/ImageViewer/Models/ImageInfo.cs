using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageViewer.Models;

public sealed class ImageInfo : ObservableObject
{
    public bool IsLoading { get; set; } = false;
    
    public bool IsAcquired { get; set; } = false;

    public string? ImageFilePath { get; set; } = string.Empty;

    public int BinarySize { get; set; }

    private Bitmap? _imageSource;
    public Bitmap? ImageSource
    {
        get
        {
            return _imageSource;
        }
        set
        {
            if (_imageSource == value)
                return;

            _imageSource = value;
            OnPropertyChanged(nameof(ImageSource));
        }
    }

    private double _imageWidth;
    public double ImageWidth
    {
        get => _imageWidth;
        set
        {
            if (_imageWidth == value)
                return;

            _imageWidth = value;
            OnPropertyChanged(nameof(ImageWidth));
        }
    }

    private double _imageMaxWidth;
    public double ImageMaxWidth
    {
        get => _imageMaxWidth;
        set
        {
            if (_imageMaxWidth == value)
                return;

            _imageMaxWidth = value;
            OnPropertyChanged(nameof(ImageMaxWidth));
        }
    }

    private double _imageHeight;
    public double ImageHeight
    {
        get => _imageHeight;
        set
        {
            if (_imageHeight == value)
                return;

            _imageHeight = value;
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    private double _imageMaxHeight;
    public double ImageMaxHeight
    {
        get => _imageMaxHeight;
        set
        {
            if (_imageMaxHeight == value)
                return;

            _imageMaxHeight = value;
            OnPropertyChanged(nameof(ImageMaxHeight));
        }
    }

    private Avalonia.Media.Stretch _imageStretch = Stretch.Uniform;
    public Avalonia.Media.Stretch ImageStretch
    {
        get => _imageStretch;
        set
        {
            if (_imageStretch == value)
                return;

            _imageStretch = value;
            OnPropertyChanged(nameof(ImageStretch));
        }
    }

    private Avalonia.Media.StretchDirection _imageStretchDirection = StretchDirection.DownOnly;
    public Avalonia.Media.StretchDirection ImageStretchDirection
    {
        get => _imageStretchDirection;
        set
        {
            if (_imageStretchDirection == value)
                return;

            _imageStretchDirection = value;
            OnPropertyChanged(nameof(ImageStretchDirection));
        }
    }
}


