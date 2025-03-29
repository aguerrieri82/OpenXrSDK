using System;
using System.Collections.Generic;
using System.Text;

namespace XrEngine.OpenGL
{
    [Flags]
    public enum GlLayerType
    {
        Unknown = 0,
        Color = 0x1,
        Opaque = 0x2 | Color,
        Blend = 0x4 | Color,
        CastShadow = 0x8,
        FullReflection = 0x10,
        Custom = 0x20,
        Light = 0x40,
        Volume = 0x80 | Color,
    }

    public interface IGlLayer : IDisposable
    {
        void Rebuild();

        void Prepare(RenderContext ctx);

        string? Name { get; }

        bool NeedUpdate { get; }

        GlLayerType Type { get; }

        ILayer3D? SceneLayer { get; }

        Scene3D Scene { get; }

        long Version { get; }

        bool IsEmpty { get; }
    }
}
