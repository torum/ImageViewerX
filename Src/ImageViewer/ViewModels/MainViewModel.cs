using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ImageViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    //readonly Queue<int> _tasks = new();

    private readonly DispatcherTimer _timer;

    private List<string> _queue = [];
    private List<string> _originalQueue = [];
    //private List<string> _shuffledQueue = [];
    //private List<string> _sortedQueue = [];

    private int _crossfadeWaitDuration = 1000;//1000;
    private bool _isUseDummyNoOverrappingCrossfade = true;

    //private readonly System.Threading.Lock _lock = new();
    //private readonly SemaphoreSlim _semaphore = new(1, 1);

    private int QueueIndex = 0;
    private int _currentIndex = -1;
    //private bool _isBusy = false;

    private bool _isWorking;
    public bool IsWorking
    {
        get
        {
            return _isWorking;
        }
        set
        {
            if (_isWorking == value)
                return;

            _isWorking = value;
            OnPropertyChanged(nameof(IsWorking));
        }
    }

    private bool _isShuffle = false;

    private readonly string[] _validExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];
    public string[] ValidExtensions => _validExtensions;

    private bool _isFullscreen = false;
    public bool IsFullscreen
    {
        get => _isFullscreen;
        set
        {
            if (_isFullscreen == value)
                return;

            _isFullscreen = value;
            OnPropertyChanged(nameof(IsFullscreen));

            if (_isFullscreen)
            {
                if (!_timer.IsEnabled)
                {
                    if (_queue.Count > 0)
                    {
                        _timer.Start();
                        //await Show(_effectDuration);
                    }
                }
            }
            else 
            {
                if (_timer.IsEnabled)
                {
                    _timer.Stop();
                }
            }

        }
    }

    private Bitmap? _diplayImageDummy;

    private Bitmap? _diplayImage1;
    public Bitmap? DiplayImage1
    {
        get
        {
            return _diplayImage1;
        }
        set
        {
            if (_diplayImage1 == value)
                return;

            _diplayImage1 = value;
            OnPropertyChanged(nameof(DiplayImage1));
        }
    }

    private Bitmap? _diplayImage2;
    public Bitmap? DiplayImage2
    {
        get
        {
            return _diplayImage2;
        }
        set
        {
            if (_diplayImage2 == value)
                return;

            _diplayImage2 = value;
            OnPropertyChanged(nameof(DiplayImage2));
        }
    }

    public MainViewModel()
    {
        //Task.Run(Test);

        var uri = new Uri("avares://ImageViewer/Assets/Untitled.png");
        using var stream = AssetLoader.Open(uri);

        _diplayImageDummy = new Bitmap(stream);


        //Start();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(6)
        };
        _timer.Tick += OnTimerTick;

        // Start the timer
        //_timer.Start();

        
    }

    #region == Public Methods ==

    public async void DroppedFiles(List<string> files)
    {
        if (files == null)
        {
            return;
        }

        _timer.Stop();

        _queue = files;
        _originalQueue = files;


        QueueIndex = 0;

        if (_queue.Count > 0)
        {
            if (_isShuffle)
            {
                List<string> shuffledQueue = _originalQueue;
                shuffledQueue.Shuffle();
                _queue = shuffledQueue;
            }

            // no wait for transitional effect.
            await Show(1);
        }
    }

    public async void SpaceKeyPressed()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }
        else
        {
            if (_queue.Count > 0)
            {
                await Show(_crossfadeWaitDuration);
            }
        }
    }

    public async void NextKeyPressed()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }

        if (_queue.Count <= 0) return;

        //QueueIndex++;
        if ((QueueIndex) > (_queue.Count - 1)) return;

        /////////
        _isUseDummyNoOverrappingCrossfade = false;
        await Show(_crossfadeWaitDuration);
    }

    public async void PrevKeyPressed()
    {
        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }

        if (_queue.Count <= 0) return;

        int inx = 0;
        if ((QueueIndex - 2) > -1)
        {
            //QueueIndex -= 2;
            inx = QueueIndex - 2;
        }

        QueueIndex = inx;
        await Show(_crossfadeWaitDuration);
    }

    #endregion

    #region == Private Methods ==

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        // This code runs on the UI thread, so it's safe to update UI elements.
        if (_queue.Count <= 0) return;
        if (QueueIndex > (_queue.Count - 1)) return;

        await Show(_crossfadeWaitDuration);
    }

    private async Task Show(int crossfadeWaitDuration)
    {
        if (_queue.Count <= 0) return;
        if (QueueIndex > (_queue.Count - 1)) return;

        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }


        /*
        _tasks.Enqueue(QueueIndex++);

        //lock(_lock)
        //using (_lock.EnterScope())
        await _semaphore.WaitAsync();
        try
        {
            //_tasks.Enqueue(QueueIndex+1);

            //await Task.Delay(100);
        }
        finally
        {
            _semaphore.Release(); 
        }

        await ShowImage(wait);
        */

        var filePath = _queue[QueueIndex];

        // just in case, double check.
        if (HasImageExtension(filePath, _validExtensions) == false)
        {
            QueueIndex++;
            await Show(crossfadeWaitDuration);
            return;
        }

        //Task.Run(() => ShowImage(filePath));
        if (await ShowImage(filePath, crossfadeWaitDuration, _isUseDummyNoOverrappingCrossfade))
        {
            if (_isFullscreen)
            {
                _timer.Start();
            }

            //QueueIndex = index;
        }
    }

    private async Task<bool> ShowImage(string filePath, int crossfadeWaitDuration, bool useDummyNoOverrappingCrossfade)
    {
        int idx = QueueIndex;

        if (_currentIndex == idx)
        {
            Debug.WriteLine($"{idx} dupe skipping");
            QueueIndex++;
            return true;
        }

        _currentIndex = idx;

        Debug.WriteLine($"{idx} Enter critical section.");


        Bitmap? bitmap = new(filePath);

        if (useDummyNoOverrappingCrossfade)
        {
            DiplayImage1 = _diplayImageDummy;
            await Task.Delay(crossfadeWaitDuration).ConfigureAwait(true);
        }
        DiplayImage1 = bitmap;

        //QueueIndex++;
        QueueIndex = idx+1;
        Debug.WriteLine($"{idx} Exit critical section.");

        if (_isFullscreen)
        {
            _timer.Start();
        }

        return true;
    }

    public static bool HasImageExtension(string fileName, string[] extensions)
    {
        string extension = System.IO.Path.GetExtension(fileName);

        foreach (string validExt in extensions)
        {
            if (string.Equals(extension, validExt, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private async void Start()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(6));

        while (await timer.WaitForNextTickAsync())
        {
            if (_queue.Count <= 0) continue;
            if (QueueIndex > (_queue.Count - 1)) continue;

            var filePath = _queue[QueueIndex];
            if (HasImageExtension(filePath, _validExtensions) == false)
            {
                continue;
            }

            //Bitmap? bitmap = new(filePath);
            //DiplayImage = bitmap;
            //ShowImage(filePath);

            QueueIndex++;
        }
    }

    private async Task Test()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(6));

        while (await timer.WaitForNextTickAsync())
        {
            Debug.WriteLine($"Test {Thread.CurrentThread.Name} : {_queue.Count}");
            if (_queue.Count <= 0) continue;
            if (QueueIndex > (_queue.Count - 1)) continue;

            Dispatcher.UIThread.Post(() =>
            {
                //var filePath = _queue[QueueIndex];

                //Bitmap? bitmap = new(filePath);
                //DiplayImage = bitmap;
            });
            QueueIndex++;
        }

    }

    #endregion
}

public static class ListExtensions
{
    private static readonly Random rng = new();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            // Swap the elements
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
}