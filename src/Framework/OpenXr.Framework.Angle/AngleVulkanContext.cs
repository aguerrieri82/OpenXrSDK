#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Silk.NET.Core.Contexts;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace OpenXr.Framework.Angle;

public sealed unsafe class AngleVulkanContext : INativeContext, IAngleContext
{
    #region DELEGATES 


    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglCreateImageKhr(nint display, nint context, uint target, nint buffer, int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglDestroyImageKhr(nint display, nint image);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GlEglImageTargetTexStorageExt(uint target, nint image, int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void GlAcquireTexturesAngle(
        uint numTextures,
        uint* textures,
        uint* layouts);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void GlReleaseTexturesAngle(
        uint numTextures,
        uint* textures,
        uint* layouts);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglGetProcAddress(
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        string name);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglGetError();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglInitialize(
        nint display,
        int* major,
        int* minor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglTerminate(
        nint display);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglBindApi(
        uint api);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglChooseConfig(
        nint display,
        int* attributes,
        nint* configs,
        int configSize,
        int* configCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglCreateContext(
        nint display,
        nint config,
        nint sharedContext,
        int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglDestroyContext(
        nint display,
        nint context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglCreatePbufferSurface(
        nint display,
        nint config,
        int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglCreateWindowSurface(
        nint display,
        nint config,
        nint nativeWindow,
        int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglDestroySurface(
        nint display,
        nint surface);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglMakeCurrent(
        nint display,
        nint drawSurface,
        nint readSurface,
        nint context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglSwapBuffers(
        nint display,
        nint surface);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglSwapInterval(
        nint display,
        int interval);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglGetPlatformDisplayExt(
        uint platform,
        nint nativeDisplay,
        int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglQueryDisplayAttribExt(
        nint display,
        int attribute,
        nint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglQueryDeviceAttribExt(
        nint device,
        int attribute,
        nint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EglLockVulkanQueueAngle(
        nint display);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void EglUnlockVulkanQueueAngle(
        nint display);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void GlClipControlExt(uint origin, uint depth);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglGetPlatformDisplay(
    uint platform,
    nint nativeDisplay,
    nint* attributes);


    #endregion

    #region CONSTS

    private enum Egl : int
    {
        False = 0,
        True = 1,
        None = 0x3038,
        OpenGlEsApi = 0x30A0,
        SurfaceType = 0x3033,
        PbufferBit = 0x0001,
        WindowBit = 0x0004,
        RenderableType = 0x3040,
        OpenGlEs3Bit = 0x0040,
        RedSize = 0x3024,
        GreenSize = 0x3023,
        BlueSize = 0x3022,
        AlphaSize = 0x3021,
        DepthSize = 0x3025,
        StencilSize = 0x3026,
        Width = 0x3057,
        Height = 0x3056,
        ContextMajorVersionKhr = 0x3098,
        ContextMinorVersionKhr = 0x30FB,
        PlatformAngle = 0x3202,
        PlatformAngleType = 0x3203,
        PlatformAngleTypeVulkan = 0x3450,
        PlatformAngleDebugLayersEnabled = 0x3451,
        DeviceExt = 0x322C,
        VulkanInstance = 0x34A9,
        VulkanInstanceExtensions = 0x34AA,
        VulkanPhysicalDevice = 0x34AB,
        VulkanDevice = 0x34AC,
        VulkanDeviceExtensions = 0x34AD,
        VulkanQueue = 0x34AF,
        VulkanQueueFamilyIndex = 0x34D0,
        VulkanImage = 0x34D3,
        VulkanImageCreateInfoHi = 0x34D4,
        VulkanImageCreateInfoLo = 0x34D5,
        GlColorspaceKhr = 0x309D,
        GlColorspaceSrgbKhr = 0x3089
    }

    private const nint EglNoDisplay = 0;
    private const nint EglNoContext = 0;
    private const nint EglNoSurface = 0;

    #endregion

    #region ImportedVulkanImage

    public sealed class ImportedVulkanImage : IDisposable
    {
        private AngleVulkanContext? _owner;

        internal ImportedVulkanImage(AngleVulkanContext owner, nint eglImage, uint texture, TextureTarget target, nint vkImage)
        {
            _owner = owner;
            EglImage = eglImage;
            Texture = texture;
            Target = target;
            VkImage = vkImage;
        }

        public void Dispose()
        {
            AngleVulkanContext? owner = _owner;
            if (owner is null)
                return;

            _owner = null;
            owner.DestroyImportedImage(this);
        }

        internal void ClearHandles()
        {
            EglImage = 0;
            Texture = 0;
            VkImage = 0;
        }

        public nint EglImage { get; private set; }
        public uint Texture { get; private set; }
        public TextureTarget Target { get; }
        public nint VkImage { get; private set; }
    }

    #endregion

    #region SharedContext

    public sealed class SharedContext : INativeContext, IAngleContext
    {
        private AngleVulkanContext? _owner;
        private GL? _gl;

        internal SharedContext(AngleVulkanContext owner, nint context, nint surface)
        {
            _owner = owner;
            Context = context;
            Surface = surface;
        }

        public void MakeCurrent()
        {
            var owner = _owner ?? throw new ObjectDisposedException(nameof(SharedContext));
            owner.Check(owner._eglMakeCurrent(owner.Display, Surface, Surface, Context), "eglMakeCurrent(shared)");
        }

        public void ReleaseCurrent()
        {
            var owner = _owner ?? throw new ObjectDisposedException(nameof(SharedContext));
            owner.Check(owner._eglMakeCurrent(owner.Display, EglNoSurface, EglNoSurface, EglNoContext), "eglMakeCurrent(shared clear)");
        }

        public void SwapBuffers()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            var owner = _owner;
            if (owner is null)
                return;

            _owner = null;

            if (Surface != EglNoSurface)
                owner._eglDestroySurface(owner.Display, Surface);

            if (Context != EglNoContext)
                owner._eglDestroyContext(owner.Display, Context);

            _gl?.Dispose();
            _gl = null;
            Surface = EglNoSurface;
            Context = EglNoContext;
        }

        nint INativeContext.GetProcAddress(string proc, int? slot)
        {
            var owner = _owner ?? throw new ObjectDisposedException(nameof(SharedContext));
            return owner._eglGetProcAddress(proc);
        }

        bool INativeContext.TryGetProcAddress(string proc, out nint address, int? slot)
        {
            address = ((INativeContext)this).GetProcAddress(proc, slot);
            return address != 0;
        }

        public GL Gl => _gl ??= GL.GetApi(this);
        
        public nint Context { get; private set; }

        public nint Surface { get; private set; }

    }

    #endregion

    public class AquiredTexture
    {
        public uint Texture;

        public uint LastLayout;

        public bool IsAcquired;
    }

    #region DELGATES DECLARATIONS

    private readonly EglGetProcAddress _eglGetProcAddress;
    private readonly EglGetError _eglGetError;
    private readonly EglInitialize _eglInitialize;
    private readonly EglTerminate _eglTerminate;
    private readonly EglBindApi _eglBindApi;
    private readonly EglChooseConfig _eglChooseConfig;
    private readonly EglCreateContext _eglCreateContext;
    private readonly EglDestroyContext _eglDestroyContext;
    private readonly EglCreatePbufferSurface _eglCreatePbufferSurface;
    private readonly EglCreateWindowSurface _eglCreateWindowSurface;
    private readonly EglDestroySurface _eglDestroySurface;
    private readonly EglMakeCurrent _eglMakeCurrent;
    private readonly EglSwapBuffers _eglSwapBuffers;
    private readonly EglSwapInterval _eglSwapInterval;
    private readonly EglQueryDisplayAttribExt _eglQueryDisplayAttrib;
    private readonly EglQueryDeviceAttribExt _eglQueryDeviceAttrib;
    private readonly EglLockVulkanQueueAngle? _eglLockVulkanQueue;
    private readonly EglUnlockVulkanQueueAngle? _eglUnlockVulkanQueue;
    private readonly EglCreateImageKhr _eglCreateImage;
    private readonly EglDestroyImageKhr _eglDestroyImage;
    private readonly GlEglImageTargetTexStorageExt _glEglImageTargetTexStorage;
    private readonly GlAcquireTexturesAngle _glAcquireTexturesAngle;
    private readonly GlReleaseTexturesAngle _glReleaseTexturesAngle;
    private readonly EglGetPlatformDisplay _eglGetPlatformDisplayAttrib;

    #endregion

    private readonly nint _eglLibrary;
    private readonly nint _glesLibrary;
    private bool _disposed;
    private GL? _gl;
    private readonly Dictionary<nint, ImportedVulkanImage> _images = [];

    private readonly Dictionary<uint, AquiredTexture> _acquiredTextures = [];

    public AngleVulkanContext()
    {
        nint eglLibrary = 0;
        nint glesLibrary = 0;

#if __ANDROID__
        var eglLibraryName = "libEGL_angle.so";
        var glesLibraryName = "libGLESv2_angle.so";
#else
        var eglLibraryName = "libEGL.dll";
        var glesLibraryName = "libGLESv2.dll";
#endif

        try
        {
            eglLibrary = LoadLibrary(eglLibraryName);
            glesLibrary = LoadLibrary(glesLibraryName);

            _eglLibrary = eglLibrary;
            _glesLibrary = glesLibrary;
            _eglGetProcAddress = LoadExport<EglGetProcAddress>("eglGetProcAddress");
            _eglGetError = LoadExport<EglGetError>("eglGetError");
            _eglInitialize = LoadExport<EglInitialize>("eglInitialize");
            _eglTerminate = LoadExport<EglTerminate>("eglTerminate");
            _eglBindApi = LoadExport<EglBindApi>("eglBindAPI");
            _eglChooseConfig = LoadExport<EglChooseConfig>("eglChooseConfig");
            _eglCreateContext = LoadExport<EglCreateContext>("eglCreateContext");
            _eglDestroyContext = LoadExport<EglDestroyContext>("eglDestroyContext");
            _eglCreatePbufferSurface = LoadExport<EglCreatePbufferSurface>("eglCreatePbufferSurface");
            _eglCreateWindowSurface = LoadExport<EglCreateWindowSurface>("eglCreateWindowSurface");
            _eglDestroySurface = LoadExport<EglDestroySurface>("eglDestroySurface");
            _eglMakeCurrent = LoadExport<EglMakeCurrent>("eglMakeCurrent");
            _eglSwapBuffers = LoadExport<EglSwapBuffers>("eglSwapBuffers");
            _eglSwapInterval = LoadExport<EglSwapInterval>("eglSwapInterval");
            _eglQueryDisplayAttrib = LoadEglProc<EglQueryDisplayAttribExt>("eglQueryDisplayAttribEXT");
            _eglQueryDeviceAttrib = LoadEglProc<EglQueryDeviceAttribExt>("eglQueryDeviceAttribEXT");
            _eglLockVulkanQueue = TryLoadEglProc<EglLockVulkanQueueAngle>("eglLockVulkanQueueANGLE");
            _eglUnlockVulkanQueue = TryLoadEglProc<EglUnlockVulkanQueueAngle>("eglUnlockVulkanQueueANGLE");

            _eglGetPlatformDisplayAttrib = LoadEglProc<EglGetPlatformDisplay>("eglGetPlatformDisplay");
            _eglCreateImage = LoadEglProc<EglCreateImageKhr>("eglCreateImageKHR");
            _eglDestroyImage = LoadEglProc<EglDestroyImageKhr>("eglDestroyImageKHR");
            _glEglImageTargetTexStorage = LoadGlProc<GlEglImageTargetTexStorageExt>("glEGLImageTargetTexStorageEXT");
            _glAcquireTexturesAngle = LoadGlProc<GlAcquireTexturesAngle>("glAcquireTexturesANGLE");
            _glReleaseTexturesAngle = LoadGlProc<GlReleaseTexturesAngle>("glReleaseTexturesANGLE");
        }
        catch
        {
            if (glesLibrary != 0)
                NativeLibrary.Free(glesLibrary);

            if (eglLibrary != 0)
                NativeLibrary.Free(eglLibrary);

            throw;
        }
    }

    public void Initialize(IReadOnlyCollection<string> requiredInstanceExtensions, IReadOnlyCollection<string> requiredDeviceExtensions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            if (!IsInitialized)
            {
                nint* displayAttributes = stackalloc nint[]
                {
                    (int)Egl.PlatformAngleType,
                    (int)Egl.PlatformAngleTypeVulkan,

#if ANGLE_DEBUG
                    (int)Egl.PlatformAngleDebugLayersEnabled,
                    (int)Egl.True,
#endif
                    (int)Egl.None
                };

                Display = _eglGetPlatformDisplayAttrib((uint)Egl.PlatformAngle, 0, displayAttributes);
                CheckHandle(Display, EglNoDisplay, "eglGetPlatformDisplayEXT");

                int major;
                int minor;

                Check(_eglInitialize(Display, &major, &minor), "eglInitialize");
                Check(_eglBindApi((uint)Egl.OpenGlEsApi), "eglBindAPI");

                int* configAttributes = stackalloc int[]
                {
                    (int)Egl.SurfaceType,
                    (int)Egl.PbufferBit | (int)Egl.WindowBit,

                    (int)Egl.RenderableType,
                    (int)Egl.OpenGlEs3Bit,

                    (int)Egl.RedSize,
                    8,

                    (int)Egl.GreenSize,
                    8,

                    (int)Egl.BlueSize,
                    8,

                    (int)Egl.AlphaSize,
                    8,

                    (int)Egl.DepthSize,
                    24,

                    (int)Egl.StencilSize,
                    8,

                    (int)Egl.None
                };

                nint config;
                int configCount;

                Check(_eglChooseConfig(Display, configAttributes, &config, 1, &configCount), "eglChooseConfig");

                if (configCount == 0)
                    throw new InvalidOperationException("ANGLE returned no matching EGLConfig.");

                Config = config;

                int* contextAttributes = stackalloc int[]
                {
                    (int)Egl.ContextMajorVersionKhr,
                    3,

                    (int)Egl.ContextMinorVersionKhr,
                    2,

                    (int)Egl.None
                };

                Context = _eglCreateContext(Display, config, EglNoContext, contextAttributes);
                CheckHandle(Context, EglNoContext, "eglCreateContext");

                int* surfaceAttributes = stackalloc int[]
                {
                    (int)Egl.Width,
                    1,

                    (int)Egl.Height,
                    1,

                    (int)Egl.None
                };

                Surface = _eglCreatePbufferSurface(Display, config, surfaceAttributes);
                CheckHandle(Surface, EglNoSurface, "eglCreatePbufferSurface");
                Check(_eglMakeCurrent(Display, Surface, Surface, Context), "eglMakeCurrent");

                nint eglDevice;

                Check(_eglQueryDisplayAttrib(Display, (int)Egl.DeviceExt, &eglDevice), "eglQueryDisplayAttribEXT(EGL_DEVICE_EXT)");

                if (eglDevice == 0)
                    throw new InvalidOperationException("ANGLE returned a null EGLDevice.");

                EglDevice = eglDevice;

                _gl = GL.GetApi(this);
            }

            VulkanInstanceHandle = QueryDevicePointer(Egl.VulkanInstance);
            VulkanPhysicalDeviceHandle = QueryDevicePointer(Egl.VulkanPhysicalDevice);
            VulkanDeviceHandle = QueryDevicePointer(Egl.VulkanDevice);
            VulkanQueueHandle = QueryDevicePointer(Egl.VulkanQueue);
            QueueFamilyIndex = (uint)QueryDeviceInteger(Egl.VulkanQueueFamilyIndex);

            EnabledInstanceExtensions = QueryExtensionArray(Egl.VulkanInstanceExtensions);
            EnabledDeviceExtensions = QueryExtensionArray(Egl.VulkanDeviceExtensions);

            ValidateExtensions("Vulkan instance", requiredInstanceExtensions, EnabledInstanceExtensions);
            ValidateExtensions("Vulkan device", requiredDeviceExtensions, EnabledDeviceExtensions);
        }
        catch
        {
            DestroyEglObjects();
            throw;
        }
    }

    public ImportedVulkanImage AttachVulkanImage(
        nint vkImage,
        int vkFormat,
        uint width,
        uint height,
        uint arrayLayers,
        uint mipLevels,
        uint sampleCount,
        ImageUsageFlags vkUsage,
        ImageCreateFlags vkFlags,
        TextureTarget glTarget)
    {
        if (_images.TryGetValue(vkImage, out var image))
            return image;

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            PNext = null,
            Flags = vkFlags,
            ImageType = ImageType.Type2D,
            Format = (Format)vkFormat,
            Extent = new Extent3D { Width = width, Height = height, Depth = 1 },
            MipLevels = mipLevels,
            ArrayLayers = arrayLayers,
            Samples = (SampleCountFlags)sampleCount,
            Tiling = ImageTiling.Optimal,
            Usage = vkUsage,
            SharingMode = SharingMode.Exclusive,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null,
            InitialLayout = ImageLayout.Undefined
        };

        ulong imageInfoAddress = (ulong)&imageInfo;

        int* attributes = stackalloc int[]
        {
            (int)Egl.VulkanImageCreateInfoHi, unchecked((int)(imageInfoAddress >> 32)),
            (int)Egl.VulkanImageCreateInfoLo, unchecked((int)imageInfoAddress),
            (int)Egl.None
        };

        var imageHandle = vkImage;

        var eglImage = _eglCreateImage(Display, EglNoContext, (uint)Egl.VulkanImage, (nint)(&imageHandle), attributes);

        CheckHandle(eglImage, 0, "eglCreateImageKHR((uint)Egl.VulkanImage)");

        uint texture = 0;
        try
        {
            texture = _gl!.GenTexture();

            _gl.BindTexture(glTarget, texture);

            _glEglImageTargetTexStorage((uint)glTarget, eglImage, null);

            _gl.BindTexture(glTarget, 0);

            var result = new ImportedVulkanImage(this, eglImage, texture, glTarget, vkImage);

            _images[vkImage] = result;

            return result;
        }
        catch
        {
            _gl!.BindTexture(glTarget, 0);

            if (texture != 0)
                _gl.DeleteTexture(texture);

            _eglDestroyImage(Display, eglImage);

            throw;
        }
    }

    public void ReleaseAllTextures()
    {
        foreach (var info in _acquiredTextures.Values)
        {
            if (info.IsAcquired)
                ReleaseTexture(info.Texture);
        }
    }


    public void AcquireTexture(uint texture)
    {
        if (!_acquiredTextures.TryGetValue(texture, out var info))
        {
            info = new AquiredTexture
            {
                Texture = texture,
                LastLayout = 0,
                IsAcquired = false
            };
            _acquiredTextures[texture] = info;
        }

        if (!info.IsAcquired)
        {
            var layout = info.LastLayout;
            _glAcquireTexturesAngle(1, &texture, &layout);
            info.IsAcquired = true;
        }
    }

    public void ReleaseTexture(uint texture)
    {
        if (!_acquiredTextures.TryGetValue(texture, out var info) || !info.IsAcquired)
            throw new InvalidOperationException("Texture is not acquired.");

        uint layout;
        
        _glReleaseTexturesAngle(1, &texture, &layout);
        
        info.IsAcquired = false;
        info.LastLayout = layout;
    }

    public void CreateWindowSurface(nint nativeWindow)
    {
        if (Surface != EglNoSurface)
        {
            _eglMakeCurrent(Display, EglNoSurface, EglNoSurface, EglNoContext);
            _eglDestroySurface(Display, Surface);
        }

        int* attributes = stackalloc int[]
        {
            (int)Egl.GlColorspaceKhr,
            (int)Egl.GlColorspaceSrgbKhr,
            (int)Egl.None
        };

        Surface = _eglCreateWindowSurface(Display, Config, nativeWindow, attributes);

        CheckHandle(Surface, EglNoSurface, "eglCreateWindowSurface");
        Check(_eglMakeCurrent(Display, Surface, Surface, Context), "eglMakeCurrent(window)");
    }

    public void SwapBuffers()
    {
        Check(_eglSwapBuffers(Display, Surface), "eglSwapBuffers");
    }

    public void SetSwapInterval(int interval)
    {
        Check(_eglSwapInterval(Display, interval), "eglSwapInterval");
    }

    public void ReleaseCurrent()
    {
        Check(_eglMakeCurrent(Display, EglNoSurface, EglNoSurface, EglNoContext), "eglMakeCurrent(release)");
    }

    public void MakeCurrent()
    {
        EnsureInitialized();

        Check(_eglMakeCurrent(Display, Surface, Surface, Context), "eglMakeCurrent");
    }

    public SharedContext CreateSharedContext()
    {
        EnsureInitialized();

        int* contextAttributes = stackalloc int[]
        {
            (int)Egl.ContextMajorVersionKhr, 3,
            (int)Egl.ContextMinorVersionKhr, 2,
            (int)Egl.None
        };

        var context = _eglCreateContext(Display, Config, Context, contextAttributes);
        CheckHandle(context, EglNoContext, "eglCreateContext(shared)");

        try
        {
            int* surfaceAttributes = stackalloc int[]
            {
                (int)Egl.Width, 1,
                (int)Egl.Height, 1,
                (int)Egl.None
            };

            var surface = _eglCreatePbufferSurface(Display, Config, surfaceAttributes);
            CheckHandle(surface, EglNoSurface, "eglCreatePbufferSurface(shared)");

            return new SharedContext(this, context, surface);
        }
        catch
        {
            _eglDestroyContext(Display, context);
            throw;
        }
    }

    public void LockVulkanQueue()
    {
        EnsureInitialized();

        if (_eglLockVulkanQueue is null)
            throw new NotSupportedException("eglLockVulkanQueueANGLE is unavailable.");

        _eglLockVulkanQueue(Display);
    }

    public void UnlockVulkanQueue()
    {
        EnsureInitialized();

        if (_eglUnlockVulkanQueue is null)
            throw new NotSupportedException("eglUnlockVulkanQueueANGLE is unavailable.");

        _eglUnlockVulkanQueue(Display);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        DestroyEglObjects();

        if (_glesLibrary != 0)
            NativeLibrary.Free(_glesLibrary);

        if (_eglLibrary != 0)
            NativeLibrary.Free(_eglLibrary);

        GC.SuppressFinalize(this);
    }

    public static string[] SplitExtensionString(string? extensions)
    {
        if (string.IsNullOrWhiteSpace(extensions))
            return Array.Empty<string>();

        return extensions.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private nint QueryDevicePointer(Egl attribute)
    {
        nint value;

        Check(
            _eglQueryDeviceAttrib(EglDevice, (int)attribute, &value),
            $"eglQueryDeviceAttribEXT(0x{(int)attribute:X})");

        if (value == 0)
        {
            throw new InvalidOperationException($"ANGLE returned null for Vulkan attribute 0x{(int)attribute:X}.");
        }

        return value;
    }

    private nint QueryDeviceInteger(Egl attribute)
    {
        nint value;

        Check(
            _eglQueryDeviceAttrib(EglDevice, (int)attribute, &value),
            $"eglQueryDeviceAttribEXT(0x{(int)attribute:X})");

        return value;
    }

    private IReadOnlyList<string> QueryExtensionArray(Egl attribute)
    {
        nint arrayPointer;

        Check(
            _eglQueryDeviceAttrib(EglDevice, (int)attribute, &arrayPointer),
            $"eglQueryDeviceAttribEXT(0x{(int)attribute:X})");

        if (arrayPointer == 0)
            return [];

        var result = new List<string>();

        nint* extensionPointers = (nint*)arrayPointer;

        for (int i = 0; extensionPointers[i] != 0; i++)
        {
            string? extension = Marshal.PtrToStringUTF8(extensionPointers[i]);

            if (!string.IsNullOrEmpty(extension))
                result.Add(extension);
        }

        return result;
    }

    private static void ValidateExtensions(string category, IReadOnlyCollection<string> required, IReadOnlyList<string> enabled)
    {
        if (required.Count == 0)
            return;

        var enabledSet = new HashSet<string>(enabled, StringComparer.Ordinal);

        List<string>? missing = null;

        foreach (string extension in required)
        {
            if (string.IsNullOrWhiteSpace(extension))
                continue;

            if (enabledSet.Contains(extension))
                continue;

            missing ??= [];
            missing.Add(extension);
        }

        if (missing is null)
            return;

        throw new NotSupportedException($"ANGLE did not enable the required {category} extensions: {string.Join(", ", missing)}");
    }

    private void DestroyImportedImage(ImportedVulkanImage image)
    {
        _images.Remove(image.VkImage);

        var texture = image.Texture;
        var eglImage = image.EglImage;

        if (texture != 0)
            _gl!.DeleteTexture(texture);

        if (eglImage != 0 && Display != EglNoDisplay)
            _eglDestroyImage(Display, eglImage);

        image.ClearHandles();
    }

    private void DestroyImportedImages()
    {
        foreach (var image in _images.Values)
        {
            var texture = image.Texture;
            var eglImage = image.EglImage;

            if (texture != 0)
                _gl!.DeleteTexture(texture);

            if (eglImage != 0 && Display != EglNoDisplay)
                _eglDestroyImage(Display, eglImage);

            image.ClearHandles();
        }

        _images.Clear();
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsInitialized)
            throw new InvalidOperationException("ANGLE has not been initialized.");
    }

    private void DestroyEglObjects()
    {
        if (Display != EglNoDisplay)
        {
            DestroyImportedImages();

            _eglMakeCurrent(Display, EglNoSurface, EglNoSurface, EglNoContext);

            if (Surface != EglNoSurface)
            {
                _eglDestroySurface(Display, Surface);

                Surface = EglNoSurface;
            }

            if (Context != EglNoContext)
            {
                _eglDestroyContext(Display, Context);

                Context = EglNoContext;
            }

            _eglTerminate(Display);

            Display = EglNoDisplay;
        }

        Config = 0;
        EglDevice = 0;

        VulkanInstanceHandle = 0;
        VulkanPhysicalDeviceHandle = 0;
        VulkanDeviceHandle = 0;
        VulkanQueueHandle = 0;

        QueueFamilyIndex = 0;

        EnabledInstanceExtensions = Array.Empty<string>();
        EnabledDeviceExtensions = Array.Empty<string>();
    }

    private static nint LoadLibrary(string name)
    {
        if (!NativeLibrary.TryLoad(name, out nint library))
            throw new DllNotFoundException($"Could not load ANGLE library '{name}'.");

        return library;
    }

    private T LoadExport<T>(string name) where T : Delegate
    {
        nint address = NativeLibrary.GetExport(_eglLibrary, name);

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private T LoadEglProc<T>(string name) where T : Delegate
    {
        T? proc = TryLoadEglProc<T>(name);

        return proc ?? throw new EntryPointNotFoundException($"ANGLE EGL function '{name}' is unavailable.");
    }

    private T? TryLoadEglProc<T>(string name) where T : Delegate
    {
        nint address = _eglGetProcAddress(name);

        if (address == 0 && NativeLibrary.TryGetExport(_eglLibrary, name, out nint export))
        {
            address = export;
        }

        return address == 0 ? null : Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private T LoadGlProc<T>(string name) where T : Delegate
    {
        T? proc = TryLoadGlProc<T>(name);

        return proc ?? throw new EntryPointNotFoundException($"ANGLE GL function '{name}' is unavailable.");
    }

    private T? TryLoadGlProc<T>(string name) where T : Delegate
    {
        nint address = _eglGetProcAddress(name);

        if (address == 0 && NativeLibrary.TryGetExport(_glesLibrary, name, out nint export))
        {
            address = export;
        }

        return address == 0 ? null : Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private void Check(int result, string operation)
    {
        if (result == (int)Egl.False)
            throw new InvalidOperationException($"{operation} failed with EGL error 0x{_eglGetError():X4}.");
    }

    private void CheckHandle(nint value, nint invalidValue, string operation)
    {
        if (value == invalidValue)
            throw new InvalidOperationException($"{operation} failed with EGL error 0x{_eglGetError():X4}.");
    }

    nint INativeContext.GetProcAddress(string proc, int? slot)
    {
        return _eglGetProcAddress(proc);
    }

    bool INativeContext.TryGetProcAddress(string proc, out nint addr, int? slot)
    {
        addr = ((INativeContext)this).GetProcAddress(proc, slot);
        return addr != 0;
    }

    public nint Display { get; private set; }

    public nint Config { get; private set; }
    
    public nint Context { get; private set; }
    
    public nint Surface { get; private set; }
    
    public nint EglDevice { get; private set; }
    
    public nint VulkanInstanceHandle { get; private set; }
    
    public nint VulkanPhysicalDeviceHandle { get; private set; }
    
    public nint VulkanDeviceHandle { get; private set; }
    
    public nint VulkanQueueHandle { get; private set; }
    
    public uint QueueFamilyIndex { get; private set; }

    public IReadOnlyList<string> EnabledInstanceExtensions { get; private set; } = [];

    public IReadOnlyList<string> EnabledDeviceExtensions { get; private set; } = [];

    public bool IsInitialized => Display != EglNoDisplay;

    public uint QueueIndex => 0;

    public GL? Gl => _gl;



}
