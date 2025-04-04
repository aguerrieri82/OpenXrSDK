﻿using System.Numerics;
using System.Runtime.InteropServices;
using XrMath;

namespace XrEngine.Filament
{
    public unsafe static class FilamentLib
    {
        public enum PrimitiveType : byte
        {
            POINTS = 0,    //!< points
            LINES = 1,    //!< lines
            LINE_STRIP = 3,    //!< line strip
            TRIANGLES = 4,    //!< triangles
            TRIANGLE_STRIP = 5     //!< triangle strip
        };


        public enum ReleaseContextMode
        {
            NotRelease = 0,
            ReleaseOnExecute = 1,
            ReleaseAndSuspend = 2
        };


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
            Auto,
            OpenGL,
            Vulkan
        }

        public enum FlTextureInternalFormat
        {
            // 8-bits per element
            R8, R8_SNORM, R8UI, R8I, STENCIL8,

            // 16-bits per element
            R16F, R16UI, R16I,
            RG8, RG8_SNORM, RG8UI, RG8I,
            RGB565,
            RGB9_E5, // 9995 is actually 32 bpp but it's here for historical reasons.
            RGB5_A1,
            RGBA4,
            DEPTH16,

            // 24-bits per element
            RGB8, SRGB8, RGB8_SNORM, RGB8UI, RGB8I,
            DEPTH24,

            // 32-bits per element
            R32F, R32UI, R32I,
            RG16F, RG16UI, RG16I,
            R11F_G11F_B10F,
            RGBA8, SRGB8_A8, RGBA8_SNORM,
            UNUSED, // used to be rgbm
            RGB10_A2, RGBA8UI, RGBA8I,
            DEPTH32F, DEPTH24_STENCIL8, DEPTH32F_STENCIL8,

            // 48-bits per element
            RGB16F, RGB16UI, RGB16I,

            // 64-bits per element
            RG32F, RG32UI, RG32I,
            RGBA16F, RGBA16UI, RGBA16I,

            // 96-bits per element
            RGB32F, RGB32UI, RGB32I,

            // 128-bits per element
            RGBA32F, RGBA32UI, RGBA32I,

            // compressed formats

            // Mandatory in GLES 3.0 and GL 4.3
            EAC_R11, EAC_R11_SIGNED, EAC_RG11, EAC_RG11_SIGNED,
            ETC2_RGB8, ETC2_SRGB8,
            ETC2_RGB8_A1, ETC2_SRGB8_A1,
            ETC2_EAC_RGBA8, ETC2_EAC_SRGBA8,

            // Available everywhere except Android/iOS
            DXT1_RGB, DXT1_RGBA, DXT3_RGBA, DXT5_RGBA,
            DXT1_SRGB, DXT1_SRGBA, DXT3_SRGBA, DXT5_SRGBA,

            // ASTC formats are available with a GLES extension
            RGBA_ASTC_4x4,
            RGBA_ASTC_5x4,
            RGBA_ASTC_5x5,
            RGBA_ASTC_6x5,
            RGBA_ASTC_6x6,
            RGBA_ASTC_8x5,
            RGBA_ASTC_8x6,
            RGBA_ASTC_8x8,
            RGBA_ASTC_10x5,
            RGBA_ASTC_10x6,
            RGBA_ASTC_10x8,
            RGBA_ASTC_10x10,
            RGBA_ASTC_12x10,
            RGBA_ASTC_12x12,
            SRGB8_ALPHA8_ASTC_4x4,
            SRGB8_ALPHA8_ASTC_5x4,
            SRGB8_ALPHA8_ASTC_5x5,
            SRGB8_ALPHA8_ASTC_6x5,
            SRGB8_ALPHA8_ASTC_6x6,
            SRGB8_ALPHA8_ASTC_8x5,
            SRGB8_ALPHA8_ASTC_8x6,
            SRGB8_ALPHA8_ASTC_8x8,
            SRGB8_ALPHA8_ASTC_10x5,
            SRGB8_ALPHA8_ASTC_10x6,
            SRGB8_ALPHA8_ASTC_10x8,
            SRGB8_ALPHA8_ASTC_10x10,
            SRGB8_ALPHA8_ASTC_12x10,
            SRGB8_ALPHA8_ASTC_12x12,

            // RGTC formats available with a GLES extension
            RED_RGTC1,              // BC4 unsigned
            SIGNED_RED_RGTC1,       // BC4 signed
            RED_GREEN_RGTC2,        // BC5 unsigned
            SIGNED_RED_GREEN_RGTC2, // BC5 signed

            // BPTC formats available with a GLES extension
            RGB_BPTC_SIGNED_FLOAT,  // BC6H signed
            RGB_BPTC_UNSIGNED_FLOAT,// BC6H unsigned
            RGBA_BPTC_UNORM,        // BC7
            SRGB_ALPHA_BPTC_UNORM,  // BC7 sRGB
        }

        public enum FlPixelFormat : byte
        {
            R,                  //!< One Red channel, float
            R_INTEGER,          //!< One Red channel, integer
            RG,                 //!< Two Red and Green channels, float
            RG_INTEGER,         //!< Two Red and Green channels, integer
            RGB,                //!< Three Red, Green and Blue channels, float
            RGB_INTEGER,        //!< Three Red, Green and Blue channels, integer
            RGBA,               //!< Four Red, Green, Blue and Alpha channels, float
            RGBA_INTEGER,       //!< Four Red, Green, Blue and Alpha channels, integer
            UNUSED,             // used to be rgbm
            DEPTH_COMPONENT,    //!< Depth, 16-bit or 24-bits usually
            DEPTH_STENCIL,      //!< Two Depth (24-bits) + Stencil (8-bits) channels
            ALPHA               //! One Alpha channel, float
        }

        public enum FlPixelType : byte
        {
            UBYTE,                //!< unsigned byte
            BYTE,                 //!< signed byte
            USHORT,               //!< unsigned short (16-bit)
            SHORT,                //!< signed short (16-bit)
            UINT,                 //!< unsigned int (32-bit)
            INT,                  //!< signed int (32-bit)
            HALF,                 //!< half-float (16-bit float)
            FLOAT,                //!< float (32-bits float)
            COMPRESSED,           //!< compressed pixels, @see CompressedPixelDataType
            UINT_10F_11F_11F_REV, //!< three low precision floating-point numbers
            USHORT_565,           //!< unsigned int (16-bit), encodes 3 RGB channels
            UINT_2_10_10_10_REV,  //!< unsigned normalized 10 bits RGB, 2 bits alpha
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
            PCSS,
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


        public struct VulkanSharedContext
        {
            public nint Instance;
            public nint PhysicalDevice;
            public nint LogicalDevice;
            public uint GraphicsQueueFamilyIndex;
            public uint GraphicsQueueIndex;
            [MarshalAs(UnmanagedType.U1)]
            public bool DebugUtilsSupported;
            [MarshalAs(UnmanagedType.U1)]
            public bool DebugMarkersSupported;
            [MarshalAs(UnmanagedType.U1)]
            public bool MultiviewSupported;
        };

        public struct InitializeOptions
        {
            public FlBackend Driver;
            public IntPtr WindowHandle;
            public IntPtr Context;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string MaterialCachePath;
            [MarshalAs(UnmanagedType.U1)]
            public bool EnableStereo;
            [MarshalAs(UnmanagedType.U1)]
            public bool OneViewPerTarget;
            [MarshalAs(UnmanagedType.U1)]
            public bool UseSrgb;
        }

        public struct ViewOptions
        {
            public FlBlendMode BlendMode;
            public FlAntiAliasing AntiAliasing;
            [MarshalAs(UnmanagedType.U1)]
            public bool FrustumCullingEnabled;
            [MarshalAs(UnmanagedType.U1)]
            public bool PostProcessingEnabled;
            public RenderQuality RenderQuality;
            public uint SampleCount;
            [MarshalAs(UnmanagedType.U1)]
            public bool ScreenSpaceRefractionEnabled;
            [MarshalAs(UnmanagedType.U1)]
            public bool ShadowingEnabled;
            [MarshalAs(UnmanagedType.U1)]
            public bool StencilBufferEnabled;
            public FlShadowType ShadowType;

            public Rect2I Viewport;

            public int RenderTargetId;

        }


        public struct RenderTargetOptions
        {
            public IntPtr TextureId;
            public uint Width;
            public uint Height;
            public uint SampleCount;
            public FlTextureInternalFormat Format;
            public uint Depth;
        }

        public struct RenderTarget
        {
            public uint ViewId;
            public int RenderTargetId;
            public CameraInfo Camera;
            public Rect2I Viewport;
        }

        public struct CameraEyesInfo
        {
            public Matrix4x4 RelTransform;
            public Matrix4x4 Projection;
        }

        public struct CameraInfo
        {
            public Matrix4x4 Transform;
            public Matrix4x4 Projection;
            public float Far;
            public float Near;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsStereo;
            public CameraEyesInfo Eye1;
            public CameraEyesInfo Eye2;
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
            public Guid GeometryId;
            public Guid MaterialId;
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
            public PrimitiveType Primitive;
        }

        public struct ImageData
        {
            public FlPixelFormat Format;
            public FlPixelType Type;
            public IntPtr Data;
            public uint DataSize;
            [MarshalAs(UnmanagedType.U1)]
            public bool AutoFree;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsBgr;

        };

        public struct TextureInfo
        {
            public uint Width;
            public uint Height;
            public FlTextureInternalFormat InternalFormat;
            public uint Levels;
            public ImageData Data;
            public Guid TextureId;
        };

        public struct MaterialInfo
        {
            public TextureInfo NormalMap;
            public TextureInfo AoMap;
            public TextureInfo MetallicRoughnessMap;
            public TextureInfo BaseColorMap;
            public TextureInfo EmissiveMap;
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
            public float NormalScale;
            public float AoStrength;
            public float RoughnessFactor;
            public float MetallicFactor;
            public float EmissiveStrength;
            public Vector3 EmissiveFactor;
            public float AlphaCutoff;
            public float Reflectance;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsLit;
            [MarshalAs(UnmanagedType.U1)]
            public bool WriteDepth;
            [MarshalAs(UnmanagedType.U1)]
            public bool UseDepth;
            [MarshalAs(UnmanagedType.U1)]
            public bool WriteColor;
            [MarshalAs(UnmanagedType.U1)]
            public bool IsShadowOnly;
            public float LineWidth;
        };

        public struct ImageLightInfo
        {
            public TextureInfo Texture;
            public float Intensity;
            public float Rotation;
            [MarshalAs(UnmanagedType.U1)]
            public bool ShowSkybox;
        };


        public struct GraphicContextInfo
        {
            public struct WinGlContext
            {
                public IntPtr GlCTx;
                public IntPtr HDc;
            }

            public struct VulkanContext
            {
                public IntPtr Instance;
                public IntPtr LogicalDevice;
                public IntPtr PhysicalDevice;
                public uint QueueFamilyIndex;
                public uint QueueIndex;
            }

            public WinGlContext WinGl;

            public VulkanContext Vulkan;
        }

        public struct FilamentApp
        {
            public nint Handle;
        }


        [DllImport("filament-native")]
        public static extern FilamentApp Initialize(ref InitializeOptions options);

        [DllImport("filament-native")]
        public static extern uint AddView(FilamentApp app, ref ViewOptions options);

        [DllImport("filament-native")]
        public static extern void UpdateView(FilamentApp app, uint viewId, ref ViewOptions options);

        [DllImport("filament-native")]
        public static extern int AddRenderTarget(FilamentApp app, ref RenderTargetOptions options);

        [DllImport("filament-native")]
        public static extern void Render(FilamentApp app, RenderTarget* targets, uint count, bool wait);

        [DllImport("filament-native")]
        public static extern void AddLight(FilamentApp app, Guid id, ref LightInfo info);

        [DllImport("filament-native")]
        public static extern void AddGeometry(FilamentApp app, Guid id, ref GeometryInfo info);

        [DllImport("filament-native")]
        public static extern void AddMesh(FilamentApp app, Guid id, ref MeshInfo info);

        [DllImport("filament-native")]
        public static extern void AddGroup(FilamentApp app, Guid id);

        [DllImport("filament-native")]
        public static extern void SetWorldMatrix(FilamentApp app, Guid meshId, ref Matrix4x4 matrix);

        [DllImport("filament-native")]
        public static extern void SetObjTransform(FilamentApp app, Guid id, Matrix4x4 matrix);

        [DllImport("filament-native")]
        public static extern void SetObjParent(FilamentApp app, Guid id, Guid parentId);

        [DllImport("filament-native")]
        public static extern void AddMaterial(FilamentApp app, Guid id, ref MaterialInfo material);
        [DllImport("filament-native")]
        public static extern void UpdateMaterial(FilamentApp app, Guid id, ref MaterialInfo material);

        [DllImport("filament-native")]
        public static extern bool GetGraphicContext(FilamentApp app, out GraphicContextInfo info);

        [DllImport("filament-native")]
        public static extern void ReleaseContext(FilamentApp app, ReleaseContextMode mode);

        [DllImport("filament-native")]
        public static extern void SetObjVisible(FilamentApp app, Guid id, bool visible);

        [DllImport("filament-native")]
        public static extern void AddImageLight(FilamentApp app, ref ImageLightInfo info);

        [DllImport("filament-native")]
        public static extern void UpdateImageLight(FilamentApp app, ref ImageLightInfo info);

        [DllImport("filament-native")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool UpdateTexture(FilamentApp app, Guid id, ref ImageData info);

        [DllImport("filament-native")]
        public static extern void SetMeshMaterial(FilamentApp app, Guid id, Guid matId);


        [DllImport("filament-native")]
        public static extern void UpdateMeshGeometry(FilamentApp app, Guid meshId, Guid geometryId, ref GeometryInfo info);


        [DllImport("filament-native")]
        public static extern nint Allocate(uint size);

    }
}
