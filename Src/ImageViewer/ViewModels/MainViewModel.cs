using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageViewer.Models;
using ImageViewer.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace ImageViewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region == Private ==

    private CancellationTokenSource _cts = new();
    //private static readonly CancellationToken token = _cts.Token;

    private int _queueIndex = 0;
    private string _currentFile = string.Empty;
    private readonly DispatcherTimer _timerSlideshow;
    //private readonly System.Threading.Lock _lock = new();
    //private bool _isBusy = false;
    private List<ImageInfo> _originalQueue = [];

    #endregion

    #region == Internal/Bind Properties ==

    private ObservableCollection<ImageInfo> _queue = [];
    public ObservableCollection<ImageInfo> Queue
    {
        get
        {
            return _queue;
        }
        set
        {
            if (_queue == value)
                return;

            _queue = value;
            OnPropertyChanged(nameof(Queue));
        }
    }

    private ImageInfo? _selectedQueueImage;
    public ImageInfo? SelectedQueueImage
    {
        get
        {
            return _selectedQueueImage;
        }
        set
        {
            if (_selectedQueueImage == value)
                return;

            _selectedQueueImage = value;
            OnPropertyChanged(nameof(SelectedQueueImage));
        }
    }

    private bool _isTransitionReversed = false;
    public bool IsTransitionReversed
    {
        get
        {
            return _isTransitionReversed;
        }
        set
        {
            if (_isTransitionReversed == value)
                return;

            _isTransitionReversed = value;
            OnPropertyChanged(nameof(IsTransitionReversed));
        }
    }

    private IEnumerable<object>? _visibleItemsImageInfo;
    public IEnumerable<object>? VisibleItemsImageInfo
    {
        get => _visibleItemsImageInfo;
        set
        {
            _visibleItemsImageInfo = value;

            if (_visibleItemsImageInfo is null)
            {
                return;
            }

            // Don't await here. Fire and forget. No _ = either.
            _ = Task.Run(() => GetPictures(_visibleItemsImageInfo));
        }
    }

    private ImageInfo? _displayImage;
    public ImageInfo? DiplayImage
    {
        get
        {
            return _displayImage;
        }
        set
        {
            if (_displayImage == value)
                return;

            _displayImage = value;
            OnPropertyChanged(nameof(DiplayImage));
        }
    }

    private double _systemDpiScalingFactor = 1;
    public double SystemDpiScalingFactor
    {
        get => _systemDpiScalingFactor;
        set
        {
            if (_systemDpiScalingFactor == value)
                return;

            _systemDpiScalingFactor = value;
            OnPropertyChanged(nameof(SystemDpiScalingFactor));
        }
    }

    private readonly string[] _validExtensions = [".jpg", ".jpeg", ".gif", ".png", ".webp", ".bmp"]; //, ".avif"
    public string[] ValidExtensions => _validExtensions;

    // internal state.
    private bool _isFullscreen = false;
    public bool IsFullscreen
    {
        get => _isFullscreen;
        set
        {
            if (_isFullscreen == value)
                return;

            _isFullscreen = value;
            OnPropertyChanged(nameof(IsFullscreenOn));

            // update opts control
            IsFullscreenOn = _isFullscreen;
        }
    }

    // When busy, it's busy processing file in/out the background thread.
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

            if (!_isFullscreen)
            {
                // Only non fullscreen mode since IsWorking is binding to busy cursor.
                OnPropertyChanged(nameof(IsWorking));
            }
        }
    }

    private bool _isQueueListBoxVisible = true;
    public bool IsQueueListBoxVisible
    {
        get { return _isQueueListBoxVisible; }
        set
        {
            if (_isQueueListBoxVisible == value)
                return;

            _isQueueListBoxVisible = value;

            OnPropertyChanged(nameof(IsQueueListBoxVisible));
        }
    }

    private bool _isSaveLog;
    public bool IsSaveLog
    {
        get { return _isSaveLog; }
        set
        {
            if (_isSaveLog == value)
                return;

            _isSaveLog = value;

            OnPropertyChanged(nameof(IsSaveLog));
        }
    }

    #endregion

    #region == Properties User Opts ==

    private int _slideshowTimerInterval = 4;
    public int SlideshowTimerInterval
    {
        get
        {
            return _slideshowTimerInterval;
        }
        set
        {
            if (_slideshowTimerInterval == value)
                return;

            _slideshowTimerInterval = value;
            OnPropertyChanged(nameof(SlideshowTimerInterval));
        }
    }

    private bool _isNoEffectsOn = false;
    public bool IsNoEffectsOn
    {
        get
        {
            return _isNoEffectsOn;
        }
        set
        {
            if (_isNoEffectsOn == value)
                return;

            _isNoEffectsOn = value;
            OnPropertyChanged(nameof(IsNoEffectsOn));
            OnPropertyChanged(nameof(DataNoEffectsIcon));

            TransitionsHasBeenChanged?.Invoke(this, EventArgs.Empty);

            //
            ToggleCrossfadeCommand.NotifyCanExecuteChanged();
        }
    }

    private bool _isEffectCrossfadeOn = true;
    public bool IsEffectCrossfadeOn
    {
        get
        {
            return _isEffectCrossfadeOn;
        }
        set
        {
            if (_isEffectCrossfadeOn == value)
                return;

            _isEffectCrossfadeOn = value;
            OnPropertyChanged(nameof(IsEffectCrossfadeOn));
            OnPropertyChanged(nameof(DataCrossfadeIcon));

            TransitionsHasBeenChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isEffectFadeInAndOutOn = true;
    public bool IsEffectFadeInAndOutOn
    {
        get
        {
            return _isEffectFadeInAndOutOn;
        }
        set
        {
            if (_isEffectFadeInAndOutOn == value)
                return;

            _isEffectFadeInAndOutOn = value;
            OnPropertyChanged(nameof(IsEffectFadeInAndOutOn));
            OnPropertyChanged(nameof(DataFadeInFadeOutIcon));

            TransitionsHasBeenChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isEffectPageSlideOn = false;
    public bool IsEffectPageSlideOn
    {
        get
        {
            return _isEffectPageSlideOn;
        }
        set
        {
            if (_isEffectPageSlideOn == value)
                return;

            _isEffectPageSlideOn = value;
            OnPropertyChanged(nameof(IsEffectPageSlideOn));
            OnPropertyChanged(nameof(DataPageSlideIcon));

            TransitionsHasBeenChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isShuffleOn = false;
    public bool IsShuffleOn
    {
        get
        {
            return _isShuffleOn;
        }
        set
        {
            if (_isShuffleOn == value)
                return;

            _isShuffleOn = value;

            OnPropertyChanged(nameof(IsShuffleOn));
            OnPropertyChanged(nameof(DataShuffleIcon));

            HideFlyout?.Invoke(this, EventArgs.Empty);

            if (_queue.Count > 0)
            {
                IsWorking = true;

                // Slideshow Timer temp stop.
                if (_timerSlideshow.IsEnabled)
                {
                    _timerSlideshow.Stop();
                }

                _queueIndex = 0;
                SelectedQueueImage = null;

                if (_isShuffleOn)
                {
                    _queue = new ObservableCollection<ImageInfo>(_originalQueue);
                    _queue.Shuffle();
                }
                else
                {
                    _queue = new ObservableCollection<ImageInfo>(_originalQueue);
                }

                if (!string.IsNullOrEmpty(_currentFile))
                {
                    //Debug.WriteLine(_currentFile);
                    var cur = _queue.FirstOrDefault(x => x.ImageFilePath == _currentFile);

                    // clear here.
                    //_currentFile = string.Empty;

                    if (cur is not null)
                    {
                        _queueIndex = Queue.IndexOf(cur);

                        //Debug.WriteLine(Queue[_queueIndex].ImageFilePath);
                    }
                }

                //_currentFile = string.Empty;

                OnPropertyChanged(nameof(Queue));

                QueueHasBeenChanged?.Invoke(this, _queueIndex);

                _queueIndex++;

                IsWorking = false;

                // Slideshow timer restart.
                if (IsSlideshowOn)
                {
                    _timerSlideshow.Start();
                }
            }
        }
    }

    private bool _isRepeatOn = false;
    public bool IsRepeatOn
    {
        get
        {
            return _isRepeatOn;
        }
        set
        {
            if (_isRepeatOn == value)
                return;

            _isRepeatOn = value;
            OnPropertyChanged(nameof(IsRepeatOn));
            OnPropertyChanged(nameof(DataRepeatIcon));

            HideFlyout?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isStayOnTop = false;
    public bool IsStayOnTop
    {
        get
        {
            return _isStayOnTop;
        }
        set
        {
            if (_isStayOnTop == value)
                return;

            _isStayOnTop = value;
            OnPropertyChanged(nameof(IsStayOnTop));
            OnPropertyChanged(nameof(DataStayOnTopIcon));

            HideFlyout?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isSlideshowOn = false;
    public bool IsSlideshowOn
    {
        get
        {
            return _isSlideshowOn;
        }
        set
        {
            if (_isSlideshowOn == value)
                return;

            _isSlideshowOn = value;
            OnPropertyChanged(nameof(IsSlideshowOn));
            OnPropertyChanged(nameof(DataPlayPauseIcon));
            OnPropertyChanged(nameof(SlideshowStartStopString));

            SlideshowStatusChanged?.Invoke(this, EventArgs.Empty);
            HideFlyout?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isFullscreenOn = false;
    public bool IsFullscreenOn
    {
        get => _isFullscreenOn;
        set
        {
            if (_isFullscreenOn == value)
                return;

            _isFullscreenOn = value;
            OnPropertyChanged(nameof(IsFullscreenOn));
            OnPropertyChanged(nameof(DataFullscreenOnIcon));

            Fullscreen?.Invoke(this, IsFullscreenOn);
            HideFlyout?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool _isOverrideSystemDpiScalingFactorOn = false;
    public bool IsOverrideSystemDpiScalingFactorOn
    {
        get => _isOverrideSystemDpiScalingFactorOn;
        set
        {
            if (_isOverrideSystemDpiScalingFactorOn == value)
                return;

            _isOverrideSystemDpiScalingFactorOn = value;
            OnPropertyChanged(nameof(IsOverrideSystemDpiScalingFactorOn));
            OnPropertyChanged(nameof(DataSystemDpiScalingFactorOnIcon));

            if (_queue.Count > 0)
            {
                foreach (var item in _queue)
                {
                    if (item.ImageSource is not null)
                    {
                        item.ImageSource = null;
                        item.ImageWidth = 0;
                        item.ImageHeight = 0;
                        item.IsAcquired = false;
                        item.IsLoading = false;
                    }
                }

                // Reload image.

                //OnPropertyChanged(nameof(Queue));
                if ((_queueIndex - 1) >= 0)
                {
                    //QueueHasBeenChanged?.Invoke(this, _queueIndex -1);
                    _queueIndex--;
                }
                else
                {
                    //QueueHasBeenChanged?.Invoke(this, 0);
                    _queueIndex = 0;
                }

                _ = Show();
            }
        }
    }

    private readonly string _play = "M5.74514 3.06445C5.41183 2.87696 5 3.11781 5 3.50023V12.5005C5 12.8829 5.41182 13.1238 5.74512 12.9363L13.7454 8.43631C14.0852 8.24517 14.0852 7.75589 13.7454 7.56474L5.74514 3.06445ZM4 3.50023C4 2.35298 5.2355 1.63041 6.23541 2.19288L14.2357 6.69317C15.2551 7.26664 15.2551 8.73446 14.2356 9.3079L6.23537 13.8079C5.23546 14.3703 4 13.6477 4 12.5005V3.50023Z";
    private readonly string _pause = "M3.75 2C2.7835 2 2 2.7835 2 3.75V12.25C2 13.2165 2.7835 14 3.75 14H5.25C6.2165 14 7 13.2165 7 12.25V3.75C7 2.7835 6.2165 2 5.25 2H3.75ZM3 3.75C3 3.33579 3.33579 3 3.75 3H5.25C5.66421 3 6 3.33579 6 3.75V12.25C6 12.6642 5.66421 13 5.25 13H3.75C3.33579 13 3 12.6642 3 12.25V3.75ZM10.75 2C9.7835 2 9 2.7835 9 3.75V12.25C9 13.2165 9.7835 14 10.75 14H12.25C13.2165 14 14 13.2165 14 12.25V3.75C14 2.7835 13.2165 2 12.25 2H10.75ZM10 3.75C10 3.33579 10.3358 3 10.75 3H12.25C12.6642 3 13 3.33579 13 3.75V12.25C13 12.6642 12.6642 13 12.25 13H10.75C10.3358 13 10 12.6642 10 12.25V3.75Z";
    public string DataPlayPauseIcon
    {
        get
        {
            if (IsSlideshowOn)
            {
                return _pause;
            }
            else
            {
                return _play;
            }
        }
    }

    private readonly string _shuffleOn = "M12.3536 3.64645C12.1583 3.45118 11.8417 3.45118 11.6464 3.64645C11.4512 3.84171 11.4512 4.15829 11.6464 4.35355L12.2961 5.00321C9.97191 5.0767 8.51587 6.40057 7.20029 7.59672L7.16366 7.63003C5.77688 8.89074 4.53421 10 2.5 10C2.22386 10 2 10.2239 2 10.5C2 10.7761 2.22386 11 2.5 11C4.94373 11 6.44607 9.63403 7.79971 8.40327L7.83634 8.36997C9.17448 7.15347 10.3784 6.078 12.2888 6.00405L11.6464 6.64645C11.4512 6.84171 11.4512 7.15829 11.6464 7.35355C11.8417 7.54882 12.1583 7.54882 12.3536 7.35355L13.8536 5.85355C14.0488 5.65829 14.0488 5.34171 13.8536 5.14645L12.3536 3.64645ZM2.5 5C4.32898 5 5.63063 5.76516 6.73729 6.66612L6.49099 6.89009C6.31609 7.04909 6.14783 7.20111 5.98421 7.34542C4.99915 6.56432 3.95092 6 2.5 6C2.22386 6 2 5.77614 2 5.5C2 5.22386 2.22386 5 2.5 5ZM12.2961 10.9968C10.5735 10.9423 9.3278 10.201 8.26272 9.33388L8.50901 9.10991C8.68391 8.95091 8.85217 8.79889 9.01579 8.65458C9.95241 9.39727 10.9461 9.94398 12.2888 9.99595L11.6464 9.35355C11.4512 9.15829 11.4512 8.84171 11.6464 8.64645C11.7437 8.5492 11.871 8.50038 11.9985 8.5C12.127 8.49962 12.2555 8.54843 12.3536 8.64645L13.8536 10.1464C14.0488 10.3417 14.0488 10.6583 13.8536 10.8536L12.3536 12.3536C12.1583 12.5488 11.8417 12.5488 11.6464 12.3536C11.4512 12.1583 11.4512 11.8417 11.6464 11.6464L12.2961 10.9968Z";
    private readonly string _shuffleOff = "M11.647 12.3541L14.1464 14.8536C14.3417 15.0488 14.6583 15.0488 14.8536 14.8536C15.0488 14.6583 15.0488 14.3417 14.8536 14.1464L1.85355 1.14645C1.65829 0.951184 1.34171 0.951184 1.14645 1.14645C0.951184 1.34171 0.951184 1.65829 1.14645 1.85355L4.7145 5.42161C4.06578 5.16073 3.33827 5 2.5 5C2.22386 5 2 5.22386 2 5.5C2 5.77614 2.22386 6 2.5 6C3.95092 6 4.99915 6.56432 5.98421 7.34542C6.09688 7.24605 6.21175 7.14302 6.32947 7.03658L7.03751 7.74461C5.70056 8.95719 4.47234 10 2.5 10C2.22386 10 2 10.2239 2 10.5C2 10.7761 2.22386 11 2.5 11C4.91114 11 6.40583 9.67022 7.74547 8.45258L8.45339 9.16049L8.26272 9.33388C8.64016 9.64117 9.04028 9.93265 9.47627 10.1834L11.6459 12.353C11.6461 12.3532 11.6468 12.3539 11.647 12.3541C11.6468 12.3539 11.6472 12.3543 11.647 12.3541ZM12.1071 9.98576L13.4142 11.2929L13.8536 10.8536C14.0488 10.6583 14.0488 10.3417 13.8536 10.1464L12.3536 8.64645C12.2555 8.54843 12.127 8.49962 11.9985 8.5C11.871 8.50038 11.7437 8.5492 11.6464 8.64645C11.4512 8.84171 11.4512 9.15829 11.6464 9.35355L12.2888 9.99595C12.2275 9.99358 12.167 9.99017 12.1071 9.98576ZM8.55597 6.43464L9.27081 7.14947C10.1345 6.50814 11.0672 6.05134 12.2888 6.00405L11.6464 6.64645C11.4512 6.84171 11.4512 7.15829 11.6464 7.35355C11.8417 7.54881 12.1583 7.54881 12.3536 7.35355L13.8536 5.85355C14.0488 5.65829 14.0488 5.34171 13.8536 5.14645L12.3536 3.64645C12.1583 3.45118 11.8417 3.45118 11.6464 3.64645C11.4512 3.84171 11.4512 4.15829 11.6464 4.35355L12.2961 5.00321C10.7291 5.05276 9.55673 5.67068 8.55597 6.43464Z";
    public string DataShuffleIcon
    {
        get
        {
            if (IsShuffleOn)
            {
                return _shuffleOff;
            }
            else
            {
                return _shuffleOn;
            }
        }
    }

    private readonly string _repeatOn = "M12.8935 5.23788C13.579 5.95588 14 6.92865 14 7.99975C14 10.1419 12.316 11.8908 10.1996 11.9949L10 11.9997L6.707 11.999L7.85525 13.1479C8.02882 13.3215 8.0481 13.5909 7.91311 13.7858L7.85525 13.855C7.68169 14.0286 7.41226 14.0479 7.21739 13.9129L7.14815 13.855L5.14645 11.8533C4.97288 11.6797 4.9536 11.4103 5.08859 11.2154L5.14645 11.1462L7.14815 9.14449C7.34341 8.94923 7.65999 8.94923 7.85525 9.14449C8.02882 9.31806 8.0481 9.58748 7.91311 9.78235L7.85525 9.8516L6.707 10.999L10 10.9997C11.5977 10.9997 12.9037 9.75083 12.9949 8.17602L13 7.99975C13 7.17778 12.6694 6.43303 12.134 5.89117C12.0522 5.80305 12 5.68194 12 5.54865C12 5.2725 12.2239 5.04865 12.5 5.04865C12.6227 5.04865 12.7351 5.09287 12.8221 5.16624L12.8935 5.23788ZM8.78431 2.08664L8.85355 2.14449L10.8553 4.14619L10.9131 4.21544C11.0312 4.38595 11.0312 4.61354 10.9131 4.78405L10.8553 4.8533L8.85355 6.855L8.78431 6.91286C8.6138 7.03098 8.3862 7.03098 8.21569 6.91286L8.14645 6.855L8.08859 6.78575C7.97047 6.61524 7.97047 6.38765 8.08859 6.21714L8.14645 6.14789L9.294 4.99905L6 4.99975C4.40232 4.99975 3.09634 6.24867 3.00509 7.82347L3 7.99975C3 8.8193 3.32863 9.56209 3.8613 10.1035C3.94745 10.1919 4 10.3134 4 10.4472C4 10.7234 3.77614 10.9472 3.5 10.9472C3.36244 10.9472 3.23785 10.8917 3.14745 10.8018C2.4379 10.0823 2 9.09215 2 7.99975C2 5.85755 3.68397 4.10867 5.80036 4.00464L6 3.99975L9.294 3.99905L8.14645 2.8516L8.08859 2.78235C7.9536 2.58748 7.97288 2.31806 8.14645 2.14449C8.32001 1.97093 8.58944 1.95164 8.78431 2.08664Z";
    private readonly string _repeatOff = "M2.78431 2.08834L2.85355 2.14619L13.8536 13.1462C14.0488 13.3415 14.0488 13.658 13.8536 13.8533C13.68 14.0269 13.4106 14.0462 13.2157 13.9112L13.1464 13.8533L11.1305 11.8378C10.8437 11.9221 10.5435 11.9752 10.2339 11.993L10 11.9997L6.707 11.999L7.85525 13.1479C8.02882 13.3215 8.0481 13.5909 7.91311 13.7858L7.85525 13.855C7.68169 14.0286 7.41226 14.0479 7.21739 13.9129L7.14815 13.855L5.14645 11.8533C4.97288 11.6797 4.9536 11.4103 5.08859 11.2154L5.14645 11.1462L7.14815 9.14449C7.34341 8.94923 7.65999 8.94923 7.85525 9.14449C8.02882 9.31806 8.0481 9.58748 7.91311 9.78235L7.85525 9.8516L6.707 10.999L10 10.9997C10.0941 10.9997 10.1871 10.9954 10.279 10.9869L4.62614 5.33211C3.66034 5.83052 3 6.83802 3 7.99975C3 8.8193 3.32863 9.56209 3.8613 10.1035C3.94745 10.1919 4 10.3134 4 10.4472C4 10.7234 3.77614 10.9472 3.5 10.9472C3.36244 10.9472 3.23785 10.8917 3.14745 10.8018C2.4379 10.0823 2 9.09215 2 7.99975C2 6.56381 2.75664 5.30459 3.89297 4.59904L2.14645 2.8533C1.95118 2.65804 1.95118 2.34146 2.14645 2.14619C2.32001 1.97263 2.58944 1.95334 2.78431 2.08834ZM12.5 5.04865C12.6227 5.04865 12.7351 5.09287 12.8221 5.16624L12.8935 5.23788C13.579 5.95588 14 6.92865 14 7.99975C14 9.07744 13.5738 10.0556 12.8808 10.7748L12.174 10.0671C12.6858 9.52898 13 8.80105 13 7.99975C13 7.17778 12.6694 6.43303 12.134 5.89117C12.0522 5.80305 12 5.68194 12 5.54865C12 5.2725 12.2239 5.04865 12.5 5.04865ZM8.14645 2.14449C8.32001 1.97093 8.58944 1.95164 8.78431 2.08664L8.85355 2.14449L10.8553 4.14619L10.9131 4.21544C11.0312 4.38595 11.0312 4.61354 10.9131 4.78405L10.8553 4.8533L8.907 6.80005L8.2 6.09305L9.294 4.99905H7.105L6.105 3.99905H9.294L8.14645 2.8516L8.08859 2.78235C7.9536 2.58748 7.97288 2.31806 8.14645 2.14449Z";
    public string DataRepeatIcon
    {
        get
        {
            if (IsRepeatOn)
            {
                return _repeatOff;
            }
            else
            {
                return _repeatOn;
            }
        }
    }

    //private string _checked = "M13.8639 3.65511C14.0533 3.85606 14.0439 4.17251 13.8429 4.36191L5.91309 11.8358C5.67573 12.0595 5.30311 12.0526 5.07417 11.8203L2.39384 9.09995C2.20003 8.90325 2.20237 8.58667 2.39907 8.39286C2.59578 8.19905 2.91235 8.2014 3.10616 8.3981L5.51192 10.8398L13.1571 3.63419C13.358 3.44479 13.6745 3.45416 13.8639 3.65511Z";
    private readonly string _stayOnTopOn = "M10.0589 2.44535C9.34701 1.73087 8.14697 1.90854 7.67261 2.79864L5.6526 6.58902L2.8419 7.52592C2.6775 7.58072 2.5532 7.71673 2.51339 7.88539C2.47357 8.05404 2.52392 8.23128 2.64646 8.35382L4.79291 10.5003L2.14645 13.1467L2 14.0003L2.85356 13.8538L5.50002 11.2074L7.64646 13.3538C7.76899 13.4764 7.94623 13.5267 8.11489 13.4869C8.28354 13.4471 8.41955 13.3228 8.47435 13.1584L9.41143 10.3472L13.1897 8.32448C14.0759 7.85006 14.2538 6.65535 13.5443 5.9433L10.0589 2.44535ZM8.55511 3.26895C8.71323 2.97225 9.11324 2.91303 9.35055 3.15119L12.836 6.64914C13.0725 6.88648 13.0131 7.28472 12.7178 7.44286L8.76403 9.55946C8.65137 9.61977 8.56608 9.72092 8.52567 9.84215L7.7815 12.0746L3.92562 8.21877L6.15812 7.47461C6.27966 7.43409 6.38101 7.34848 6.44126 7.23542L8.55511 3.26895Z";
    private readonly string _stayOnTopOff = "M9.56016 10.2673L14.1464 14.8536C14.3417 15.0488 14.6583 15.0488 14.8536 14.8536C15.0488 14.6583 15.0488 14.3417 14.8536 14.1464L1.85355 1.14645C1.65829 0.951184 1.34171 0.951184 1.14645 1.14645C0.951184 1.34171 0.951184 1.65829 1.14645 1.85355L5.73223 6.43934L5.6526 6.58876L2.8419 7.52566C2.6775 7.58046 2.5532 7.71648 2.51339 7.88513C2.47357 8.05378 2.52392 8.23102 2.64646 8.35356L4.79291 10.5L2.14645 13.1465L2 14L2.85356 13.8536L5.50002 11.2071L7.64646 13.3536C7.76899 13.4761 7.94623 13.5264 8.11489 13.4866C8.28354 13.4468 8.41955 13.3225 8.47435 13.1581L9.41143 10.3469L9.56016 10.2673ZM8.82138 9.52849L8.76403 9.5592C8.65137 9.61951 8.56608 9.72066 8.52567 9.84189L7.7815 12.0744L3.92562 8.21851L6.15812 7.47435C6.27966 7.43383 6.38101 7.34822 6.44126 7.23516L6.47143 7.17854L8.82138 9.52849ZM12.7178 7.4426L10.6636 8.54227L11.4024 9.28105L13.1897 8.32422C14.0759 7.84981 14.2538 6.65509 13.5443 5.94304L10.0589 2.44509C9.34701 1.73062 8.14697 1.90828 7.67261 2.79838L6.71556 4.59421L7.45476 5.33341L8.55511 3.26869C8.71323 2.97199 9.11324 2.91277 9.35055 3.15093L12.836 6.64888C13.0725 6.88623 13.0131 7.28446 12.7178 7.4426Z";
    public string DataStayOnTopIcon
    {
        get
        {
            if (IsStayOnTop)
            {
                return _stayOnTopOff;
            }
            else
            {
                return _stayOnTopOn;
            }
        }
    }

    private readonly string _checkedBox = "M4.5 2C3.11929 2 2 3.11929 2 4.5V11.5C2 12.8807 3.11929 14 4.5 14H11.5C12.8807 14 14 12.8807 14 11.5V4.5C14 3.11929 12.8807 2 11.5 2H4.5ZM3 4.5C3 3.67157 3.67157 3 4.5 3H11.5C12.3284 3 13 3.67157 13 4.5V11.5C13 12.3284 12.3284 13 11.5 13H4.5C3.67157 13 3 12.3284 3 11.5V4.5ZM10.8536 6.85355C11.0488 6.65829 11.0488 6.34171 10.8536 6.14645C10.6583 5.95118 10.3417 5.95118 10.1464 6.14645L7 9.29289L5.85355 8.14645C5.65829 7.95118 5.34171 7.95118 5.14645 8.14645C4.95118 8.34171 4.95118 8.65829 5.14645 8.85355L6.64645 10.3536C6.84171 10.5488 7.15829 10.5488 7.35355 10.3536L10.8536 6.85355Z";
    private readonly string _uncheckedBox = "M2 4.5C2 3.11929 3.11929 2 4.5 2H11.5C12.8807 2 14 3.11929 14 4.5V11.5C14 12.8807 12.8807 14 11.5 14H4.5C3.11929 14 2 12.8807 2 11.5V4.5ZM4.5 3C3.67157 3 3 3.67157 3 4.5V11.5C3 12.3284 3.67157 13 4.5 13H11.5C12.3284 13 13 12.3284 13 11.5V4.5C13 3.67157 12.3284 3 11.5 3H4.5Z";

    private readonly string _checkedCircle = "M2 8C2 4.68629 4.68629 2 8 2C11.3137 2 14 4.68629 14 8C14 11.3137 11.3137 14 8 14C4.68629 14 2 11.3137 2 8ZM8 1C4.13401 1 1 4.13401 1 8C1 11.866 4.13401 15 8 15C11.866 15 15 11.866 15 8C15 4.13401 11.866 1 8 1ZM10.8536 6.85355C11.0488 6.65829 11.0488 6.34171 10.8536 6.14645C10.6583 5.95118 10.3417 5.95118 10.1464 6.14645L7.25 9.04289L5.85355 7.64645C5.65829 7.45118 5.34171 7.45118 5.14645 7.64645C4.95118 7.84171 4.95118 8.15829 5.14645 8.35355L6.89645 10.1036C7.09171 10.2988 7.40829 10.2988 7.60355 10.1036L10.8536 6.85355Z";
    private readonly string _unCheckedCircle = "M8 14C11.3137 14 14 11.3137 14 8C14 4.68629 11.3137 2 8 2C4.68629 2 2 4.68629 2 8C2 11.3137 4.68629 14 8 14ZM8 13C5.23858 13 3 10.7614 3 8C3 5.23858 5.23858 3 8 3C10.7614 3 13 5.23858 13 8C13 10.7614 10.7614 13 8 13Z";


    public string DataCrossfadeIcon
    {
        get
        {
            if (IsEffectCrossfadeOn)
            {
                return _checkedBox;
            }
            else
            {
                return _uncheckedBox;
            }
        }
    }

    public string DataNoEffectsIcon
    {
        get
        {
            if (IsNoEffectsOn)
            {
                return _checkedCircle; 
            }
            else
            {
                return _unCheckedCircle;
            }
        }
    }

    public string DataPageSlideIcon
    {
        get
        {
            if (IsEffectPageSlideOn)
            {
                return _checkedCircle;
            }
            else
            {
                return _unCheckedCircle;
            }
        }
    }

   public string DataFadeInFadeOutIcon
    {
        get
        {
            if (IsEffectFadeInAndOutOn)
            {
                return _checkedCircle;
            }
            else
            {
                return _unCheckedCircle;
            }
        }
    }

    private readonly string _fullscreenOn = "M11 4C11 4.55228 11.4477 5 12 5H13.5C13.7761 5 14 5.22386 14 5.5C14 5.77614 13.7761 6 13.5 6H12C10.8954 6 10 5.10457 10 4V2.5C10 2.22386 10.2239 2 10.5 2C10.7761 2 11 2.22386 11 2.5V4ZM11 12C11 11.4477 11.4477 11 12 11H13.5C13.7761 11 14 10.7761 14 10.5C14 10.2239 13.7761 10 13.5 10H12C10.8954 10 10 10.8954 10 12V13.5C10 13.7761 10.2239 14 10.5 14C10.7761 14 11 13.7761 11 13.5V12ZM4 11C4.55228 11 5 11.4477 5 12V13.5C5 13.7761 5.22386 14 5.5 14C5.77614 14 6 13.7761 6 13.5V12C6 10.8954 5.10457 10 4 10H2.5C2.22386 10 2 10.2239 2 10.5C2 10.7761 2.22386 11 2.5 11H4ZM5 4C5 4.55228 4.55228 5 4 5H2.5C2.22386 5 2 5.22386 2 5.5C2 5.77614 2.22386 6 2.5 6H4C5.10457 6 6 5.10457 6 4V2.5C6 2.22386 5.77614 2 5.5 2C5.22386 2 5 2.22386 5 2.5V4Z";
    private readonly string _fullscreenOff = "M3.75 3C3.33579 3 3 3.33579 3 3.75V5.5C3 5.77614 2.77614 6 2.5 6C2.22386 6 2 5.77614 2 5.5V3.75C2 2.7835 2.7835 2 3.75 2H5.5C5.77614 2 6 2.22386 6 2.5C6 2.77614 5.77614 3 5.5 3H3.75ZM10 2.5C10 2.22386 10.2239 2 10.5 2H12.25C13.2165 2 14 2.7835 14 3.75V5.5C14 5.77614 13.7761 6 13.5 6C13.2239 6 13 5.77614 13 5.5V3.75C13 3.33579 12.6642 3 12.25 3H10.5C10.2239 3 10 2.77614 10 2.5ZM2.5 10C2.77614 10 3 10.2239 3 10.5V12.25C3 12.6642 3.33579 13 3.75 13H5.5C5.77614 13 6 13.2239 6 13.5C6 13.7761 5.77614 14 5.5 14H3.75C2.7835 14 2 13.2165 2 12.25V10.5C2 10.2239 2.22386 10 2.5 10ZM13.5 10C13.7761 10 14 10.2239 14 10.5V12.25C14 13.2165 13.2165 14 12.25 14H10.5C10.2239 14 10 13.7761 10 13.5C10 13.2239 10.2239 13 10.5 13H12.25C12.6642 13 13 12.6642 13 12.25V10.5C13 10.2239 13.2239 10 13.5 10Z";
    public string DataFullscreenOnIcon
    {
        get
        {
            if (IsFullscreenOn)
            {
                return _fullscreenOn;
            }
            else
            {
                return _fullscreenOff;
            }
        }
    }

    public string DataSystemDpiScalingFactorOnIcon
    {
        get
        {
            if (IsOverrideSystemDpiScalingFactorOn)
            {
                return _checkedBox;
            }
            else
            {
                return _uncheckedBox;
            }
        }
    }
    //

    #endregion

    #region == String Properties ==

    public string SlideshowStartStopString
    {
        get
        {
            if (IsSlideshowOn)
            {
                return ImageViewer.Assets.Resources.Label_SlideshowStop;
            }
            else
            {
                return ImageViewer.Assets.Resources.Label_SlideshowStart;
            }
        }
    }

    #endregion

    public event EventHandler<int>? QueueHasBeenChanged;
    public event EventHandler? TransitionsHasBeenChanged;
    public event EventHandler? SlideshowStatusChanged;
    public event EventHandler? QueueLoaded;
    public event EventHandler<bool>? Fullscreen;
    public event EventHandler? HideFlyout;

    //
    public MainViewModel()
    {
        // Init Timer.
        _timerSlideshow = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(_slideshowTimerInterval)
        };
        _timerSlideshow.Tick += OnSlideshowTimerTick;

#if DEBUG
        IsSaveLog = true;

#else
        IsSaveLog = false;
#endif
    }

    #region == Public Methods ==

    public async void DroppedFiles(List<ImageInfo> images, string singleSelectedOriginalFile)
    {
        //Debug.WriteLine("DroppedFiles()");

        if ((images is null) || (images.Count < 1))
        {
            IsWorking = false;
            return;
        }

        /*
        Dispatcher.UIThread.Post(async () =>
        {

        }, DispatcherPriority.Loaded);
        */

        IsWorking = true;

        if (_timerSlideshow.IsEnabled)
        {
            _timerSlideshow.Stop();
        }

        // rest 
        _queueIndex = 0;

        // Let's keep the prev image for the sake of fade out effects.
        //DiplayImage1 = null;
        // Just because.
        SelectedQueueImage = null;

        _queue.Clear();
        _queue = new ObservableCollection<ImageInfo>(images);
        _originalQueue = images;//[.. images];

        // When the same files dropped. The same file won't reload due to "dup". So clear it.
        _currentFile = string.Empty;

        if (_queue.Count > 0)
        {
            // Hide welcome screen
            QueueLoaded?.Invoke(this, EventArgs.Empty);

            // Hide if count = 1
            IsQueueListBoxVisible = _queue.Count != 1;

            if (_isShuffleOn)
            {
                //Debug.WriteLine("Shuffle @DroppedFiles()");

                _queue.Shuffle();
            }

            if (!string.IsNullOrEmpty(singleSelectedOriginalFile))
            {
                //Debug.WriteLine("Getting SingleSelectedOriginalFile @DroppedFiles()");

                var item = _queue.First(x => x.ImageFilePath == singleSelectedOriginalFile);
                if (item is not null)
                {
                    _queueIndex = _queue.IndexOf(item);
                }
            }

            // Test -> yap. IsWorking causes await delay 300 mil sec in Show(),
            IsWorking = false;

            IsTransitionReversed = false;

            //Debug.WriteLine("Calling Show() @DroppedFiles()");

            // Show Image.
            await Show();

            // Wait untill the Image drawn before loading ListBox which starts loading images on its own.
            await Task.Delay(500);

            IsWorking = true;
            await Task.Yield();

            //Debug.WriteLine("Updating Queue @DroppedFiles()");

            // Display images in the ListBox.
            OnPropertyChanged(nameof(Queue));

            // Need this for trigger UpdateVisibleItems because if only a few images are loaded,
            // scrollviewer does not fire changed events.
            if (_queueIndex > 0)
            {   
                // Shuffled
                QueueHasBeenChanged?.Invoke(this, _queueIndex - 1);
            }
            else
            {
                QueueHasBeenChanged?.Invoke(this, 0);
            }

            IsWorking = false;
            await Task.Yield();
        }
        else
        {
            IsWorking = false;
        }
    }

    public void SpaceKeyPressed()
    {
        ToggleSlideshow();
        /*
        if (_timerSlideshow.IsEnabled)
        {
            _timerSlideshow.Stop();
        }
        else
        {
            if (_queue.Count > 0)
            {
                await Show(_crossfadeWaitDuration);
            }
        }
        */
    }

    public async void NextKeyPressed()
    {
        if (_timerSlideshow.IsEnabled)
        {
            _timerSlideshow.Stop();
        }

        if (_queue.Count <= 0) return;

        //_queueIndex++;
        if ((_queueIndex) > (_queue.Count - 1)) 
        {
            if (IsRepeatOn)
            {
                // Reset index.
                _queueIndex = 0;
            }
            else
            {
                if (IsSlideshowOn)
                {
                    // No more to show.
                    IsSlideshowOn = false;
                }

                return;
            }
        }

        IsTransitionReversed = false;

        await Show();
    }

    public async void PrevKeyPressed()
    {
        if (_queue.Count <= 0) return;

        if (_queueIndex == 1)
        {
            //Debug.WriteLine("(_queueIndex == 0) PrevKeyPressed");
            return;
        }

        if ((_queueIndex - 2) <= -1)
        {
            return;
        }

        if (_timerSlideshow.IsEnabled)
        {
            _timerSlideshow.Stop();
        }

        _queueIndex -= 2;

        IsTransitionReversed = true;

        await Show();
    }

    public async void ListBoxItemSelected(ImageInfo img)
    {
        if (img is null)
        {
            return;
        }

        IsTransitionReversed = false;
        _queueIndex = Queue.IndexOf(img);
        await Show();
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

    public void CleanUp()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    #endregion

    #region == Private Methods ==

    private async Task GetPictures(IEnumerable<object>? imageInfoItems)
    {
        if (Queue.Count <= 0)
        {
            return;
        }

        if (imageInfoItems is null)
        {
            Debug.WriteLine("imageInfoItems is null @GetPictures");
            return;
        }

        if (Queue.Count <= 0)
        {
            Debug.WriteLine("Queue.Count <= 0 @GetPictures");
            return;
        }

        foreach (var item in imageInfoItems)
        {
            await Task.Delay(10);

            if (_cts.IsCancellationRequested)
            {
                Debug.WriteLine($"@GetPictures() in foreach IsCancellationRequested");
                return;
            }

            if (item is not ImageInfo img)
            {
                Debug.WriteLine("item is not ImageInfo @GetPictures");
                continue;
            }

            if (img is null)
            {
                Debug.WriteLine("img is null @GetPictures");
                continue;
            }

            if (string.IsNullOrEmpty(img.ImageFilePath))
            {
                continue;
            }

            if (img.IsAcquired)
            {
                //Debug.WriteLine("img.IsAcquired");
                continue;
            }

            if (img.IsLoading)
            {
                //Debug.WriteLine("img.IsLoading @GetPictures");
                continue;
            }
            /*
            if (File.Exists(img.ImageFilePath))
            {

            }
            */
            img.IsLoading = true;

            //Debug.WriteLine($"@GetPictures IsLoading: {img.ImageFilePath}");

            try
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    Bitmap? bitmap = await Task.Run(() =>
                    {
                        if (_cts.IsCancellationRequested)
                        {
                            Debug.WriteLine($"@GetPictures() before new Bitmap IsCancellationRequested");
                            return null;
                        }

                        try
                        {
                            return new Bitmap(img.ImageFilePath);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Exception: Failed to load image @GetPictures on new Bitmap {img.ImageFilePath} - {ex}");
                        }
                        return null;
                    }, _cts.Token);
                    //Bitmap? bitmap = new(img.ImageFilePath);

                    if (bitmap is null)
                    {
                        // TODO
                        Dispatcher.UIThread.Post(() =>
                        {
                            img.IsAcquired = false;
                            img.IsLoading = false;
                            img.ImageSource = null;
                        });
                        return;
                    }

                    img.ImageSource = bitmap;

                    //img.ImageSource = img.ImageSource.CreateScaledBitmap(new PixelSize((int)(img.ImageSource.PixelSize.Width / 1.5), (int)(img.ImageSource.PixelSize.Height / 1.5)));
                    if (IsOverrideSystemDpiScalingFactorOn && (SystemDpiScalingFactor > 1))
                    {
                        img.ImageWidth = (img.ImageSource.PixelSize.Width / SystemDpiScalingFactor);
                        img.ImageHeight = (img.ImageSource.PixelSize.Height / SystemDpiScalingFactor);
                    }
                    else
                    {
                        img.ImageWidth = (img.ImageSource.PixelSize.Width);
                        img.ImageHeight = (img.ImageSource.PixelSize.Height);
                    }

                    img.IsAcquired = true;
                    img.IsLoading = false;
                }, DispatcherPriority.Loaded);//Default//.Background

            }
            catch (Exception e)
            {
                // TODO:
                Dispatcher.UIThread.Post(() =>
                {
                    img.IsAcquired = false;
                    img.IsLoading = false;
                    img.ImageSource = null;
                });

                Debug.WriteLine("GetPictures: Exception while loading: " + img.ImageFilePath + Environment.NewLine + e.Message);

                continue;
            }
            finally
            {
                img.IsLoading = false;
            }

            await Task.Delay(20);
        }

        return;
    }

    private async void OnSlideshowTimerTick(object? sender, EventArgs e)
    {
        // This code runs on the UI thread, so it's safe to update UI elements.
        if (_queue.Count <= 0) return;
        if (_queueIndex > (_queue.Count - 1))
        {
            if (IsRepeatOn)
            {
                // Reset index.
                _queueIndex = 0;
            }
            else
            {
                if (IsSlideshowOn)
                {
                    // No more to show.
                    IsSlideshowOn = false;
                }

                return;
            }
        }

        IsTransitionReversed = false;

        await Show();
    }

    private async Task Show()
    {
        if (_queue.Count <= 0) return;
        if (_queueIndex < 0) return;
        if (_queueIndex > (_queue.Count - 1)) 
        {
            if (IsRepeatOn)
            {
                // Reset index.
                _queueIndex = 0;
            }
            else
            {
                if (IsSlideshowOn)
                {
                    // No more to show.
                    IsSlideshowOn = false;
                }

                return;
            }
        }

        if (_timerSlideshow.IsEnabled)
        {
            _timerSlideshow.Stop();
        }

        if (IsWorking)
        {
            await Task.Delay(300);
        }

        if (_cts.IsCancellationRequested)
        {
            Debug.WriteLine($"@Show() IsCancellationRequested");
            return;
        }

        var img = _queue[_queueIndex];

        if (string.IsNullOrEmpty(img.ImageFilePath))
        {
            return;
        }

        // just in case, double check.
        if (HasImageExtension(img.ImageFilePath, _validExtensions) == false)
        {
            _queueIndex++;
            await Show();
            return;
        }

        /*

        if (await ShowImage(img))
        {
            if (IsSlideshowOn)
            {
                _timerSlideshow.Start();
            }

            QueueHasBeenChanged?.Invoke(this, _queueIndex - 1);
        }

        */
        
        // Little hackkish... but it seems to work great without locking the UI when HDD (not SSD) is slowly waking up from sleep for example.
        await Task.Run(async () =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                IsWorking = true;
            });

            if (await ShowImage(img))
            {
                if (_cts.IsCancellationRequested)
                {
                    Debug.WriteLine($"@Show() after ShowImage() IsCancellationRequested");
                    return;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    IsWorking = false;

                    if (IsSlideshowOn)
                    {
                        _timerSlideshow.Start();
                    }

                    //_queueIndex = index;

                    QueueHasBeenChanged?.Invoke(this, _queueIndex - 1);
                });
            }
        }, _cts.Token);
    }

    private Task<bool> ShowImage(ImageInfo img)
    {
        if (_cts.IsCancellationRequested)
        {
            Debug.WriteLine($"@ShowImage IsCancellationRequested");
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(img.ImageFilePath))
        {
            return Task.FromResult(false);
        }

        if (!string.IsNullOrEmpty(_currentFile))
        {
            if (img.ImageFilePath.Equals(_currentFile))
            {
                Debug.WriteLine($"{_queueIndex} dupe skipping [{_currentFile}]");
                _queueIndex++;
                return Task.FromResult(true);
            }
        }

        int idx = _queueIndex;

        _currentFile = img.ImageFilePath;

        // Test not good.
        //_isWorking = true;

        //Debug.WriteLine($"{idx} Enter critical section.");
        
        // no longer needed
        //Bitmap? bitmap;
        if (img.IsAcquired)
        {
            // no longer needed
            //bitmap = img.ImageSource;
        }
        else if (img.IsLoading)
        {
            Debug.WriteLine($"@ShowImage IsLoading: {img.ImageFilePath}");

            // no longer needed
            //bitmap = img.ImageSource;
        }
        else
        {
            img.IsLoading = true;

            //Debug.WriteLine($"{idx} {Path.GetFileName(_currentFile)}");

            try
            {
                img.ImageSource = new(img.ImageFilePath);

                if (IsOverrideSystemDpiScalingFactorOn && (SystemDpiScalingFactor > 1))
                {
                    img.ImageWidth = (img.ImageSource.PixelSize.Width / SystemDpiScalingFactor);
                    img.ImageHeight = (img.ImageSource.PixelSize.Height / SystemDpiScalingFactor);
                }
                else
                {
                    img.ImageWidth = (img.ImageSource.PixelSize.Width);
                    img.ImageHeight = (img.ImageSource.PixelSize.Height);
                }

                img.IsAcquired = true;
                img.IsLoading = false;
            }
            catch (Exception ex)
            {
                // TODO: 

                Debug.WriteLine($"{ex} @ShowImage");

                img.IsAcquired = false;
                img.IsLoading = false;

                // no longer needed
                //bitmap = null;
            }
            finally
            {
                img.IsLoading = false;
            }

        }

        //DiplayImage1 = bitmap;
        // Testing
        DiplayImage = img;

        //_queueIndex++;
        _queueIndex = idx + 1;
        //Debug.WriteLine($"{idx} Exit critical section.");

        #region == Unload some of the images from memory ==

        if (_queueIndex > 20)
        {
            int i = _queueIndex - 15;
            if (_queue[i].ImageSource is not null)
            {
                _queue[i].ImageSource?.Dispose();
                _queue[i].ImageSource = null;
                _queue[i].IsAcquired = false;
                _queue[i].IsLoading = false;
            }
            /*
            for (int i = 0; i < (_queueIndex - 10); i++)
            {
                if (_queue[i].ImageSource is not null)
                {
                    _queue[i].ImageSource?.Dispose();
                    _queue[i].ImageSource = null;
                    _queue[i].IsAcquired = false;
                    _queue[i].IsLoading = false;
                }
            }
            */
        }

        var c = _queue.Count;
        if (_queueIndex + 20 < c)
        {
            int i = _queueIndex + 15;
            if (_queue[i].ImageSource is not null)
            {
                _queue[i].ImageSource?.Dispose();
                _queue[i].ImageSource = null;
                _queue[i].IsAcquired = false;
                _queue[i].IsLoading = false;
            }
            /*
            for (int i = (c-1); i > (_queueIndex + 10); i--)
            {
                if (_queue[i].ImageSource is not null)
                {
                    _queue[i].ImageSource?.Dispose();
                    _queue[i].ImageSource = null;
                    _queue[i].IsAcquired = false;
                    _queue[i].IsLoading = false;
                }
            }
            */
        }

        #endregion

        if (IsSlideshowOn)
        {
            _timerSlideshow.Start();
        }

        return Task.FromResult(true);
    }

    #endregion

    #region == Commands ==

    [RelayCommand]
    public void ToggleStayOnTop()
    {
        var win = App.GetService<MainWindow>();

        if (IsStayOnTop)
        {
            win.Topmost = false;
        }
        else
        {
            win.Topmost = true;
        }

        IsStayOnTop = !IsStayOnTop;
    }

    [RelayCommand(CanExecute = nameof(CanToggleCrossfade))]
    public void ToggleCrossfade()
    {
        IsEffectCrossfadeOn = !IsEffectCrossfadeOn;
    }

    private bool CanToggleCrossfade()
    {
        return !IsNoEffectsOn;
    }

    [RelayCommand]
    public void ToggleFadeInAndOut()
    {
        IsEffectFadeInAndOutOn = true;//!IsEffectFadeInAndOutOn;
        IsEffectPageSlideOn = !IsEffectFadeInAndOutOn;
        IsNoEffectsOn = !IsEffectFadeInAndOutOn;
    }

    [RelayCommand]
    public void TogglePageSlide()
    {
        IsEffectPageSlideOn = true;//!IsEffectPageSlideOn;
        IsEffectFadeInAndOutOn = !IsEffectPageSlideOn;
        IsNoEffectsOn = !IsEffectPageSlideOn;
    }

    [RelayCommand]
    public void ToggleNoEffects()
    {
        IsNoEffectsOn = true;// !IsNoEffectsOn;
        IsEffectFadeInAndOutOn = !IsNoEffectsOn;
        IsEffectPageSlideOn = !IsNoEffectsOn;
    }

    [RelayCommand(CanExecute = nameof(CanToggleSlideshow))]
    public void ToggleSlideshow()
    {
        var isOn = IsSlideshowOn;

        var page = App.GetService<MainView>();
        page.ToggleSlideshowAnimation(isOn, TimeSpan.FromMilliseconds(600));

        if (IsSlideshowOn)
        {
            //Debug.WriteLine("StartSlideshow false");
            IsSlideshowOn = false;

            if (_timerSlideshow.IsEnabled)
            {
                _timerSlideshow.Stop();
            }
        }
        else
        {
            if (_timerSlideshow.IsEnabled)
            {
                _timerSlideshow.Stop();
            }

            if (_queue.Count > 0)
            {
                //Debug.WriteLine("StartSlideshow true");
                IsSlideshowOn = true;

                // If at the end, reset index.
                if (_queueIndex >= (_queue.Count - 1))
                {
                    _queueIndex = 0;
                }

                _timerSlideshow.Start();
                //_ = Show(_crossfadeWaitDuration);
            }
        }

        OnPropertyChanged(nameof(IsSlideshowOn));
    }
    private bool CanToggleSlideshow()
    {
        return _queue.Count > 1;
    }

    [RelayCommand]
    public void ToggleShuffle()
    {
        IsShuffleOn = !IsShuffleOn;
    }

    [RelayCommand]
    public void ToggleRepeat()
    {
        IsRepeatOn = !IsRepeatOn;
    }

    [RelayCommand]
    public void ToggleFullscreen()
    {
        IsFullscreenOn = !IsFullscreenOn;
    }

    [RelayCommand]
    public void ToggleSystemDpiScalingFactor()
    {
        IsOverrideSystemDpiScalingFactorOn = !IsOverrideSystemDpiScalingFactorOn;
    }

    [RelayCommand(CanExecute = nameof(CanShowInExplorer))]
    public async Task ShowInExplorer()
    {
        if (string.IsNullOrEmpty(_currentFile))
        {
            return;
        }

        if (!System.IO.File.Exists(_currentFile))
        {
            return;
        }

        var dir = Path.GetDirectoryName(_currentFile);
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        var mainWin = App.GetService<MainWindow>();
        var launcher = TopLevel.GetTopLevel(mainWin)?.Launcher;
        if (launcher is null)
        {
            return;
        }

        // Open in default app.
        //await launcher.LaunchFileInfoAsync(new FileInfo(_currentFile));
        
        // Open in explorer/file manager.
        if (await launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(dir)))
        {
            // ok
        }
        else
        {
            // TODO: show error message.

            // This failes in Debug session when the app is attached VSCode from Snap packages on Linux. 
        }

        return;

        /*
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Test A
            Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });

            // Test B
            string argument = $"/select, \"{_currentFile}\"";
            Process.Start("explorer.exe", argument);

        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{dir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        */

        /*
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string argument = $"/select,\"{_currentFile}\"";
            Process.Start("explorer.exe", argument);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{_currentFile}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var dir = Path.GetDirectoryName(_currentFile);
            if (dir is not null)
            {
                Process.Start("xdg-open", dir);
            }
        }
        */
    }
    private bool CanShowInExplorer()
    {
        return _queue.Count > 0;
    }

    [RelayCommand]
    public void QueueListviewEnterKey(ImageInfo img)
    {
        Debug.WriteLine("QueueListviewEnterKey");

        if (img is null)
        {
            Debug.WriteLine("QueueListviewEnterKey img is null");

            return;
        }

        ListBoxItemSelected(img);
    }

    [RelayCommand]
    public static void Quit()
    {
        var mainWin = App.GetService<MainWindow>();
        mainWin.Close();
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