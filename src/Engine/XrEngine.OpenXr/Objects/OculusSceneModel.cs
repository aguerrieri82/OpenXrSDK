using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine.Physics;

namespace XrEngine.OpenXr
{
    public class OculusSceneModel : Group3D
    {
        protected bool _isSceneLoaded;
        protected Space _meshSpace;
        protected bool _isSceneLoading;
        protected XrApp? _app;

        public OculusSceneModel()
        { 
            Material = PbrMaterial.CreateDefault(Color.White);
            Name = "SceneModel";
        }

        public override void Update(RenderContext ctx)
        {
            if (!_isSceneLoaded && !_isSceneLoading)
            {
                if (_app == null && XrApp.Current != null)
                    _app = XrApp.Current;

                if (_app != null && _app.IsStarted)
                    _ = LoadSceneAsync();
            }

            base.Update(ctx);
        }

        protected async Task LoadSceneAsync()
        {
            _isSceneLoading = true;

            var oculus = _app!.Plugin<OculusXrPlugin>();

            try
            {
                var anchors = await oculus.QueryAllAnchorsAsync();

                var meshAnchor = anchors.FirstOrDefault(a => oculus.GetSpaceComponentEnabled(a.Space, OculusXrPlugin.XR_SPACE_COMPONENT_TYPE_TRIANGLE_MESH_META));

                if (meshAnchor.Space.Handle == 0)
                    return;

                var sceneMesh = oculus.GetSpaceTriangleMesh(meshAnchor.Space);

                var geo = new Geometry3D
                {
                    Indices = sceneMesh.Indices!,
                    ActiveComponents = VertexComponent.Position,
                    Vertices = sceneMesh.Vertices!.Select(a => new VertexData
                    {
                        Pos = new Vector3(a.X, a.Y, a.Z)
                    }).ToArray()
                };

                geo.Rebuild();
                geo.ComputeNormals();

                var isLocatable = oculus.EnumerateSpaceSupportedComponentsFB(meshAnchor.Space).Contains(SpaceComponentTypeFB.LocatableFB);
                if (isLocatable)
                    await oculus.SetSpaceComponentStatusAsync(meshAnchor.Space, SpaceComponentTypeFB.LocatableFB, true);

                var location = _app!.LocateSpace(meshAnchor.Space, _app.Stage, 1);

                var mesh = new TriangleMesh(geo, Material);
                mesh.Name = "global-mesh";
                mesh.Transform.Position = location.Pose.Position;
                mesh.Transform.Orientation = location.Pose.Orientation;

                mesh.AddComponent(new MeshCollider());

                var rigidBody = mesh.AddComponent<RigidBody>();

                rigidBody.Type = PhysicsActorType.Static;
                rigidBody.Material = new PhysicsMaterialInfo
                {
                    DynamicFriction = 1,
                    StaticFriction = 1,
                    Restitution = 1
                };

                AddChild(mesh);

                _isSceneLoaded = true;

                _meshSpace = meshAnchor.Space;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _isSceneLoading = false;
            }
        }

        public Material Material { get; set; }
    }
}
