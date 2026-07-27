#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using System.Numerics;
using XrMath;

namespace XrEngine.OpenGL
{
    public unsafe class GlRayColliderPass : GlBaseSingleMaterialPass, 
        IGlDynamicRenderPass<RayPoniterTarget>,
        IRayHitTestSource
    {
        static Ray3? PointerProviderRay(IRayPointerProvider provider)
        {
            if (provider.Pointer == null)
                return null;

            var status = provider.Pointer.GetPointerStatus();

            if (!status.IsActive)
                return null;

            return status.Ray;
        }

        protected Func<Ray3?>? _getRay;
        protected GlRenderPassTarget _passTarget;
        protected GlBufferRing<Vector4I> _idsBuffer;
        protected PerspectiveCamera _camera;
        protected uint _objId;
        private Matrix4x4 _lastViewProjInv;
        private readonly List<Object3D> _lastObjects;
        private HitTestResult _lastHit;
        protected readonly List<Object3D> _objects = [];
        protected readonly uint _size;
        protected readonly Plane[] _cameraFrustum;

        internal GlRayColliderPass(OpenGLRender renderer, uint size = 2)
            : base(renderer)
        {
            _size = size;

            _passTarget = new GlRenderPassTarget(_gl)
            {
                DepthFormat = TextureFormat.Depth16,
                DepthMode = TargetDepthMode.Create,
                UseColor = false,
                Name = "Ray Collider"
            };

            _passTarget.Configure(_size, _size);

            _idsBuffer = new GlBufferRing<Vector4I>(_gl, BufferTargetARB.ShaderStorageBuffer);
            _idsBuffer.Allocate(_size * _size, 2);

            _flags |= GlRenderPassFlags.CustomCamera;

            _camera = new PerspectiveCamera
            {
                FovDegree = 1,
                Near = 0.01f,
                Far = 50f
            };

            _lastObjects = [];
            _objects = [];

            _cameraFrustum = new Plane[6];
        }

        public GlRayColliderPass(OpenGLRender renderer, Func<Ray3?> getRay)
            : this(renderer)
        {
            _getRay = getRay;
        }


        protected override bool BeginRender(GlUpdateContext ctx)
        {
            var ray = _getRay?.Invoke();

            if (ray == null)
                return false;

            _camera.WorldPosition = ray.Value.Origin;
            _camera.Forward = ray.Value.Direction;
            _camera.ViewSize = new Size2I(_size, _size);

            ctx.PassCamera = _camera;

            _passTarget.RenderTarget!.Begin(_camera);

            _renderer.State.SetClearColor(Color.Transparent);
            _renderer.State.SetWriteDepth(true);
            _renderer.State.SetWriteColor(true);

            _gl.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

            _objects.Clear();

            _camera.FrustumPlanes(_cameraFrustum, out int _);

            _idsBuffer.BindWrite(12);

            return base.BeginRender(ctx);
        }

        protected Vector3 ToView(uint x, uint y, float z)
        {
            return new Vector3(
                2.0f * x / _size - 1.0f,
                1.0f - 2.0f * y / _size,
                2f * z - 1f
            );
        }

        protected override IGlRenderTarget? GetRenderTarget()
        {
            return _passTarget.RenderTarget;
        }

        protected void ProcessLastHit()
        {
            if (!_idsBuffer.WaitRead(TimeSpan.FromMicroseconds(0)))
                return;

            var data = _idsBuffer.ActiveReadSpan;

            for (var y = 0; y < _size; y++)
            {
                for (var x = 0; x < _size; x++)
                {
                    var i = (y * (int)_size) + x;
                    var pixData = data[i];

                    if (pixData.X != 0)
                    {
                        _lastHit = new HitTestResult
                        {
                            Object = _lastObjects[pixData.X - 1],
                            Depth = pixData.Z / (float)ushort.MaxValue,
                            Index = (uint)pixData.Y
                        };

                        _lastHit.Pos = ToView((uint)x, (uint)y, _lastHit.Depth).Project(_lastViewProjInv);

                        if (_lastHit.Object is TriangleMesh mesh)
                        {
                            var geo = mesh.Geometry;
                            if (geo!.Indices != null && geo.Indices.Length > 0)
                            {
                                var index = geo.Indices[_lastHit.Index * 3];
                                _lastHit.Normal = geo.Vertices[index].Normal;
                            }
                            else
                                _lastHit.Normal = geo.Vertices[_lastHit.Index * 3].Normal;

                            //_lastHit.Normal = _lastHit.Normal.Transform(mesh.NormalMatrix).Normalize();
                        }

                        return;
                    }
                }
            }

            _lastHit.Object = null;
        }

        protected override void EndRender(GlUpdateContext ctx)
        {
            if (_objects.Count > 0)
            {
                ProcessLastHit();

                _idsBuffer.Swap();

                _lastViewProjInv = _camera.ViewProjectionInverse;

                _lastObjects.Clear();
                _lastObjects.AddRange(_objects);
            }
            else
            {
                _lastHit.Object = null;
            }

            _passTarget.RenderTarget!.End(true);

            base.EndRender(ctx);
        }

        protected override UpdateProgramResult UpdateProgram(UpdateShaderContext updateContext, Object3D model)
        {
            var objId = (uint)_objects.Count + 1;
            var effect = (RayCollisionEffect)_programInstance!.Material;
            effect.DrawId = objId;
            return UpdateProgramResult.Changed;
        }

        protected override void Draw(DrawContent draw)
        {
            _objects.Add(draw.Object!);
            draw.Draw!();
        }

        protected override bool CanDraw(DrawContent draw)
        {
            return draw.Object != null &&
                   draw.Object.WorldBounds.IntersectFrustum(_cameraFrustum);
        }

        protected override ShaderMaterial CreateMaterial()
        {
            return new RayCollisionEffect()
            {
                Size = _size
            };
        }

        protected override IEnumerable<IGlLayer> SelectLayers()
        {
            return _renderer.Layers.Where(a => a.Type == GlLayerType.MeshCollider);
        }

        public void SetOptions(RayPoniterTarget options)
        {
            _getRay = () => PointerProviderRay(options.Provider);
            options.Provider.SetHitTestSource(this);
        }

        public HitTestResult LastHit => _lastHit;

    }
}
