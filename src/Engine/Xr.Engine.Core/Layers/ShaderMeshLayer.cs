namespace Xr.Engine
{

    public class ShaderMeshLayer : BaseAutoLayer<TriangleMesh>
    {
        readonly Shader _shader;

        public ShaderMeshLayer(Shader shader)
        {
            _shader = shader;
        }

        protected override bool BelongsToLayer(TriangleMesh obj)
        {
            return obj.IsVisible &&
                obj.Materials.
                    OfType<ShaderMaterial>().
                    Any(a => a.Shader == _shader);
        }

        public Shader? Shader => _shader;
    }

    public class ShaderMeshLayerBuilder : IObjectChangeListener
    {
        readonly Dictionary<Shader, ShaderMeshLayer> _layers = [];

        private ShaderMeshLayerBuilder()
        {
        }

        public void NotifyChanged(Object3D obj, ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.SceneAdd, ObjectChangeType.Render) && obj is TriangleMesh mesh)
            {
                foreach (var material in mesh.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if (!_layers.ContainsKey(material.Shader))
                    {
                        var layer = new ShaderMeshLayer(material.Shader);
                        _layers[material.Shader] = layer;
                        obj.Scene!.Layers.Add(layer);
                        layer.NotifyChanged(obj, change);
                    }
                }
            }
        }


        public static readonly ShaderMeshLayerBuilder Instance = new();
    }

}
