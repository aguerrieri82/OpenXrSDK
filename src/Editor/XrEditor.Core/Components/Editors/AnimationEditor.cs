using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrEngine.Animation;


namespace XrEditor
{
    public class AnimationEditor : BaseEditor<IAnimation, IAnimation>
    {
        public AnimationEditor()
        {
            Properties = [];
        }

        protected override void OnEditValueChanged(IAnimation newValue)
        {
            base.OnEditValueChanged(newValue);
            var result = new List<PropertyView>();
            PropertyView.CreateProperties(newValue, null, result);
            Properties = result.ToArray();
        }


        public PropertyView[] Properties { get; set; }
    }
}
