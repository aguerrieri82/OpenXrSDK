﻿using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using PhysX.Framework;
using Silk.NET.OpenXR;
using System.Numerics;
using XrEngine.OpenXr.Components;
using XrEngine.Physics;
using XrMath;

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
            Material = (Material)MaterialFactory.CreatePbr(Color.White);

            Flags |= EngineObjectFlags.DisableNotifyChangedScene;

            Name = "SceneModel";

            AddPhysics = true;
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
                {
                    if (!oculus.GetSpaceComponentEnabled(meshAnchor.Space, SpaceComponentTypeFB.LocatableFB))
                        await oculus.SetSpaceComponentStatusAsync(meshAnchor.Space, SpaceComponentTypeFB.LocatableFB, true);
                }

                var location = _app!.LocateSpace(meshAnchor.Space, _app.Stage, 1);

                var mesh = new TriangleMesh(geo, Material)
                {
                    Name = "global-mesh"
                };

                mesh.AddComponent(new XrAnchorUpdate
                {
                    Space = meshAnchor.Space
                });

                if (AddPhysics)
                {
                    var rigidBody = mesh.AddComponent<RigidBody>();

                    rigidBody.Type = PhysicsActorType.Static;
                    rigidBody.ContactOffset = 0.2f;
                    rigidBody.Material = new PhysicsMaterialInfo
                    {
                        DynamicFriction = 1,
                        StaticFriction = 1,
                        Restitution = 0.7f
                    };

                    mesh.AddComponent(new PyMeshCollider());
                }

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

        public bool AddPhysics { get; set; }

        public Material Material { get; set; }
    }
}
