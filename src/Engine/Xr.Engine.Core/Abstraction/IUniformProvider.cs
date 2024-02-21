using System.Numerics;

namespace OpenXr.Engine
{
    public interface IUniformProvider
    {
        void SetUniform(string name, int value, bool optional = false);

        void SetUniform(string name, Matrix4x4 value, bool optional = false);

        void SetUniform(string name, float value, bool optional = false);

        void SetUniform(string name, Vector2I value, bool optional = false);

        void SetUniform(string name, Vector3 value, bool optional = false);

        void SetUniform(string name, Color value, bool optional = false);

        void SetUniform(string name, Texture2D value, int slot = 0, bool optional = false);

        void SetUniform(string name, float[] value, bool optional = false);

        void SetUniform(string name, int[] value, bool optional = false);

        void SetUniform(string name, IBuffer value, bool optional = false);

        void SetLineSize(float size);

    }
}
