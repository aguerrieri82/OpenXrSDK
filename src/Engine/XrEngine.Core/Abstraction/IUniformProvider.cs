using System.Numerics;
using XrMath;

namespace XrEngine
{
    public interface IUniformProvider
    {

        void SetUniform(string name, int value, bool optional = false);

        void SetUniform(string name, uint value, bool optional = false);

        void SetUniform(string name, Matrix4x4 value, bool optional = false);

        void SetUniform(string name, Matrix3x3 value, bool optional = false);

        void SetUniform(string name, float value, bool optional = false);

        void SetUniform(string name, Vector2I value, bool optional = false);

        void SetUniform(string name, Vector3 value, bool optional = false);

        void SetUniform(string name, Vector2 value, bool optional = false);

        void SetUniform(string name, Color value, bool optional = false);

        void SetUniform(string name, Texture value, int slot = 0, bool optional = false);

        void LoadTexture(Texture value, int slot = 0);

        void SetUniform(string name, float[] value, bool optional = false);

        void SetUniform(string name, int[] value, bool optional = false);

        void SetUniform(string name, Vector2[] value, bool optional = false);

        void SetUniform(string name, IBuffer value, int slot = 0, bool optional = false);

        void SetLineSize(float size);

    }
}
