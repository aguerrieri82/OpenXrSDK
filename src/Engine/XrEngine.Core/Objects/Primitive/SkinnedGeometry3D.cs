
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.Objects
{
    public class SkinnedGeometry3D : Geometry3D, IVertexAttributes
    {
        protected SkinData[] _skin;
        protected Dictionary<int, Bounds3> _jointBounds = [];
        protected bool _skinDirty;

        public SkinnedGeometry3D()
        {
            _skin = [];
        }

        public SkinData[] Skin
        {
            get => _skin;
            set 
            {
                _skin = value;
                _skinDirty = true;
                _boundsDirty = true;
            }
        }

        protected virtual void UpdateJointBounds()
        {
            static int Max(in Vector4I v)
            {
                return Math.Max(v.X, Math.Max(v.Y, Math.Max(v.Z, v.W)));
            }

            var maxIndex = _skin.Max(a => Max(a.JointIndices));

            var builders = new Bounds3Builder[maxIndex + 1];
            for (var i = 0; i < builders.Length; i++)
                builders[i] = new Bounds3Builder();

            void Add(int jIndex, int vIndex)
            {
                ref var builder = ref builders[jIndex];
                builder.Add(_vertices[vIndex].Pos);
            }

            var threshold = JointWeigthBoundsThreshold;

            for (var i = 0; i  < _skin.Length; i++)
            {
                ref var skin = ref _skin[i];
                
                if (skin.JointWeights.X > threshold)
                    Add(skin.JointIndices.X, i);
                
                if (skin.JointWeights.Y > threshold)
                    Add(skin.JointIndices.Y, i);
                
                if (skin.JointWeights.Z > threshold)
                    Add(skin.JointIndices.Z, i);

                if (skin.JointWeights.W > threshold)
                    Add(skin.JointIndices.W, i);
            }


            _jointBounds = new();

            for (var i = 0; i < builders.Length; i++)
            {
                var result = builders[i].Result;
                if (result.Size != Vector3.Zero)
                    _jointBounds[i] = result;
            }
        }


        public override void UpdateBounds()
        {
            base.UpdateBounds();
            UpdateJointBounds();
        }

        public override void NotifyLoaded()
        {
            base.NotifyLoaded();

            if (this.Is(EngineObjectFlags.GpuOnly))
                _skin = [];
        }

        public Dictionary<int, Bounds3> JointBounds
        {
            get
            {
                if (_skinDirty)
                    UpdateBounds();
                return _jointBounds;
            }
        }

        public float JointWeigthBoundsThreshold { get; set; }

        int IVertexAttributes.BufferCount => 1;

        Array IVertexAttributes.GetBuffer(int index)
        {
            Debug.Assert(index == 0);

            return _skin;
        }
    }
}
