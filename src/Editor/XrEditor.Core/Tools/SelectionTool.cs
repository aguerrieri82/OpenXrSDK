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

        private Task SetSelectedAsync(IEnumerable<TriangleMesh> items, bool selected) =>
        _sceneView!.Dispatcher.ExecuteAsync(() =>
        {
            foreach (var item in items)
            {

                var outline = item.Materials.OfType<OutlineMaterial>().FirstOrDefault();
                if (outline == null && selected)
                {
                    outline = new OutlineMaterial()
                    {
                        Color = new Color(1, 1, 0, 0.7f),
                        CompareStencilMask = 1,
                        StencilFunction = StencilFunction.NotEqual,
                        Alpha = AlphaMode.Blend,
                        Size = 5,
                    };
                    item.Materials.Add(outline);
                }

                if (outline != null)
                    outline.IsEnabled = selected;

                foreach (var mat in item.Materials)
                {
                    if (mat is OutlineMaterial)
                        continue;

                    mat.WriteStencilMask(1, selected);
                }
            }
        });

        private async void OnSelectionChanged(IReadOnlyCollection<INode> items)
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

                if (_lastOutline != null)
                    await SetSelectedAsync(_lastOutline, false);

                _lastOutline = outlineMeshes.ToArray();

                await SetSelectedAsync(_lastOutline, true);

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

            var collision = _lastCollision;

            if (collision?.Normal != null)
            {
                canvas.State.Color = new Color(0, 1, 0, 1);
                canvas.State.Transform = Matrix4x4.Identity;

                var normal = (collision.Normal.Value.Transform(collision.Object!.NormalMatrix)).Normalize();

                canvas.DrawLine(collision.Point, collision.Point + normal * 0.5f);

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
