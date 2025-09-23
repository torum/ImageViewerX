using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;

namespace ImageViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public static string Greeting => "Welcome to Avalonia!";

    private Bitmap? _diplayImage;
    public Bitmap? DiplayImage
    {
        get
        {
            return _diplayImage;
        }
        set
        {
            if (_diplayImage == value)
                return;

            _diplayImage = value;
            OnPropertyChanged(nameof(DiplayImage));
        }
    }

    public MainViewModel()
    {
        //Task.Run(Test);
    }

    private async Task Test()
    {
        await Task.Delay(5);

        Dispatcher.UIThread.Post(() =>
        {
            var filePath = "C:\\Users\\torum\\Pictures\\sample-1.webp";
            Bitmap? bitmap = new(filePath);
            DiplayImage = bitmap;
        });
        /*
        await Task.Delay(5000);

        Dispatcher.UIThread.Post(() =>
        {
            var filePath = "C:\\Users\\torum\\Desktop\\sample-1.webp";
            Bitmap? bitmap = new(filePath);
            DiplayImage = bitmap;
        });

        await Task.Delay(5000);

        Dispatcher.UIThread.Post(() =>
        {
            var filePath = "C:\\Users\\torum\\Desktop\\Slideshow3\\sample-1.webp";
            Bitmap? bitmap = new(filePath);
            DiplayImage = bitmap;
        });
        */
    }
}
