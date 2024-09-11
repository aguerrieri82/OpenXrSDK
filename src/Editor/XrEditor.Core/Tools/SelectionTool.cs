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
        private OutlineMaterial _selectionMat;


        public SelectionTool()
        {
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
            _nodes = Context.Require<NodeManager>();
            _selectionMat = new OutlineMaterial(new Color(1, 1,0, 0.8f), 10);
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

        private void OnSelectionChanged(IReadOnlyCollection<INode> items)
        {
            _lastSelection = items.ToArray();

            if (_selectionLayer != null)
            {
                _selectionLayer.BeginUpdate();
                _selectionLayer.Clear();

                foreach (var item in _lastSelection.Select(a=> a.Value).OfType<TriangleMesh>())
                    _selectionLayer.Add(PrepareMeshOutline(item));

                _selectionLayer.EndUpdate();
            }
        }

        protected TriangleMesh PrepareMeshOutline(TriangleMesh mesh)
        {
            var outline = mesh.GetProp<TriangleMesh>("Outline");
            if (outline == null)
            {
                outline = new TriangleMesh(mesh.Geometry!.TransformToLine());
                outline.Materials.Add(_selectionMat);

                mesh.SetProp("Outline", outline);

                mesh.AddBehavior((_, _) =>
                {
                    outline.WorldMatrix = mesh.WorldMatrix;
                });
            }



            return outline;
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
