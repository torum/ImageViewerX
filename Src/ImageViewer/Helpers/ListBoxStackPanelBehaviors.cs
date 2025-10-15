using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Collections.Generic;
using System.Linq;

namespace ImageViewer.Helpers;

public sealed class ListBoxStackPanelBehaviors
{
    // The target is ListBox, and the value is an IEnumerable of objects.
    public static readonly AttachedProperty<IEnumerable<object>> VisibleItemsProperty =
        AvaloniaProperty.RegisterAttached<ListBoxStackPanelBehaviors, ListBox, IEnumerable<object>>("VisibleItems");

    public static IEnumerable<object> GetVisibleItems(ListBox element)
    {
        return element.GetValue(VisibleItemsProperty);
    }

    public static void SetVisibleItems(ListBox element, IEnumerable<object> value)
    {
        element.SetValue(VisibleItemsProperty, value);
    }

    static ListBoxStackPanelBehaviors()
    {
        // This class handler is triggered whenever a ListBox is loaded into the visual tree.
        ListBox.LoadedEvent.AddClassHandler<ListBox>((sender, e) =>
        {
            var listBox = sender;

            if (listBox.Tag != null)
            {
                //Debug.WriteLine("(listBox.Tag != null) @ListBoxStackPanelBehaviors");
                return;
            }

            listBox.Tag = "LoadedEvent_ListBoxStackPanelBehaviors";

            var scrollViewer = listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

            var virtualPanel = listBox.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();

            if ((scrollViewer != null) && (virtualPanel != null))
            {
                // Subscribe to the scroll event to update the visible items.
                scrollViewer.ScrollChanged += (s, args) => UpdateVisibleItems(listBox, scrollViewer, virtualPanel);
                // .. size changed event too.
                scrollViewer.SizeChanged += (s, args) => UpdateVisibleItems(listBox, scrollViewer, virtualPanel);

                // Call it once initially to set the property.
                UpdateVisibleItems(listBox, scrollViewer, virtualPanel);
            }
        });
    }

    private static void UpdateVisibleItems(ListBox listBox, ScrollViewer scrollViewer, VirtualizingStackPanel virtualPanel)
    {
        //var _scrollViewer = listBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (scrollViewer == null) return;

        //var _virtualPanel = listBox.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();
        if (virtualPanel == null) return;

        var viewportRect = new Rect(new Point(scrollViewer.Offset.X, scrollViewer.Offset.Y), scrollViewer.Viewport);

        var visibleObjects = virtualPanel.Children.Where(child => child.Bounds.Intersects(viewportRect)).ToList();

        var visibleItems = new List<object>();

        foreach (var itemContainer in visibleObjects)
        {
            var dataItem = itemContainer.DataContext;
            if (dataItem != null)
            {
                visibleItems.Add(dataItem);
            }
        }

        // Set the new value of the attached property.
        listBox.SetValue(VisibleItemsProperty, visibleItems);
    }
}
