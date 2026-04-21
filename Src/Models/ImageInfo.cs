using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageViewer.Models;

public sealed class ImageInfo : ObservableObject
{
    public bool IsLoading { get; set; } = false;
    
    public bool IsAcquired { get; set; } = false;

    public string? ImageFilePath { get; set; } = string.Empty;

    //public int BinarySize { get; set; }

    public Bitmap? ImageSource
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public double ImageWidth
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public double ImageMaxWidth
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public double ImageHeight
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public double ImageMaxHeight
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    }

    public Avalonia.Media.Stretch ImageStretch
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    } = Stretch.Uniform;

    public Avalonia.Media.StretchDirection ImageStretchDirection
    {
        get;
        set
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged();
        }
    } = StretchDirection.DownOnly;
}


