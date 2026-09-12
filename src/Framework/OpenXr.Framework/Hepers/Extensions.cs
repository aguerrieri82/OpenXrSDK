using OpenXr.Framework.Layers;
using Silk.NET.OpenXR;
using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using XrMath;

namespace OpenXr.Framework
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToVector3(this in Vector3f value)
        {
            return new Vector3(value.X, value.Y, value.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToVector2(this in Vector2f value)
        {
            return new Vector2(value.X, value.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 ToVector4(this in Vector4f value)
        {
            return new Vector4(value.X, value.Y, value.Z, value.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Posef ToPoseF(this in Pose3 pose)
        {
            return new Posef
            {
                Orientation = pose.Orientation.ToQuaternionf(),
                Position = pose.Position.ToVector3f()
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3f ToVector3f(this in Vector3 vector)
        {
            return new Vector3f(vector.X, vector.Y, vector.Z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternionf ToQuaternionf(this in Quaternion quat)
        {
            return new Quaternionf(quat.X, quat.Y, quat.Z, quat.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Pose3 ToPose3(this in Posef pose)
        {
            return new Pose3
            {
                Orientation = new Quaternion(pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W),
                Position = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z)
            };
        }

        public static XrQuadLayer AddQuod(this XrLayerManager manager, GetQuadDelegate getQuad, RenderGeometryLayerDelegate render, Size2I size, int priority = XrLayerPriority.BaseGeometry)
        {
            var source = new XrTextureLayerSource(render, size);
            var layer = new XrQuadLayer(getQuad, source)
            {
                Priority = priority
            };

            manager.Add(layer);

            return layer;
        }

        public static XrCylinderLayer AddCylinder(this XrLayerManager manager, GetCylinderDelegate getCylinder, RenderGeometryLayerDelegate render, Size2I size, int priority = XrLayerPriority.BaseGeometry)
        {
            var source = new XrTextureLayerSource(render, size);
            var layer = new XrCylinderLayer(getCylinder, source)
            {
                Priority = priority
            };

            manager.Add(layer);

            return layer;
        }

        public static XrEquirect2Layer AddEquirect2(this XrLayerManager manager, GetSphericalSectionDelegate getSection, RenderGeometryLayerDelegate render, Size2I size, int priority = XrLayerPriority.BaseGeometry)
        {
            var source = new XrTextureLayerSource(render, size);
            var layer = new XrEquirect2Layer(getSection, source)
            {
                Priority = priority
            };

            manager.Add(layer);

            return layer;
        }

        public static XrQuadLayer[] AddStereoQuod(this XrLayerManager manager, GetQuadDelegate getQuad, RenderGeometryLayerDelegate render, Size2I size, int priority = XrLayerPriority.BaseGeometry)
        {
            var swapchain = new XrSwapchain(XrApp.Current!, 2);

            var source0 = new XrTextureLayerSource(render, size);
            var source1 = new XrTextureLayerSource(render, size);

            source0.ConfigureStereo(swapchain, 0);
            source1.ConfigureStereo(swapchain, 1);

            var eye0 = new XrQuadLayer(getQuad, source0)
            {
                Priority = priority
            };

            var eye1 = new XrQuadLayer(getQuad, source1)
            {
                Priority = priority
            };

            manager.Add(eye0);
            manager.Add(eye1);

            return [eye0, eye1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static XrProjectionLayer AddProjection(this XrLayerManager manager, RenderViewDelegate renderView, bool useDepthSwapchain)
        {
            var layer = new XrProjectionLayer(renderView, useDepthSwapchain);
            manager.List.Add(layer);
            return layer;
        }

        public static void ScheduleCancel<T>(this TaskCompletionSource<T> completionSource, TimeSpan time)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(time);
                if (!completionSource.Task.IsCompleted)
                    completionSource.SetCanceled();
            });
        }


        public static unsafe void DumpLayersJson(this XrApp self, ref CompositionLayerBaseHeader*[] layers, uint count)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var typeMap = new Dictionary<StructureType, Type>
            {
                [StructureType.CompositionLayerProjection] = typeof(CompositionLayerProjection),
                [StructureType.CompositionLayerProjectionView] = typeof(CompositionLayerProjectionView),
                [StructureType.CompositionLayerDepthInfoKhr] = typeof(CompositionLayerDepthInfoKHR),
                [StructureType.CompositionLayerQuad] = typeof(CompositionLayerQuad),
                [StructureType.CompositionLayerDepthTestFB] = typeof(CompositionLayerDepthTestFB),
                [StructureType.CompositionLayerImageLayoutFB] = typeof(CompositionLayerImageLayoutFB),

                [StructureType.CompositionLayerCylinderKhr] = typeof(CompositionLayerCylinderKHR),
                [StructureType.CompositionLayerCubeKhr] = typeof(CompositionLayerCubeKHR),
                [StructureType.CompositionLayerEquirectKhr] = typeof(CompositionLayerEquirectKHR),
                [StructureType.CompositionLayerEquirect2Khr] = typeof(CompositionLayerEquirect2KHR),
            };

            var n = Math.Min(count, (uint)layers.Length);
            var result = new Dictionary<string, object?>
            {
                ["count"] = count,
                ["arrayLength"] = layers.Length,
                ["dumpedCount"] = n
            };

            var outLayers = new List<object?>();

            for (uint i = 0; i < n; i++)
            {
                var layer = layers[i];

                if (layer == null)
                {
                    outLayers.Add(null);
                    continue;
                }

                var type = layer->Type;
                var actualType = typeMap.TryGetValue(type, out var t)
                    ? t
                    : typeof(CompositionLayerBaseHeader);

                outLayers.Add(DumpStruct(actualType, layer));
            }

            result["layers"] = outLayers;

            Debug.WriteLine(JsonSerializer.Serialize(result, options));

            object? DumpStruct(Type type, void* ptr)
            {
                if (ptr == null)
                    return null;

                var obj = new Dictionary<string, object?>
                {
                    ["$ptr"] = $"0x{(nint)ptr:X}",
                    ["$type"] = type.FullName
                };

                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var offset = (int)Marshal.OffsetOf(type, field.Name);
                    var fieldPtr = (byte*)ptr + offset;

                    obj[field.Name] = DumpField(type, ptr, field, fieldPtr);
                }

                return obj;
            }

            object? DumpField(Type ownerType, void* ownerPtr, FieldInfo field, void* fieldPtr)
            {
                var ft = field.FieldType;

                if (ft.IsPointer)
                {
                    var p = *(void**)fieldPtr;

                    if (p == null)
                        return null;

                    if (field.Name == "Next")
                        return DumpNextChain((BaseInStructure*)p);

                    if (ownerType == typeof(CompositionLayerProjection) &&
                        field.Name == "Views" &&
                        ft.GetElementType() == typeof(CompositionLayerProjectionView))
                    {
                        var viewCountField = ownerType.GetField("ViewCount", BindingFlags.Public | BindingFlags.Instance)!;
                        var viewCountOffset = (int)Marshal.OffsetOf(ownerType, viewCountField.Name);
                        var viewCount = *(uint*)((byte*)ownerPtr + viewCountOffset);

                        var views = new List<object?>();

                        var viewSize = Marshal.SizeOf<CompositionLayerProjectionView>();

                        for (uint i = 0; i < viewCount; i++)
                        {
                            var viewPtr = (byte*)p + i * viewSize;
                            views.Add(DumpStruct(typeof(CompositionLayerProjectionView), viewPtr));
                        }

                        return views;
                    }

                    return $"0x{(nint)p:X}";
                }

                if (ft.IsEnum)
                    return DumpEnum(ft, fieldPtr);

                if (ft == typeof(byte)) return *(byte*)fieldPtr;
                if (ft == typeof(sbyte)) return *(sbyte*)fieldPtr;
                if (ft == typeof(short)) return *(short*)fieldPtr;
                if (ft == typeof(ushort)) return *(ushort*)fieldPtr;
                if (ft == typeof(int)) return *(int*)fieldPtr;
                if (ft == typeof(uint)) return *(uint*)fieldPtr;
                if (ft == typeof(long)) return *(long*)fieldPtr;
                if (ft == typeof(ulong)) return *(ulong*)fieldPtr;
                if (ft == typeof(float)) return *(float*)fieldPtr;
                if (ft == typeof(double)) return *(double*)fieldPtr;
                if (ft == typeof(bool)) return *(bool*)fieldPtr;

                if (ft == typeof(nint) || ft == typeof(IntPtr))
                    return $"0x{(*(nint*)fieldPtr):X}";

                if (ft == typeof(nuint) || ft == typeof(UIntPtr))
                    return $"0x{(*(nuint*)fieldPtr):X}";

                if (ft.IsValueType)
                    return DumpStruct(ft, fieldPtr);

                return $"<unsupported {ft.FullName}>";
            }

            object DumpEnum(Type enumType, void* ptr)
            {
                var raw = ReadEnumRaw(enumType, ptr);

                return new Dictionary<string, object?>
                {
                    ["value"] = raw,
                    ["name"] = Enum.ToObject(enumType, raw).ToString()
                };
            }

            long ReadEnumRaw(Type enumType, void* ptr)
            {
                var u = Enum.GetUnderlyingType(enumType);

                if (u == typeof(byte)) return *(byte*)ptr;
                if (u == typeof(sbyte)) return *(sbyte*)ptr;
                if (u == typeof(short)) return *(short*)ptr;
                if (u == typeof(ushort)) return *(ushort*)ptr;
                if (u == typeof(int)) return *(int*)ptr;
                if (u == typeof(uint)) return *(uint*)ptr;
                if (u == typeof(long)) return *(long*)ptr;

                return unchecked((long)*(ulong*)ptr);
            }

            object DumpNextChain(BaseInStructure* next)
            {
                var chain = new List<object?>();

                var type = next->Type;
                var actualType = typeMap.TryGetValue(type, out var t)
                    ? t
                    : typeof(BaseInStructure);

                chain.Add(DumpStruct(actualType, next));

                return chain;
            }
        }
    }
}
