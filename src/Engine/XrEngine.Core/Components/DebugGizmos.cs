using XrMath;

namespace XrEngine.Components
{
    public class DebugGizmos : Behavior<Scene>, IDrawGizmos
    {
        public DebugGizmos()
        {
            Debuggers = [];
            ShowBounds = false;
        }

        public void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            if (ShowBounds)
            {
                foreach (var obj in _host!.DescendantsWithFeature<ILocalBounds>())
                {
                    if (obj.Object is Group3D)
                        canvas.State.Color = new Color(0, 1, 1, 1);
                    else
                        canvas.State.Color = new Color(1, 1, 0, 1);

                    var local = obj.Feature.LocalBounds;

                    canvas.State.Transform = obj.Object.WorldMatrix;
                    canvas.DrawBounds(local);
                }

                /*
                canvas.State.Color = new Color(0, 1, 1, 1);
                canvas.State.Transform = Matrix4x4.Identity;

                foreach (var obj in _host!.Descendants<Group3D>())
                    canvas.DrawBounds(obj.WorldBounds);
                */
            }

            foreach (var debugger in Debuggers)
                debugger.DrawGizmos(canvas);

            canvas.Restore();
        }


        public HashSet<IDrawGizmos> Debuggers { get; }

        public bool ShowBounds { get; set; }
    }
}
