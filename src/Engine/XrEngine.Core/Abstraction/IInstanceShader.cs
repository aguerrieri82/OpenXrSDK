namespace XrEngine
{
    public interface IInstanceShader
    {
        bool NeedUpdate(Object3D model, long curVersion);

        unsafe long Update(UpdateShaderContext ctx, byte* dstData, Object3D model, int drawId);

        public Type InstanceBufferType { get; }
    }
}
