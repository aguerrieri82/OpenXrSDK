using System.Numerics;
using XrEditor.Services;
using XrEngine;
using XrInteraction;
using XrMath;

namespace XrEditor
{
    public class SelectionTool : PickTool, IOutlineSource
    {
        private readonly SelectionManager _selection;
        private readonly NodeManager _nodes;
        private INode[] _lastSelection = [];
        private Vector2 _downPos;
        private DetachedLayer? _selectionLayer;
        private TriangleMesh[]? _lastOutline;

        public SelectionTool()
        {
            Context.Implement<IOutlineSource>(this);

            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
            _nodes = Context.Require<NodeManager>();
        }

        public override void NotifySceneChanged()
        {
            if (_sceneView?.Scene == null)
                return;

            var layers = _sceneView.Scene.Layers;

            _selectionLayer ??= layers.Add(new DetachedLayer()
            {
                Name = "Selection",
                IsVisible = false,
                Usage = DetachedLayerUsage.Selection | DetachedLayerUsage.Outline
            });

            base.NotifySceneChanged();
        }

        bool IOutlineSource.HasOutline(Object3D obj, out Color color)
        {
            color = new Color(1, 1, 0, 0.7f);
            return _lastOutline != null && _lastOutline.Contains(obj);
        }

        bool IOutlineSource.HasOutlines()
        {
            return _lastOutline != null && _lastOutline.Length > 0;
        }

        private void OnSelectionChanged(IReadOnlyCollection<INode> items)
        {
            _lastSelection = items.ToArray();

            if (_selectionLayer != null)
            {
                _selectionLayer.BeginUpdate();
                _selectionLayer.Clear();

                var outlineMeshes = _lastSelection
                    .Select(a => a.Value)
                    .OfType<Object3D>()
                    .Where(a => a is not Scene3D)
                    .SelectMany(a => a.DescendantsOrSelf())
                    .OfType<TriangleMesh>();


                _lastOutline = outlineMeshes.ToArray();

                foreach (var item in _lastOutline)
                    _selectionLayer.Add(item);

                _selectionLayer.EndUpdate();
            }
        }
        protected override void OnPointerDown(Pointer2Event ev)
        {
            _downPos = ev.Position;
            base.OnPointerDown(ev);
        }

        protected override void OnPointerUp(Pointer2Event ev)
        {
            if (ev.IsLeftDown && _downPos == ev.Position)
            {
                if (_currentPick == null)
                    _selection.Clear();
                else
                {
                    _selection.Set(_nodes.CreateNode(_currentPick));
                    Log.Info(this, _lastCollision?.Point.ToString() ?? "");
                }

            }

            base.OnPointerUp(ev);
        }

        public override void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            foreach (var item in _lastSelection.Select(a => a.Value).OfType<Object3D>())
            {
                var local = item.Feature<ILocalBounds>();

                if (local != null)
                {
                    canvas.State.Color = new Color(0, 1, 1, 1);
                    canvas.State.Transform = item.WorldMatrix;
                    canvas.DrawBounds(local.LocalBounds);
                }

                canvas.State.Transform = Matrix4x4.Identity;
                canvas.State.Color = new Color(1, 1, 1, 1);
                canvas.DrawBounds(item.WorldBounds);

            }

            var collision = _lastCollision;

            if (collision != null)
            {
                if (collision.Normal != null)
                {
                    canvas.State.Color = new Color(0, 1, 1, 1);
                    canvas.State.Transform = Matrix4x4.Identity;

                    canvas.DrawLine(collision.Point, collision.Point + collision.Normal.Value * 0.5f);
                }

                if (collision.Tangent != null)
                {
                    canvas.State.Color = new Color(0, 1, 1, 1);
                    var tangent = new Vector3(collision.Tangent.Value.X, collision.Tangent.Value.Y, collision.Tangent.Value.Z).Normalize();

                    tangent = tangent.Transform(collision.Object!.NormalMatrix).Normalize();

                    canvas.DrawLine(collision.Point, collision.Point + tangent * 0.5f);
                }

            }

            canvas.Restore();

            base.DrawGizmos(canvas);
        }

    }
}
