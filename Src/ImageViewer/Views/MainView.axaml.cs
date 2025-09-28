using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering.Composition;
using System;
using System.Diagnostics;
using System.Numerics;

namespace ImageViewer.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public void UpDownAnimation1(bool isOn)
    {
        int direction;
        if (isOn)
        {
            direction = 10;
        }
        else
        {
            direction = -10;
        }

        var image = this.ImageGrid;
        CompositionVisual? compositionVisual = ElementComposition.GetElementVisual(image);
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

        /*
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.Target = "Offset";
        offsetAnimation.InsertExpressionKeyFrame(1.0f, "this.FinalValue");
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);

        offsetAnimation.StartAnimation("Offset", offsetAnimation);
        */
    }

    public void UpDownAnimation(bool isOn, TimeSpan duration)
    {
        var image = this.ImageGrid;

        if (isOn)
        {
            image.StartArkAnimation(isOn);
        }
        else
        {
            image.StartFadeInAnimation(duration);
        }
    }
}

public static partial class FadeInFadeOut
{
    public static void StartArkAnimation(this Visual control, bool isOn)
    {
        int direction;
        if (isOn)
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

    public static void StartFadeInAnimation(this Visual control, TimeSpan duration)
    {
        if (duration == TimeSpan.Zero) return;

        var compositionVisual = ElementComposition.GetElementVisual(control);

        if (compositionVisual is null) return;

        var animation = compositionVisual.Compositor.CreateScalarKeyFrameAnimation();

        animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(0.5f, 0.5f);
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
        animation.InsertKeyFrame(0.5f, 0.5f);
        animation.InsertKeyFrame(1f, 0f);
        animation.Duration = duration;
        animation.Target = nameof(CompositionVisual.Opacity);

        compositionVisual.StartAnimation(nameof(CompositionVisual.Opacity), animation);
    }
}

