﻿using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace XrEditor
{
    public class OpenContextMenuAction : TriggerAction<FrameworkElement>
    {
        protected override void Invoke(object parameter)
        {
            if (AssociatedObject?.ContextMenu != null)
            {
                AssociatedObject.ContextMenu.PlacementTarget = AssociatedObject;
                AssociatedObject.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                AssociatedObject.ContextMenu.IsOpen = true;
            }
        }
    }
}
