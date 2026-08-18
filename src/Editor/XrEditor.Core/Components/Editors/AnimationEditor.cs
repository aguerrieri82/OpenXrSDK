using CefSharp.DevTools.Media;
using PureHDF.Selections;
using System;
using System.Collections.Generic;
using System.Text;
using XrEngine;
using XrEngine.Animation;


namespace XrEditor
{
    public class AnimationEditor : BaseEditor<IAnimation, IAnimation>, IDisposable
    {
        public AnimationEditor()
        {
            Properties = [];
            Player = new PlayerView();
        }

        protected override void OnEditValueChanged(IAnimation newValue)
        {
            base.OnEditValueChanged(newValue);

            var result = new List<PropertyView>();
            
            PropertyView.CreateProperties(newValue, null, result);
            
            Properties = result.ToArray();
            
            if (Player.EditValue is IDisposable disposable)
                disposable.Dispose();

            Player.EditValue = new AnimationPlayer(newValue);
        }

        public void Dispose()
        {
            if (Player.EditValue is IDisposable disposable)
                disposable.Dispose();

            GC.SuppressFinalize(this);
        }

        public PlayerView Player { get; }

        public PropertyView[] Properties { get; set; }

    }
}
