namespace XrEngine.Components
{
    public class MeshMorph : BaseComponent<TriangleMesh>, IMorphedMesh
    {
        protected long _morphVersion = 0;
        protected float[] _weights = [];

        public void InvalidateWeights()
        {
            _morphVersion++;
        }

        public long MorphVersion => _morphVersion;

        public float[] Weights
        {
            get => _weights;
            set
            {
                _weights = value;
                InvalidateWeights();
            }
        }

    }
}
