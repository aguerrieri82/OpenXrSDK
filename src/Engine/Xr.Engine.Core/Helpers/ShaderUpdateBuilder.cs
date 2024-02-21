using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OpenXr.Engine
{
    public class ShaderUpdate
    {
        public IList<UpdateUniformAction>? Actions { get; set; }

        public IList<string>? Features { get; set; }
    }

    public struct ShaderUpdateBuilder : IUniformProvider, IFeatureList
    {
        readonly ShaderUpdate _result;

        public ShaderUpdateBuilder()
        {
            _result = new ShaderUpdate()
            {
                Features = new List<string>(),
                Actions = new List<UpdateUniformAction>()
            };
        }

        public void SetLineSize(float size)
        {
            throw new NotImplementedException();
        }

        public readonly void SetUniform(string name, int value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional)); 
        }

        public readonly void SetUniform(string name, Matrix4x4 value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void SetUniform(string name, float value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void SetUniform(string name, Vector2I value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void SetUniform(string name, Vector3 value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void SetUniform(string name, Color value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void SetUniform(string name, Texture2D value, int slot = 0, bool optional = false)
        {
            Log(name, slot);
            _result.Actions!.Add(up => up.SetUniform(name, value, slot, optional));
        }

        public readonly void SetUniform(string name, float[] value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void SetUniform(string name, int[] value, bool optional = false)
        {
            Log(name, value);
            _result.Actions!.Add(up => up.SetUniform(name, value, optional));
        }

        public readonly void AddFeature(string name)
        {
            _result.Features!.Add(name);
        }

        readonly void Log(string name, object value)
        {
            //Logs.Append(name).Append(" = ").Append(value).AppendLine();
        }

        public StringBuilder Logs { get; } = new StringBuilder();

        public readonly ShaderUpdate Result => _result;
    }
}
