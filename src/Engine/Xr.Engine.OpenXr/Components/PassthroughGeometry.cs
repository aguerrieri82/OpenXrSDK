﻿using OpenXr.Framework;
using OpenXr.Framework.Oculus;
using Silk.NET.OpenXR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Xr.Engine.OpenXr
{
    public class PassthroughGeometry : Behavior<Scene>
    {
        private bool _isInit;
        private OculusSceneModel? _sceneModel;
        private XrPassthroughLayer? _ptLayer;

        protected override void Update(RenderContext ctx)
        {
            if (!_isInit)
            {
                var xrApp = XrApp.Current;

                _sceneModel ??= _host!.Descendants<OculusSceneModel>().FirstOrDefault();

                if (_sceneModel != null && _sceneModel.Children.Count > 0)
                {
                    _ptLayer ??= xrApp?.Layers.Layers.OfType<XrPassthroughLayer>().FirstOrDefault();

                    if (_ptLayer != null)
                    {
                        var meshObj = (TriangleMesh)_sceneModel.Children[0];

                        Debug.Assert(meshObj.Geometry != null);

                        var triMesh = new XrMesh
                        {
                            Indices = meshObj.Geometry.Indices,
                            Vertices = meshObj.Geometry.ExtractPositions()
                        };

                        var test = Cube3D.Instance;

                        triMesh.Indices = test.Indices!;
                        triMesh.Vertices = test.Vertices.Select(a => a.Pos).ToArray()!;

                        var ptMesh = _ptLayer.AddMesh(triMesh, xrApp!.Stage, meshObj);

                        /*
                        _ptLayer.UpdateMesh(
                            ptMesh, new Posef
                            {
                                Orientation = meshObj.Transform.Orientation.ToQuaternionf(),
                                Position = meshObj.Transform.Position.ToVector3f(),  
                            },
                            meshObj.Transform.Scale.ToVector3f(),
                            xrApp.Stage,
                            xrApp.LastFrameTime);
                        */

                        
                        _ptLayer.UpdateMesh(
                            ptMesh, new Posef
                            {
                                Orientation = Quaternion.Identity.ToQuaternionf() 
                            },
                            new Vector3f(0.2f, 0.2f, 0.2f),
                            xrApp.Stage,
                            xrApp.LastFrameTime);
                        
                        _isInit = true;
                    }
                }

            }
            base.Update(ctx);
        }
    }
}