namespace XrEngine
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

        public void NotifyChanged(Object3D sender, ObjectChange change)
        {
            if (change.IsAny(ObjectChangeType.SceneAdd, ObjectChangeType.Render) && sender is TriangleMesh mesh)
            {
                foreach (var material in mesh.Materials.OfType<ShaderMaterial>())
                {
                    if (material.Shader == null)
                        continue;

                    if (!_layers.ContainsKey(material.Shader))
                    {
                        var layer = new ShaderMeshLayer(material.Shader);
                        _layers[material.Shader] = layer;
                        sender.Scene!.Layers.Add(layer);
                        layer.NotifyChanged(sender, change);
                    }
                }
            }
        }


        public static readonly ShaderMeshLayerBuilder Instance = new();
    }

}
