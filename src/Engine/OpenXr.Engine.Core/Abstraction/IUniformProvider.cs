using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine
{
    public interface IUniformProvider
    {
        void SetUniform(string name, int value);

        void SetUniform(string name, Matrix4x4 value);

        void SetUniform(string name, float value);

        void SetUniform(string name, Vector3 value);
    }
}
