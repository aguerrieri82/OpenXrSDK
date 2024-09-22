using System.Numerics;
using XrEditor.Services;
using XrEngine;
using XrEngine.Layers;
using XrInteraction;
using XrMath;

namespace XrEditor
{
    public class SelectionTool : PickTool
    {
        private readonly SelectionManager _selection;
        private readonly NodeManager _nodes;
        private INode[] _lastSelection = [];
        private Vector2 _downPos;
        private DetachedLayer? _selectionLayer;
        private TriangleMesh[]? _lastOutline;

        public SelectionTool()
        {
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
            _nodes = Context.Require<NodeManager>();
        }

        public override void NotifySceneChanged()
        {
            if (_sceneView?.Scene == null)
                return;

            var layers = _sceneView.Scene.Layers;

            _selectionLayer = layers.Layers
                .OfType<DetachedLayer>()
                .Where(a => a.Name == "Selection")
                .FirstOrDefault();

            _selectionLayer ??= layers.Add(new DetachedLayer() { Name = "Selection" });

            base.NotifySceneChanged();
        }

        private void EnableStencil(IEnumerable<TriangleMesh> items, bool enable)
        {
            foreach (var item in items)
            {
                foreach (var material in item.Materials)
                    material.WriteStencil = enable ? 1 : null;
            }
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
                    .SelectMany(a => a.DescendantsOrSelf())
                    .OfType<TriangleMesh>();

                if (_lastOutline != null)
                    EnableStencil(_lastOutline, false);
                
                _lastOutline = outlineMeshes.ToArray();

                EnableStencil(_lastOutline, true);

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
                    _selection.Set(_nodes.CreateNode(_currentPick));
            }

            base.OnPointerUp(ev);
        }

        public override void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            canvas.State.Color = new Color(0, 1, 1, 1);

            foreach (var item in _lastSelection.Select(a => a.Value).OfType<Object3D>())
            {
                var local = item.Feature<ILocalBounds>();

                if (local != null)
                {
                    canvas.State.Transform = item.WorldMatrix;
                    canvas.DrawBounds(local.LocalBounds);
                }
            }

            if (_lastCollision?.Normal != null)
            {
                canvas.State.Color = new Color(0, 1, 0, 1);

                var normalMatrix = Matrix4x4.Transpose(_lastCollision.Object!.WorldMatrixInverse);

                canvas.DrawLine(_lastCollision.Point, _lastCollision.Point + _lastCollision.Normal.Value.Transform(normalMatrix).Normalize());

                if (_lastCollision.Tangent != null)
                {
                    canvas.State.Color = new Color(0, 1, 1, 1);
                    var nq = Quaternion.Normalize(_lastCollision.Tangent.Value);
                    canvas.DrawLine(_lastCollision.Point, _lastCollision.Point + new Vector3(nq.X, nq.Y, nq.Z));
                }
            }

            canvas.Restore();

            base.DrawGizmos(canvas);
        }
    }
}
