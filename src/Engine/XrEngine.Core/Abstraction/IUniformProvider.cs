using System.Numerics;
using XrMath;

namespace XrEngine
{
    public enum BufferUsage
    {
        Default,
        Uniforms,
        SSbo,
        SharedSsbo
    }

    public interface IUniformProvider
    {
        void LoadTexture(Texture value, int slot, bool forceBinding = false);

        void LoadImage(Texture2D copyDepthImage, int slot, BufferAccessMode accessMode = BufferAccessMode.ReadWrite);

        void SetUniform(string name, bool value, bool optional = false);

        void SetUniform(string name, int value, bool optional = false, bool force = false);

        void SetUniform(string name, uint value, bool optional = false);

        void SetUniform(string name, Matrix4x4 value, bool optional = false);

        void SetUniform(string name, Matrix3x3 value, bool optional = false);

        void SetUniform(string name, float value, bool optional = false);

        void SetUniform(string name, Vector2I value, bool optional = false);

        void SetUniform(string name, Vector3I value, bool optional = false);

        void SetUniform(string name, Vector4 value, bool optional = false);

        void SetUniform(string name, Vector3 value, bool optional = false);

        void SetUniform(string name, Vector2 value, bool optional = false);

        void SetUniform(string name, Color value, bool optional = false);

        void SetUniform(string name, Texture value, int slot = 0, bool optional = false);

        void SetUniform(string name, float[] value, bool optional = false);

        void SetUniform(string name, int[] value, bool optional = false);

        void SetUniform(string name, Vector2[] value, bool optional = false);

        void SetUniform(string name, Vector3[] value, bool optional = false);

        void LoadBuffer<T>(ISimpleBuffer<T> value, int slot = 0, BufferUsage usage = BufferUsage.Default)
            where T : unmanaged;

        void LoadSampler(TextureSampler value, int slot = 0);

        void SetLineSize(float size);

    }
}
