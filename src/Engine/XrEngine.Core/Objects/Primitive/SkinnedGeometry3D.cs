
using System.Diagnostics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.Objects
{
    public class SkinnedGeometry3D : Geometry3D, IVertexAttributes
    {
        protected SkinData[] _skin;
        protected Bounds3[] _jointBounds = [];
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

            void Add(int jIndex, int vIndex)
            {
                ref var builder = ref builders[jIndex];
                builder.Add(_vertices[vIndex].Pos);
            }

            for (var i = 0; i  < _skin.Length; i++)
            {
                ref var skin = ref _skin[i];
                
                if (skin.JointWeights.X > 0)
                    Add(skin.JointIndices.X, i);
                
                if (skin.JointWeights.Y > 0)
                    Add(skin.JointIndices.Y, i);
                
                if (skin.JointWeights.Z > 0)
                    Add(skin.JointIndices.Z, i);

                if (skin.JointWeights.W > 0)
                    Add(skin.JointIndices.W, i);
            }

            _jointBounds = builders.Select(a => a.Result).ToArray();
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

        public Bounds3[] JointBounds
        {
            get
            {
                if (_skinDirty)
                    UpdateBounds();
                return _jointBounds;
            }
        }

        int IVertexAttributes.BufferCount => 1;

        Array IVertexAttributes.GetBuffer(int index)
        {
            Debug.Assert(index == 0);

            return _skin;
        }
    }
}
