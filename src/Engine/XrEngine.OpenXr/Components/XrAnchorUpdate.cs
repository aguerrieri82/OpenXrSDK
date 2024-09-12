using OpenXr.Framework;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XrEngine.OpenXr.Components
{
    public class XrAnchorUpdate : Behavior<Object3D>
    {
        double _lastUpdateTime;

        protected override void Start(RenderContext ctx)
        {
            base.Start(ctx);
        }

        protected override void Update(RenderContext ctx)
        {
            var xrApp = XrApp.Current;
            if (xrApp == null || !xrApp.IsStarted)
                return;

            if (ctx.Time - _lastUpdateTime < UpdateInterval.TotalSeconds)
                return;

            var loc = xrApp.LocateSpace(Space, xrApp.Stage, xrApp.FramePredictedDisplayTime);
            if (loc.IsValid)
            {
                _host!.Transform.Position = loc.Pose.Position;
                _host!.Transform.Orientation = loc.Pose.Orientation;
            }

            _lastUpdateTime = ctx.Time;

            base.Update(ctx);
        }


        public TimeSpan UpdateInterval { get; set; }

        public Guid AnchorId { get; set; }

        public Space Space { get; set; }
    }
}
