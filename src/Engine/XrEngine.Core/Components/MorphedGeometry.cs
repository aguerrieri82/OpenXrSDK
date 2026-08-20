using Common.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace XrEngine
{
    public struct MorphComponent
    {
        public VertexComponent Component;

        public Vector3[] Values;
    }

    public struct MorphTarget
    {
        public MorphComponent[] Components;
    }

    public enum MorphStorageType
    {
        Auto,
        Attributes,
        Ssbo,
        Texture
    }

    public class MorphedGeometry : BaseComponent<Geometry3D>, IGeometryComponent, IVertexAttributes
    {

        public Texture2D CreateTexture()
        {
            var data = UpdateTextureData();

            return new Texture2D
            {
                Data = [data],
                Width = data.Width,
                Height = data.Height,
                Format = TextureFormat.RgbFloat16,
                MagFilter = ScaleFilter.Nearest,
                MinFilter = ScaleFilter.Nearest,
                MipLevelCount = 0
            };
        }

        public unsafe void UpdateBuffer(IBuffer<Vector3> buffer)
        {
            Debug.Assert(Targets != null);

            var count = Targets.Sum(t => t.Components.Sum(c => c.Values.Length));

            buffer.Allocate((uint)(count * sizeof(Vector3)));

            using var data = buffer.Lock(BufferAccessMode.Replace);

            var pDst = (Vector3*)data.Data;

            foreach (var target in Targets)
            {
                foreach (var component in target.Components)
                {
                    fixed (Vector3* pSrc = component.Values)
                    {
                        var size = component.Values.Length * sizeof(Vector3);

                        Buffer.MemoryCopy(
                            pSrc,
                            pDst,
                            size,
                            size);

                        pDst += component.Values.Length;
                    }
                }
            }
        }

        public unsafe TextureData UpdateTextureData()
        {
            Debug.Assert(Targets != null);

            var maxSize = EngineApp.Current.Renderer.Features.MaxTextureSize;

            var vertexCount = (uint)Targets[0].Components[0].Values.Length;
            var width = Math.Min(vertexCount, maxSize.Width);
            var rowsPerComponent = (vertexCount + width - 1) / width;

            var componentCount = Targets.Sum(t => t.Components.Length);
            var height = (uint)(componentCount * rowsPerComponent);

            if (height > maxSize.Height)
                throw new NotSupportedException("Morph texture exceeds maximum texture size.");

            var rowSize = (uint)(width * sizeof(Vector3));
            var buffer = MemoryBuffer.Create<byte>(rowSize * height);

            using var data = buffer.MemoryLock();

            var pDst = data.Data;
            var row = 0u;

            foreach (var target in Targets)
            {
                foreach (var component in target.Components)
                {
                    Debug.Assert(component.Values.Length == vertexCount);

                    fixed (Vector3* pSrc = component.Values)
                    {
                        Buffer.MemoryCopy(
                            pSrc,
                            pDst + row * rowSize,
                            rowsPerComponent * rowSize,
                            vertexCount * sizeof(Vector3));
                    }

                    row += rowsPerComponent;
                }
            }

            return new TextureData
            {
                Content = buffer,
                Width = width,
                Height = height,
                Format = TextureFormat.RgbFloat32
            };
        }


        int IVertexAttributes.BufferCount
        {
            get
            {
                if (StorageType != MorphStorageType.Attributes)
                    return 0;

                Debug.Assert(Targets != null);

                return Targets.Sum(a => a.Components.Length);
            }
        }

        VertexAttributesBuffer IVertexAttributes.GetBuffer(int index)
        {
            Debug.Assert(Targets != null);

            int i = 0;
            foreach (var target in Targets)
            {
                foreach (var  component in target.Components)
                {
                    if (index == i)
                        return new VertexAttributesBuffer
                        {
                            Data = component.Values,
                            BaseLocation = (uint)(AttributeSlots.MorphBase + i),
                            Component = component.Component,
                            ElementType = typeof(Vector3)
                        };
                    i++;
                }
            }

            throw new InvalidOperationException();
        }

        public MorphTarget[]? Targets { get; set; }

        public MorphStorageType StorageType { get; set; }
    }
}
