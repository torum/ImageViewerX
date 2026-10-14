using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ImageViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageViewer.Views;

internal sealed partial class MainView : UserControl
{
    readonly MainViewModel _viewModel;

#pragma warning disable CS8618
    // Optional parameterless constructor for XAML Previewer
    public MainView() { InitializeComponent(); }
#pragma warning restore CS8618
    public MainView(MainViewModel vm)
    {
        //_viewModel = App.GetService<MainViewModel>();
        _viewModel = vm;

        InitializeComponent();

        this.Loaded += (s, e) =>
        {
            // Just in case the event was already subscribed, unsubscribe first to avoid multiple subscriptions.
            _viewModel?.TransitionsHasBeenChanged -= OnTransitionsHasBeenChanged;

            _viewModel?.TransitionsHasBeenChanged += OnTransitionsHasBeenChanged;

            UpdatePageTransition();
        };

        this.Unloaded += (s, e) =>
        {
            _viewModel?.TransitionsHasBeenChanged -= OnTransitionsHasBeenChanged;
        };
    }

    private void OnTransitionsHasBeenChanged(object? sender, EventArgs e)
    {
        UpdatePageTransition();
    }

    private void UpdatePageTransition()
    {
        if (_viewModel.IsNoEffectsOn)
        {
            // This does not work. Every other image does not show...
            //this.ImageTransitioningContentControl.PageTransition = null;

            var compositeTransition = new CompositePageTransition();
            compositeTransition.PageTransitions.Add(new CustomNoTransition(TimeSpan.FromMilliseconds(0)));
            this.ImageTransitioningContentControl.PageTransition = compositeTransition;

            return;
        }

        if (_viewModel.IsEffectFadeInAndOutOn)
        {
            var compositeTransition = new CompositePageTransition();
            compositeTransition.PageTransitions.Add(new CustomFadeTransitionOnComposition(TimeSpan.FromMilliseconds(1000), _viewModel.IsEffectCrossfadeOn));
            //compositeTransition.PageTransitions.Add(new CustomCompositorFadeTransition(TimeSpan.FromMilliseconds(1000), _viewModel.IsEffectCrossfadeOn));
            this.ImageTransitioningContentControl.PageTransition = compositeTransition;
        }
        else if (_viewModel.IsEffectPageSlideOn)
        {
            var compositeTransition = new CompositePageTransition();

            if (_viewModel.IsEffectCrossfadeOn)
            {
                var crossFade = new CrossFade(TimeSpan.FromSeconds(0.5));
                compositeTransition.PageTransitions.Add(crossFade);
            }

            //var pageSlide = new PageSlide(TimeSpan.FromSeconds(0.5), PageSlide.SlideAxis.Vertical) //Horizontal
            var pageSlide = new CustomPageSlideTransition(TimeSpan.FromSeconds(0.7), CustomPageSlideTransition.SlideAxis.Vertical)
            {
                SlideInEasing = new CubicEaseIn(),
                SlideOutEasing = new BounceEaseOut()

                //SlideInEasing = new CubicEaseOut(),
                //SlideOutEasing = new CubicEaseIn()

                //SlideInEasing = new QuadraticEaseIn(),
                //SlideOutEasing = new QuadraticEaseOut()

                //SlideInEasing = new ElasticEaseIn(),
                //SlideOutEasing = new ElasticEaseOut()

                //SlideInEasing = new BounceEaseIn(),
                //SlideOutEasing = new BounceEaseOut()

                //SlideInEasing = new SineEaseIn(),
                //SlideOutEasing = new SineEaseOut()

                //SlideInEasing = new LinearEasing(),
                //SlideOutEasing = new LinearEasing()
            };

            compositeTransition.PageTransitions.Add(pageSlide);

            this.ImageTransitioningContentControl.PageTransition = compositeTransition;
        }
        else
        {
            Debug.WriteLine("This shouldn't be happening @UpdatePageTransition().");
            // shouldn't be happening.
            var compositeTransition = new CompositePageTransition();
            compositeTransition.PageTransitions.Add(new CustomFadeTransition(TimeSpan.FromMilliseconds(1000), _viewModel.IsEffectCrossfadeOn));
            this.ImageTransitioningContentControl.PageTransition = compositeTransition;
        }
    }

    public void ToggleSlideshowAnimation(bool isOn, TimeSpan duration)
    {
        var image = this.ImageTransitioningContentControl;//this.ImageGrid;

        if (isOn)
        {
            image.StartArk1Animation(true);
        }
        else
        {
            image.StartArk2Animation(duration);
        }
    }
}

public static partial class FadeInFadeOut
{
    public static void StartArk1Animation(this Visual control, bool isFromTopToBottom)
    {
        int direction;
        if (isFromTopToBottom)
        {
            direction = 10;
        }
        else
        {
            direction = -10;
        }

        CompositionVisual? compositionVisual = ElementComposition.GetElementVisual(control);
        Compositor compositor = compositionVisual!.Compositor;

        // "Offset" is a Vector3 property, so we create a Vector3KeyFrameAnimation
        Vector3KeyFrameAnimation animation = compositor.CreateVector3KeyFrameAnimation();
        // Change the offset of the visual slightly to the top when the animation beginning

        //animation.InsertKeyFrame(0f, compositionVisual.Offset with { X = compositionVisual.Offset.X - 20 });

        // Get the new animated value by creating a copy of the existing Offset with a modified X property
        Avalonia.Vector3D newOffset = compositionVisual.Offset with { Y = compositionVisual.Offset.Y + direction };

        // Vector3KeyFrameAnimation は System.Numerics.Vector3 を期待する
        // したがって、Avalonia.Vector3D を System.Numerics.Vector3 に明示的に変換する
        System.Numerics.Vector3 convertedOffset1 = new(
            (float)newOffset.X,
            (float)newOffset.Y,
            (float)newOffset.Z
        );
        // 新しい値をキーフレームに挿入する
        animation.InsertKeyFrame(0f, convertedOffset1);



        // Revert the offset to the original position (0,0,0) when the animation ends
        //animation.InsertKeyFrame(1f, compositionVisual.Offset);

        Avalonia.Vector3D originalOffset = compositionVisual.Offset;

        // 2. Explicitly convert it to a System.Numerics.Vector3
        //    Note the explicit (float) cast for each component.
        System.Numerics.Vector3 convertedOffset2 = new(
            (float)originalOffset.X,
            (float)originalOffset.Y,
            (float)originalOffset.Z
        );

        // 3. Insert the converted value into the keyframe
        animation.InsertKeyFrame(1f, convertedOffset2);


        animation.Duration = TimeSpan.FromMilliseconds(200);
        // Start the new animation!
        compositionVisual.StartAnimation("Offset", animation);

    }

    public static void StartArk2Animation(this Visual control, TimeSpan duration)
    {
        if (duration == TimeSpan.Zero) return;

        var compositionVisual = ElementComposition.GetElementVisual(control);

        if (compositionVisual is null) return;

        var animation = compositionVisual.Compositor.CreateScalarKeyFrameAnimation();

        //animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(0.3f, 0.3f);
        animation.InsertKeyFrame(1f, 1f);
        animation.Duration = duration;
        animation.Target = nameof(CompositionVisual.Opacity);

        compositionVisual.StartAnimation(nameof(CompositionVisual.Opacity), animation);
    }

    public static void StartFadeInAnimation(this Visual control, TimeSpan duration, float finalOpacityValue)
    {
        if (duration == TimeSpan.Zero) return;

        var compositionVisual = ElementComposition.GetElementVisual(control);

        if (compositionVisual is null) return;

        var animation = compositionVisual.Compositor.CreateScalarKeyFrameAnimation();

        animation.InsertKeyFrame(0f, 0f);
        //animation.InsertKeyFrame(0.5f, 0.5f);
        //animation.InsertKeyFrame(1f, 1f);
        animation.InsertKeyFrame(1f, finalOpacityValue);
        animation.Duration = duration;
        animation.Target = nameof(CompositionVisual.Opacity);

        compositionVisual.StartAnimation(nameof(CompositionVisual.Opacity), animation);
    }

    public static void StartFadeOutAnimation(this Visual control, TimeSpan duration)
    {
        if (duration == TimeSpan.Zero) return;

        var compositionVisual = ElementComposition.GetElementVisual(control);

        if (compositionVisual is null) return;

        var animation = compositionVisual.Compositor.CreateScalarKeyFrameAnimation();

        animation.InsertKeyFrame(0f, 1f);
        //animation.InsertKeyFrame(0.5f, 0.5f);
        animation.InsertKeyFrame(1f, 0f);
        animation.Duration = duration;
        animation.Target = nameof(CompositionVisual.Opacity);

        compositionVisual.StartAnimation(nameof(CompositionVisual.Opacity), animation);
    }
}

public class CustomFadeTransition(TimeSpan duration, bool crossFade) : IPageTransition
{
    private readonly TimeSpan _duration = duration;
    private readonly bool _crossFade = crossFade;
    private bool _first = true;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        var parent = from?.GetVisualParent() ?? to?.GetVisualParent();
        if (parent == null) return;

        var fromAnimTask = Task.CompletedTask;
        var toAnimTask = Task.CompletedTask;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Animate the "from" page to fade out
            if (from != null)
            {
                var fromAnimation = new Avalonia.Animation.Animation
                {
                    //Easing = new CubicEaseIn(), // Starts slowly, then rapidly accelerates towards the end.
                    //Easing = new CubicEaseOut(), // Starts very fast, then gently decelerates to a smooth stop at the end.
                    //Easing = new CubicEaseInOut(), // Starts slowly, speeds up in the middle, and slows down at the end.
                    Duration = _duration,
                    FillMode = FillMode.Forward,
                    Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
                    }
                }
                };

                if (_crossFade)
                {
                    fromAnimation.Easing = new CubicEaseInOut();
                }
                else
                {
                    fromAnimation.Easing = new CubicEaseIn();
                }

                fromAnimTask = fromAnimation.RunAsync(from, cancellationToken);
            }

            // Animate the "to" page to fade in
            if (to != null)
            {
                to.Opacity = 0.0;

                // (!_crossFade) Do not await if _crossFade is true.
                // (!_first) Do not await for the first time.
                if ((!_crossFade) && (!_first))
                {
                    //await Task.Delay(_duration, cancellationToken);

                    // Safely track completion using a Task.Delay matching the duration
                    var tcs = new TaskCompletionSource<bool>();
                    //await Task.Delay(_duration, cancellationToken).ContinueWith(_ => tcs.TrySetResult(true));
                    await Task.Delay(_duration, cancellationToken)
                            .ContinueWith(_ => tcs.TrySetResult(true), cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Current);
                }

                var toAnimation = new Avalonia.Animation.Animation
                {
                    //Easing = new CubicEaseInOut(),
                    //Easing = new CubicEaseOut(),
                    //Easing = new CubicEaseIn(),
                    Easing = new CubicEaseInOut(),
                    Duration = _duration,
                    FillMode = FillMode.Forward,
                    Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
                    }
                }
                };

                toAnimTask = toAnimation.RunAsync(to, cancellationToken);
            }

            _first = false;

            //await toAnimTask;
            //await fromAnimTask;
            await Task.WhenAll(fromAnimTask, toAnimTask);
        }
        catch (OperationCanceledException)
        {
            // Re-throw so Avalonia knows this transition cycle was aborted
            throw;
        }
        finally
        {
            // Reset elements back to standard UI thread states if required by your layout container
            if (from != null) from.Opacity = 0.0;
            if (to != null) to.Opacity = 1.0;


            _first = false;

        }

    }
}


public class CustomFadeTransitionOnComposition(TimeSpan duration, bool crossFade) : IPageTransition
{
    private readonly TimeSpan _duration = duration;
    private readonly bool _crossFade = crossFade;
    private bool _first = true;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        var parent = from?.GetVisualParent() ?? to?.GetVisualParent();
        if (parent == null) return;

        var fromAnimTask = Task.CompletedTask;
        var toAnimTask = Task.CompletedTask;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Fade out the "from" page on the compositor
            if (from != null)
            {
                Easing fromEasing = _crossFade ? new CubicEaseInOut() : new CubicEaseIn();
                fromAnimTask = RunOpacityAnimation(from, 1.0f, 0.0f, fromEasing, cancellationToken);
            }

            // Fade in the "to" page
            if (to != null)
            {
                to.Opacity = 0.0;

                // Do not wait when cross-fading, nor on the first run.
                if ((!_crossFade) && (!_first))
                {
                    await Task.Delay(_duration, cancellationToken);
                }

                toAnimTask = RunOpacityAnimation(to, 0.0f, 1.0f, new CubicEaseInOut(), cancellationToken);
            }

            _first = false;

            await Task.WhenAll(fromAnimTask, toAnimTask);
        }
        catch (OperationCanceledException)
        {
            // Re-throw so Avalonia knows this transition cycle was aborted
            throw;
        }
        finally
        {
            if (from != null) from.Opacity = 0.0;
            if (to != null) to.Opacity = 1.0;

            _first = false;
        }
    }

    private Task RunOpacityAnimation(Visual visual, float start, float end, Easing easing, CancellationToken ct)
    {
        var compositionVisual = ElementComposition.GetElementVisual(visual);
        if (compositionVisual == null)
            return Task.CompletedTask;

        var compositor = compositionVisual.Compositor;

        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.Target = "Opacity";
        animation.Duration = _duration;
        animation.InsertKeyFrame(0.0f, start, new LinearEasing());
        // The easing of a key frame applies to the segment ending at that frame.
        animation.InsertKeyFrame(1.0f, end, easing);
        animation.IterationCount = 1;
        // Equivalent of FillMode.Forward: the final value is held after completion.
        animation.StopBehavior = AnimationStopBehavior.LeaveCurrentValue;

        var tcs = new TaskCompletionSource();

        // Start from the initial value, then animate on the compositor thread.
        compositionVisual.Opacity = start;
        compositionVisual.StartAnimation("Opacity", animation);

        var registration = ct.Register(() =>
        {
            compositionVisual.StopAnimation("Opacity");
            tcs.TrySetCanceled(ct);
        });

        // No public completion callback exists, so complete after the animation duration.
        _ = Task.Delay(_duration, CancellationToken.None).ContinueWith(_ =>
        {
            registration.Dispose();
            tcs.TrySetResult();
        }, TaskScheduler.Default);

        return tcs.Task;
    }
}

// Failed attempt to use Compositor for fade transition.
// The fade in and fade out animations work, but the coordination between manual page transition and the Compositor animations is not working as expected.
// The "to" page does not appear correctly after the transition.
// Besides, the performance of the Compositor animations is not significantly better than the Avalonia.Animation animations for this use case.
// Therefore, I reverted to using the previous CustomFadeTransition implementation above.
public class CustomFadeTransition2(TimeSpan duration, bool crossFade) : IPageTransition
{
    private readonly TimeSpan _duration = duration;
    private readonly bool _crossFade = crossFade;
    private bool _first = true;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        var parent = from?.GetVisualParent() ?? to?.GetVisualParent();
        if (parent == null) return;

        // This ensures the layout engine knows exactly how to render them initially,
        // preventing a 1-frame visual layout spike.
        if (to != null)
        {
            to.Opacity = 0.0;
        }
        
        if (!_first)
        {
            // This allows Avalonia's layout system to process the page swap, attach the controls
            // to the visual tree, and initialize properties before the animation handoff.
            await Task.Delay(16, cancellationToken); // ~1 frame delay at 60Hz
        }

        var fromAnimTask = Task.CompletedTask;
        var toAnimTask = Task.CompletedTask;

        // Animate the "from" page to fade out
        if (from != null)
        {
            var fromVisual = ElementComposition.GetElementVisual(from);
            if (fromVisual != null)
            {
                var compositor = fromVisual.Compositor;
                var animation = compositor.CreateScalarKeyFrameAnimation();
                animation.Target = "Opacity";
                animation.Duration = _duration;

                // Keyframes use float values for Opacity in the composition engine
                animation.InsertKeyFrame(0.0f, 1.0f);
                animation.InsertKeyFrame(1.0f, 0.0f, new CubicEaseIn());

                // Start the render-thread animation
                fromVisual.StartAnimation("Opacity", animation);

                // Safely track completion using a Task.Delay matching the duration
                var tcs = new TaskCompletionSource<bool>();
                _ = Task.Delay(_duration, cancellationToken).ContinueWith(_ => tcs.TrySetResult(true));
                fromAnimTask = tcs.Task;
            }
        }

        // Animate the "to" page to fade in
        if (to != null)
        {
            var toVisual = ElementComposition.GetElementVisual(to);
            if (toVisual != null)
            {
                // Explicitly set starting composition opacity instead of UI Thread Opacity
                toVisual.Opacity = 0.0f;

                if ((!_crossFade) && (!_first))
                {
                    await Task.Delay(_duration, cancellationToken);
                }

                var compositor = toVisual.Compositor;
                var animation = compositor.CreateScalarKeyFrameAnimation();
                animation.Target = "Opacity";
                animation.Duration = _duration;

                animation.InsertKeyFrame(0.0f, 0.0f);
                animation.InsertKeyFrame(1.0f, 1.0f, new CubicEaseOut());

                // Start the render-thread animation
                toVisual.StartAnimation("Opacity", animation);

                var tcs = new TaskCompletionSource<bool>();
                _ = Task.Delay(_duration, cancellationToken).ContinueWith(_ => tcs.TrySetResult(true));
                toAnimTask = tcs.Task;
            }
        }

        // Await both tracking tasks to ensure the transition engine handles the lifecycle correctly
        await Task.WhenAll(fromAnimTask, toAnimTask);

        // Reset elements back to standard UI thread states if required by your layout container
        if (from != null) from.Opacity = 0.0;
        if (to != null) to.Opacity = 1.0;

        _first = false;
    }
}

public class CustomNoTransition(TimeSpan duration) : IPageTransition
{
    private readonly TimeSpan _duration = duration;

    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        var parent = from?.GetVisualParent() ?? to?.GetVisualParent();
        if (parent == null) return;

        var fromAnimTask = Task.CompletedTask;
        var toAnimTask = Task.CompletedTask;

        // Animate the "from" page to fade out
        if (from != null)
        {
            var fromAnimation = new Avalonia.Animation.Animation
            {
                Duration = _duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 0.0 } }
                    }
                }
            };
            fromAnimTask = fromAnimation.RunAsync(from, cancellationToken);
        }

        // Animate the "to" page to fade in
        if (to != null)
        {
            //to.Opacity = 0.0;

            var toAnimation = new Avalonia.Animation.Animation
            {
                Duration = _duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.1 } }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter { Property = Visual.OpacityProperty, Value = 1.0 } }
                    }
                }
            };
            toAnimTask = toAnimation.RunAsync(to, cancellationToken);
        }

        await Task.WhenAll(fromAnimTask, toAnimTask);
    }
}

public class CustomPageSlideTransition : IPageTransition
{
    /// <summary>
    /// The axis on which the PageSlide should occur
    /// </summary>
    public enum SlideAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageSlide"/> class.
    /// </summary>
    public CustomPageSlideTransition()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageSlide"/> class.
    /// </summary>
    /// <param name="duration">The duration of the animation.</param>
    /// <param name="orientation">The axis on which the animation should occur</param>
    public CustomPageSlideTransition(TimeSpan duration, SlideAxis orientation = SlideAxis.Horizontal)
    {
        Duration = duration;
        Orientation = orientation;
    }

    /// <summary>
    /// Gets the duration of the animation.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Gets the orientation of the animation.
    /// </summary>
    public SlideAxis Orientation { get; set; }

    /// <summary>
    /// Gets or sets element entrance easing.
    /// </summary>
    public Easing SlideInEasing { get; set; } = new LinearEasing();

    /// <summary>
    /// Gets or sets element exit easing.
    /// </summary>
    public Easing SlideOutEasing { get; set; } = new LinearEasing();

    /// <inheritdoc />
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        //var parent = GetVisualParent(from, to);
        var parent = from?.GetVisualParent() ?? to?.GetVisualParent();
        if (parent == null) return;

        var tasks = new List<Task>();
        var distance = Orientation == SlideAxis.Horizontal ? parent.Bounds.Width : parent.Bounds.Height;
        var translateProperty = Orientation == SlideAxis.Horizontal ? TranslateTransform.XProperty : TranslateTransform.YProperty;

        if (from != null)
        {
            // This!!!!
            from.IsVisible = true;
            from.Opacity = 1.0;

            var animation = new Animation
            {
                Easing = SlideOutEasing,
                Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter { Property = translateProperty, Value = 0d } },
                            Cue = new Cue(0d)
                        },
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter
                                {
                                    Property = translateProperty,
                                    Value = forward ? -distance : distance
                                }
                            },
                            Cue = new Cue(1d)
                        }
                    },
                Duration = Duration
            };
            tasks.Add(animation.RunAsync(from, cancellationToken));
        }

        if (to != null)
        {
            to.IsVisible = true;
            to.Opacity = 1.0;
            var animation = new Animation
            {
                Easing = SlideInEasing,
                Children =
                    {
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter
                                {
                                    Property = translateProperty,
                                    Value = forward ? distance : -distance
                                }
                            },
                            Cue = new Cue(0d)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter { Property = translateProperty, Value = 0d } },
                            Cue = new Cue(1d)
                        }
                    },
                Duration = Duration
            };
            tasks.Add(animation.RunAsync(to, cancellationToken));
        }

        await Task.WhenAll(tasks);
        //await Task.WhenAll(tasks[0]);

        if (from != null && !cancellationToken.IsCancellationRequested)
        {
            from.IsVisible = false;

            from.Opacity = 0.0;
        }
    }
    /*
    /// <summary>
    /// Gets the common visual parent of the two control.
    /// </summary>
    /// <param name="from">The from control.</param>
    /// <param name="to">The to control.</param>
    /// <returns>The common parent.</returns>
    /// <exception cref="ArgumentException">
    /// The two controls do not share a common parent.
    /// </exception>
    /// <remarks>
    /// Any one of the parameters may be null, but not both.
    /// </remarks>
    protected static Visual GetVisualParent(Visual? from, Visual? to)
    {
        var p1 = (from ?? to)!.VisualParent;
        var p2 = (to ?? from)!.VisualParent;

        if (p1 != null && p2 != null && p1 != p2)
        {
            throw new ArgumentException("Controls for PageSlide must have same parent.");
        }

        return p1 ?? throw new InvalidOperationException("Cannot determine visual parent.");
    }
    */
}

