﻿namespace Xr.Engine.Editor
{
    public class PropertyView : BaseView
    {

        public PropertyView()
        {

        }

        public string? Label { get; set; }

        public IPropertyEditor? Editor { get; set; }
    }
}
