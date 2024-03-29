﻿using System.Numerics;
using XrEditor.Services;
using XrEngine;
using XrEngine.Interaction;
using XrMath;

namespace XrEditor
{
    public class SelectionTool : PickTool
    {
        private SelectionManager _selection;
        private NodeFactory _nodes;
        private INode[] _lastSelection = [];

        public SelectionTool()
        {
            _selection = Context.Require<SelectionManager>();
            _selection.Changed += OnSelectionChanged;
            _nodes = Context.Require<NodeFactory>();
        }

        private void OnSelectionChanged(IReadOnlyCollection<INode> items)
        {
            _lastSelection = items.ToArray();
        }

        protected override void OnPointerDown(Pointer2Event ev)
        {
            if (ev.IsLeftDown)
            {
                if (_currentPick == null)
                    _selection.Clear();
                else
                    _selection.Set(_nodes.CreateNode(_currentPick));
            }

            base.OnPointerDown(ev);
        }

        public override void DrawGizmos(Canvas3D canvas)
        {
            canvas.Save();

            canvas.State.Color = new Color(0, 1, 1, 1);

            foreach (var item in _lastSelection.Select(a=> a.Value).OfType<Object3D>())
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