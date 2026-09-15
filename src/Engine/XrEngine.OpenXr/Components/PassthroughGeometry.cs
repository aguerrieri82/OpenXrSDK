using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using XrMath;

namespace XrEngine.OpenXr
{
    public class PassthroughGeometry : BaseXrComponent<Scene3D>
    {
        private bool _isInit;
        private OculusSceneView? _sceneModel;
        private XrPassthroughLayer? _layer;


        protected override void AttachXr()
        {
            _sceneModel = _host.Descendants<OculusSceneView>().FirstOrDefault();

            _layer = _xrApp!.Layers.List.OfType<XrPassthroughLayer>().FirstOrDefault();
        }

        protected override void DetachXr()
        {
            _isInit = false;    
        }

        protected override void UpdateWork(RenderContext ctx)
        {
            if (_isInit || _layer == null)
                return;

            if (_sceneModel == null || _sceneModel.Children.Count == 0)
                return;

            Debug.Assert(_xrApp != null);

            var meshObj = (TriangleMesh)_sceneModel.Children[0];

            Debug.Assert(meshObj.Geometry != null);

            var triMesh = new Mesh3
            {
                Indices = meshObj.Geometry.Indices,
                Vertices = meshObj.Geometry.ExtractPositions()
            };

            var test = Cube3D.Default;

            triMesh.Indices = test.Indices;
            triMesh.Vertices = test.Vertices.Select(a => a.Pos).ToArray()!;

            var ptMesh = _layer.AddMesh(triMesh, _xrApp.ReferenceSpace, meshObj);

            _layer.UpdateMesh(
                ptMesh, new Posef
                {
                    Orientation = Quaternion.Identity.ToQuaternionf()
                },
                new Vector3f(0.2f, 0.2f, 0.2f),
                _xrApp.ReferenceSpace,
                _xrApp.FramePredictedDisplayTime);
        }
    }
}
