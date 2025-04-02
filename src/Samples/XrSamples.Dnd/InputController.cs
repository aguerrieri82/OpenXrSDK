using OpenXr.Framework.Oculus;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using XrEngine;
using XrEngine.OpenXr;

namespace XrSamples.Dnd
{
    public class InputController : Behavior<Scene3D>
    {
        private XrOculusTouchController? _inputs;

        public void Configure(XrEngineApp e)
        {
            _inputs = e.GetInputs<XrOculusTouchController>();

        }

        [Action]
        public void ScaleDown()
        {
            Scale(0.8f);
        }

        [Action]
        public void ScaleUp()
        {
           Scale(1f / 0.8f);

        }

        public void Scale(float value)
        {
            var center = Player!.WorldPosition;
            var newTrans = Map!.Transform.Matrix *
                Matrix4x4.CreateTranslation(-center) *
                Matrix4x4.CreateScale(new Vector3( value)) *
                Matrix4x4.CreateTranslation(center);

            Map!.Transform.Set(newTrans);


        }

        [Action]
        public void ToggleSimplified()
        {
            var materials = Map!.Descendants<TriangleMesh>()
                               .SelectMany(a => a.Materials)
                               .OfType<PbrV2Material>()
                               .Distinct();

            foreach (var material in materials)
            {
                material.Simplified = !material.Simplified;
                material.NotifyChanged(ObjectChangeType.Property);
            }
        }

        protected override void Update(RenderContext ctx)
        {
            var aButton = _inputs!.Right!.Button!.AClick!;
            var bButton = _inputs.Right!.Button!.BClick!;
            var xButton = _inputs.Left!.Button!.XClick!;
            var rTrigger = _inputs.Right!.TriggerValue!;

            if (bButton.IsChanged && bButton.Value)
            {
                if (rTrigger.Value > 0.8)
                    ScaleUp();
                else
                    ScaleDown();
            }
            if (xButton.IsChanged && xButton.Value)
            {
                ToggleSimplified();
            }
        }

        protected Object3D? Player => ((DndScene?)_host?.Scene)?.Player;

        protected Group3D? Map => ((DndScene?)_host?.Scene)?.Map;
    }
}
