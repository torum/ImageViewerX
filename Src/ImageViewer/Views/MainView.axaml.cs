using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ImageViewer.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageViewer.Views;

public partial class MainView : UserControl
{
    readonly MainViewModel _viewModel;

    public MainView()
    {
        _viewModel = App.GetService<MainViewModel>();

        InitializeComponent();

        /*
        var compositeTransition = new CompositePageTransition();
        compositeTransition.PageTransitions.Add(new PageSlide(TimeSpan.FromMilliseconds(500), PageSlide.SlideAxis.Vertical));
        compositeTransition.PageTransitions.Add(new PageSlide(TimeSpan.FromMilliseconds(500), PageSlide.SlideAxis.Horizontal));
        compositeTransition.PageTransitions.Add(new CrossFade(TimeSpan.FromMilliseconds(1000)));
        this.ImageTransitioningContentControl.PageTransition = compositeTransition;
        */

        _viewModel.TransitionsHasBeenChanged += OnTransitionsHasBeenChanged;

        UpdatePageTransition();
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
            compositeTransition.PageTransitions.Add(new CustomFadeTransition(TimeSpan.FromMilliseconds(1000), _viewModel.IsEffectCrossfadeOn));
            this.ImageTransitioningContentControl.PageTransition = compositeTransition;
        }
        else if (_viewModel.IsEffectPageSlideOn)
        {
            var compositeTransition = new CompositePageTransition();

            if (_viewModel.IsEffectCrossfadeOn)
            {
                var crossFade = new CrossFade(TimeSpan.FromSeconds(0.3));
                compositeTransition.PageTransitions.Add(crossFade);
            }

            //var pageSlide = new PageSlide(TimeSpan.FromSeconds(0.5), PageSlide.SlideAxis.Vertical)
            var pageSlide = new CustomPageSlideTransition(TimeSpan.FromSeconds(0.5), CustomPageSlideTransition.SlideAxis.Horizontal)
            {
                SlideInEasing = new CubicEaseOut(),
                SlideOutEasing = new CubicEaseIn()

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
            // shouldn't be happening.
            var compositeTransition = new CompositePageTransition();
            compositeTransition.PageTransitions.Add(new CustomFadeTransition(TimeSpan.FromMilliseconds(1000), _viewModel.IsEffectCrossfadeOn));
            this.ImageTransitioningContentControl.PageTransition = compositeTransition;
        }
    }

    public void ToggleSlideshowAnimation(bool isOn, TimeSpan duration)
    {
        var image = this.ImageGrid;

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

    public static void StartFadeInAnimation(this Visual control, TimeSpan duration)
    {
        if (duration == TimeSpan.Zero) return;

        var compositionVisual = ElementComposition.GetElementVisual(control);

        if (compositionVisual is null) return;

        var animation = compositionVisual.Compositor.CreateScalarKeyFrameAnimation();

        animation.InsertKeyFrame(0f, 0f);
        //animation.InsertKeyFrame(0.5f, 0.5f);
        animation.InsertKeyFrame(1f, 1f);
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
            to.Opacity = 0.0;

            // (!_crossFade) Do not await if _crossFade is true.
            // (!_first) Do not await for the first time.
            if ((!_crossFade) && (!_first))
            {
                await Task.Delay(_duration, cancellationToken);
            }

            var toAnimation = new Avalonia.Animation.Animation
            {
                Duration = _duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0),
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

        await fromAnimTask;
        await toAnimTask;
        //await Task.WhenAll(fromAnimTask, toAnimTask);

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
