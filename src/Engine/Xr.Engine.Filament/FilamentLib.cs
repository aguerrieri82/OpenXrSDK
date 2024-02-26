﻿using SkiaSharp;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Xr.Engine.Filament.FilamentLib;

namespace Xr.Engine.Filament
{
    internal unsafe static class FilamentLib
    {
        public enum FlLightType : byte
        {
            Sun,           
	        Directional,   
	        Point,       
	        FocusedSpot,  
	        Spot,
        }

        public enum FlBackend : byte
        {
            Auto ,
	        OpenGL,
	        Vulkan
        }

        public enum FlTextureInternalFormat
        {
            RGB8 = 17,
            SRGB8 = 18,
            RGBA8 = 30,
            SRGB8_A8 = 31
        }

        public enum FlPixelFormat : byte
        {
            RGB = 4,
            RGB_INTEGER = 5,
            RGBA = 6,
            RGBA_INTEGER = 7
        }

        public enum FlPixelType : byte
        {
            UBYTE = 0
        }


        public enum VertexAttributeType
        {
            Position,
	        Normal,
	        Tangent,
	        Color,
	        UV0,
	        UV1
        }

        public enum FlBlendingMode : byte
        {
            OPAQUE,
            TRANSPARENT,
            ADD,
            MASKED,
            FADE,
            MULTIPLY,
            SCREEN,
        }

        public enum FlSpecularAO : byte
        {
            None,
            Simple,
            BentNormals
        }

        public enum FlAntiAliasing : byte
        {
            NONE, 
            FXAA    
        };

        public enum FlBlendMode : byte
        {
            OPAQUE,
            TRANSLUCENT
        }

        public enum FlShadowType : byte
        {
            PCF,     
            VSM,       
            DPCF,    
            PCSS  
        }

        public enum FlQualityLevel : byte
        {
            LOW,
            MEDIUM,
            HIGH,
            ULTRA
        }

        public struct RenderQuality
        {
            public FlQualityLevel HdrColorBuffer;
        }

        public struct InitializeOptions
        {
            public FlBackend Driver;
            public IntPtr WindowHandle;
            public IntPtr Context;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string MaterialCachePath;
        }

        public struct ViewOptions
        {
            public FlBlendMode blendMode;
            public FlAntiAliasing antiAliasing;
            [MarshalAs(UnmanagedType.U1)]
            public bool frustumCullingEnabled;
            [MarshalAs(UnmanagedType.U1)]
            public bool postProcessingEnabled;
            public RenderQuality renderQuality;
            public uint sampleCount;
            [MarshalAs(UnmanagedType.U1)]
            public bool screenSpaceRefractionEnabled;
            [MarshalAs(UnmanagedType.U1)]
            public bool shadowingEnabled;
            [MarshalAs(UnmanagedType.U1)]
            public bool stencilBufferEnabled;
            public FlShadowType shadowType;
        }


        public struct RenderTargetOptions
        {
            public IntPtr TextureId;
            public uint Width;
            public uint Height;
            public uint SampleCount;
        }

        public struct RenderTarget
        {
            public uint ViewId;
            public int RenderTargetId;
            public CameraInfo Camera;
            public Rect2I Viewport;
        }

        public struct CameraInfo
        {
            public Matrix4x4 Transform;
            public Matrix4x4 Projection;
            public float Far;
            public float Near;
        }

        public struct SunLight
        {
            public float AngularRadius;
            public float HaloFalloff;
            public float HaloSize;
        }
    
        public struct LightInfo
        {
            public FlLightType Type;
            public float Intensity;
            public float FalloffRadius;
            public Color Color;
            public Vector3 Direction;
            public Vector3 Position;
            [MarshalAs(UnmanagedType.U1)]
            public bool CastShadows;
            [MarshalAs(UnmanagedType.U1)]
            public bool CastLight;
            public SunLight Sun;
        }

        public struct MeshInfo
        {
            public uint GeometryId;
            public uint MaterialId;
            [MarshalAs(UnmanagedType.U1)]
            public bool Culling;
            [MarshalAs(UnmanagedType.U1)]
            public bool CastShadows;
            [MarshalAs(UnmanagedType.U1)]
            public bool ReceiveShadows;
            [MarshalAs(UnmanagedType.U1)]
            public bool Fog;
        }

        public struct VertexAttribute
        {
            public VertexAttributeType Type;
            public uint Offset;
            public uint Size;
        }

        public struct VertexLayout
        {
            public uint SizeByte;
	        public VertexAttribute* Attributes;
            public uint AttributeCount;
        }

        public struct GeometryInfo
        {
            public uint* Indices;
            public long IndicesCount;
            public byte* Vertices;
            public long VerticesCount;
            public VertexLayout layout;
            public Bounds3 Bounds;
        }

        public struct ImageData
        {
            public FlPixelFormat Format;
            public FlPixelType Type;    
            public IntPtr Data;
            public uint DataSize;

        };

        public struct TextureInfo
        {
            public uint Width;
            public uint Height;
            public FlTextureInternalFormat InternalFormat;
            public uint Levels;
            public ImageData Data;
        };

        public struct MaterialInfo
        {
            public TextureInfo NormalMap;
            public TextureInfo AoMap;
            public TextureInfo MetallicRoughnessMap;
            public TextureInfo BaseColorMap;
            public Color Color;
            [MarshalAs(UnmanagedType.U1)]
            public bool ClearCoat;
            [MarshalAs(UnmanagedType.U1)]
            public bool Anisotropy;
            [MarshalAs(UnmanagedType.U1)]
            public bool MultiBounceAO;
            [MarshalAs(UnmanagedType.U1)]
            public bool SpecularAntiAliasing; //true;
            [MarshalAs(UnmanagedType.U1)]
            public bool ClearCoatIorChange;
            [MarshalAs(UnmanagedType.U1)]
            public bool DoubleSided;
            [MarshalAs(UnmanagedType.U1)]
            public bool ScreenSpaceReflection; //True
            public FlBlendingMode Blending;
            public FlSpecularAO SpecularAO;
        };


        [DllImport("filament-native")]
        public static extern IntPtr Initialize(ref InitializeOptions options);

        [DllImport("filament-native")]
        public static extern uint AddView(IntPtr app, ref ViewOptions options);

        [DllImport("filament-native")]
        public static extern int AddRenderTarget(IntPtr app, ref RenderTargetOptions options);

        [DllImport("filament-native")]
        public static extern void Render(IntPtr app, RenderTarget* targets, uint count);

        [DllImport("filament-native")]
        public static extern void AddLight(IntPtr app, uint id, ref LightInfo info);

        [DllImport("filament-native")]
        public static extern void AddGeometry(IntPtr app, uint id, ref GeometryInfo info);

        [DllImport("filament-native")]
        public static extern void AddMesh(IntPtr app, uint id, ref MeshInfo info);

        [DllImport("filament-native")]
        public static extern void AddGroup(IntPtr app, uint id);

        [DllImport("filament-native")]
        public static extern void SetWorldMatrix(IntPtr app, uint meshId, ref Matrix4x4 matrix);

        [DllImport("filament-native")]
        public static extern void SetObjTransform(IntPtr app, uint id, Matrix4x4 matrix);

        [DllImport("filament-native")]
        public static extern void SetObjParent(IntPtr app, uint id, uint parentId);

        [DllImport("filament-native")]
        public static extern void AddMaterial(IntPtr app, uint id, ref MaterialInfo material);
    }
}
