#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif

using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.OpenGL
{
    public struct GlVertexAttribute
    {
        public uint Location;

        public uint Count;

        public VertexAttribPointerType Type;

        public uint Offset;

        public string? Name;

        public VertexComponent Component;
    }

    public class GlVertexLayout
    {
        public static GlVertexLayout FromType<T>(VertexComponent activeComponents) where T : unmanaged
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance);

            var infos = fields.Select(a => new
            {
                Type = a.FieldType,
                Ref = a.GetCustomAttribute<ShaderRefAttribute>()
            })
            .Where(a => a.Ref != null)
            .OrderBy(a => a.Ref!.Location)
            .ToArray();

            var attrbs = new List<GlVertexAttribute>();

            var res = new GlVertexLayout
            {
                Attributes = new GlVertexAttribute[infos.Length]
            };

            uint curOfs = 0;
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];


                var item = new GlVertexAttribute();

                item.Name = info.Ref!.Name;
                item.Location = info.Ref.Location;
                item.Component = info.Ref.Component;

                if (info.Type == typeof(Vector3))
                {
                    item.Type = VertexAttribPointerType.Float;
                    item.Count = 3;
                }
                else if (info.Type == typeof(Quaternion))
                {
                    item.Type = VertexAttribPointerType.Float;
                    item.Count = 4;
                }
                else if (info.Type == typeof(Vector4))
                {
                    item.Type = VertexAttribPointerType.Float;
                    item.Count = 4;
                }
                else if (info.Type == typeof(Vector2))
                {
                    item.Type = VertexAttribPointerType.Float;
                    item.Count = 2;
                }
                else if (info.Type == typeof(float))
                {
                    item.Type = VertexAttribPointerType.Float;
                    item.Count = 1;
                }
                else if (info.Type == typeof(Color))
                {
                    item.Type = VertexAttribPointerType.Float;
                    item.Count = 4;
                }
                else
                    throw new NotImplementedException();

                item.Offset = curOfs;
                curOfs += (uint)Marshal.SizeOf(info.Type);

                if ((info.Ref!.Component & activeComponents) != 0)
                    attrbs.Add(item);

            }

            res.Size = (uint)Marshal.SizeOf<T>();
            res.Attributes = attrbs.ToArray();  

            return res;
        }

        public GlVertexAttribute[]? Attributes { get; set; }

        public uint Size { get; set; }

    }
}
