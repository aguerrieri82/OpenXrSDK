
namespace XrEngine
{
    public interface ITextureLayout
    {
        void Update(UpdateShaderContext ctx, IUniformProvider up, Texture source, uint slot = 0);
    }
}
