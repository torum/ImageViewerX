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

    public string? ImageFilePath { get; set; }

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
}


