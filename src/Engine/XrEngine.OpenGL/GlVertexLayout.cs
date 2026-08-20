#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Common.Interop;
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

        public bool IsNormalized;

        public bool IsIntegerStore;
    }

    public class GlVertexLayout
    {

        public static GlVertexLayout FromType(Type baseType, VertexComponent activeComponents, uint baseLocation = 0)
        {
            if (baseType == typeof(Vector2))
            {
                return new GlVertexLayout
                {
                    Attributes = [new GlVertexAttribute
                    {
                        Name = "aVector2",
                        Location = baseLocation,
                        Type = VertexAttribPointerType.Float,
                        Count = 2,
                        Offset = 0,
                        Component = VertexComponent.None
                    }],
                    Size = (uint)MarshalCache.SizeOf(baseType)
                };
            }
            if (baseType == typeof(Vector3))
            {
                return new GlVertexLayout
                {
                    Attributes = [new GlVertexAttribute
                    {
                        Name = "aVector3",
                        Location = baseLocation,
                        Type = VertexAttribPointerType.Float,
                        Count = 3,
                        Offset = 0,
                        Component = VertexComponent.None
                    }],
                    Size = (uint)MarshalCache.SizeOf(baseType)
                };
            }

            var attrbs = new List<GlVertexAttribute>();

            void Collect(Type type, uint baseOffset)
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    var fieldOffset = baseOffset + (uint)Marshal.OffsetOf(type, field.Name).ToInt64();
                    var shaderRef = field.GetCustomAttribute<ShaderRefAttribute>();

                    if (shaderRef != null)
                    {
                        var item = new GlVertexAttribute
                        {
                            Name = shaderRef.Name,
                            Location = baseLocation + shaderRef.Location,
                            Component = shaderRef.Component,
                            Offset = fieldOffset,
                            IsIntegerStore = shaderRef.IsIntegerStore,
                            IsNormalized = shaderRef.IsNormalized
                        };

                        if (field.FieldType == typeof(Vector3))
                        {
                            item.Type = VertexAttribPointerType.Float;
                            item.Count = 3;
                        }
                        else if (field.FieldType == typeof(Quaternion))
                        {
                            item.Type = VertexAttribPointerType.Float;
                            item.Count = 4;
                        }
                        else if (field.FieldType == typeof(Vector4))
                        {
                            item.Type = VertexAttribPointerType.Float;
                            item.Count = 4;
                        }
                        else if (field.FieldType == typeof(Vector2))
                        {
                            item.Type = VertexAttribPointerType.Float;
                            item.Count = 2;
                        }
                        else if (field.FieldType == typeof(float))
                        {
                            item.Type = VertexAttribPointerType.Float;
                            item.Count = 1;
                        }
                        else if (field.FieldType == typeof(Color))
                        {
                            item.Type = VertexAttribPointerType.Float;
                            item.Count = 4;
                        }
                        else if (field.FieldType == typeof(Vector4I))
                        {
                            item.Type = VertexAttribPointerType.Int;
                            item.IsIntegerStore = true;
                            item.Count = 4;
                        }
                        else
                            throw new NotImplementedException($"Unsupported vertex attribute type '{field.FieldType}'.");

                        if ((shaderRef.Component & activeComponents) != 0)
                            attrbs.Add(item);

                        continue;
                    }

                    if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                        Collect(field.FieldType, fieldOffset);
                }
            }

            Collect(baseType, 0);

            return new GlVertexLayout
            {
                Size = (uint)MarshalCache.SizeOf(baseType),
                Attributes = attrbs.OrderBy(a => a.Location).ToArray()
            };
        }

        public GlVertexAttribute[]? Attributes { get; set; }

        public uint Size { get; set; }

    }
}
