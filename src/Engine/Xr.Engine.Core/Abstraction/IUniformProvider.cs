using System.Numerics;

namespace OpenXr.Engine
{
    public interface IUniformProvider
    {
        void SetUniform(string name, int value);

        void SetUniform(string name, Matrix4x4 value);

        void SetUniform(string name, float value);

        void SetUniform(string name, Vector2I value);

        void SetUniform(string name, Vector3 value, bool optional = false);

        void SetUniform(string name, Color value);

        void SetUniform(string name, Texture2D value, int slot = 0);

        void SetLineSize(float size);

        void EnableFeature(string name, bool enabled);  
    }
}
