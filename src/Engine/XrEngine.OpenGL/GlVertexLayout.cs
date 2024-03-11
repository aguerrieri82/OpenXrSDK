﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;

#endif

using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

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
            .Where(a => a.Ref != null && (a.Ref.Component & activeComponents) == a.Ref.Component)
            .OrderBy(a => a.Ref!.Location)
            .ToArray();

            var res = new GlVertexLayout();

            res.Attributes = new GlVertexAttribute[infos.Length];

            uint curOfs = 0;
            for (var i = 0; i < infos.Length; i++)
            {
                var info = infos[i];

                ref var item = ref res.Attributes[i];

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
                else
                    throw new NotImplementedException();

                item.Offset = curOfs;
                curOfs += (uint)Marshal.SizeOf(info.Type);
            }

            res.Size = (uint)Marshal.SizeOf<T>();

            return res;
        }

        public GlVertexAttribute[]? Attributes { get; set; }

        public uint Size { get; set; }

    }
}
