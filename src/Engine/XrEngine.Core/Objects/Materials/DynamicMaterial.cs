namespace XrEngine
{
    public class DynamicMaterial : ShaderMaterial
    {
        static readonly Dictionary<string, Shader> _shaders = [];

        readonly Action<ShaderUpdateBuilder>? _update;

        public DynamicMaterial(string vertSource, string fragSource, Action<ShaderUpdateBuilder>? update = null)
        {
            var key = $"{vertSource}|{fragSource}";

            if (!_shaders.TryGetValue(key, out _shader))
            {
                _shader = new Shader()
                {
                    VertexSourceName = vertSource,
                    FragmentSourceName = fragSource,
                    Resolver = a => Embedded.GetString(a),
                    IsLit = false
                };
                _shaders[key] = _shader;
            }

            _update = update;
        }

        protected override void UpdateShaderMaterial(ShaderUpdateBuilder bld)
        {
            _update?.Invoke(bld);
        }
    }
}
