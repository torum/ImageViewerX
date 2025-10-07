using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageViewer.Models;

public class ImageInfo : ObservableObject
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
}


