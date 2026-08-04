#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using Silk.NET.Core.Contexts;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using System.Diagnostics;

namespace OpenXr.Framework.Angle;

public sealed unsafe class AngleVulkanContext : IDisposable, INativeContext
{
    #region INTEROP

    private const int EGL_FALSE = 0;
    private const int EGL_NONE = 0x3038;

    private const uint EGL_OPENGL_ES_API = 0x30A0;

    private const int EGL_SURFACE_TYPE = 0x3033;
    private const int EGL_PBUFFER_BIT = 0x0001;
    private const int EGL_WINDOW_BIT = 0x0004;
    private const int EGL_RENDERABLE_TYPE = 0x3040;
    private const int EGL_OPENGL_ES3_BIT = 0x0040;

    private const int EGL_RED_SIZE = 0x3024;
    private const int EGL_GREEN_SIZE = 0x3023;
    private const int EGL_BLUE_SIZE = 0x3022;
    private const int EGL_ALPHA_SIZE = 0x3021;
    private const int EGL_DEPTH_SIZE = 0x3025;
    private const int EGL_STENCIL_SIZE = 0x3026;

    private const int EGL_WIDTH = 0x3057;
    private const int EGL_HEIGHT = 0x3056;

    private const int EGL_CONTEXT_MAJOR_VERSION_KHR = 0x3098;
    private const int EGL_CONTEXT_MINOR_VERSION_KHR = 0x30FB;

    private const uint EGL_PLATFORM_ANGLE_ANGLE = 0x3202;
    private const int EGL_PLATFORM_ANGLE_TYPE_ANGLE = 0x3203;
    private const int EGL_PLATFORM_ANGLE_TYPE_VULKAN_ANGLE = 0x3450;

    private const int EGL_PLATFORM_ANGLE_DEBUG_LAYERS_ENABLED_ANGLE = 0x3451;

    private const int EGL_TRUE = 1;


    private const int EGL_DEVICE_EXT = 0x322C;

    private const int EGL_VULKAN_INSTANCE_ANGLE = 0x34A9;
    private const int EGL_VULKAN_INSTANCE_EXTENSIONS_ANGLE = 0x34AA;
    private const int EGL_VULKAN_PHYSICAL_DEVICE_ANGLE = 0x34AB;
    private const int EGL_VULKAN_DEVICE_ANGLE = 0x34AC;
    private const int EGL_VULKAN_DEVICE_EXTENSIONS_ANGLE = 0x34AD;
    private const int EGL_VULKAN_QUEUE_ANGLE = 0x34AF;

    // The typo "FAMILIY" is present in ANGLE's actual public token.
    private const int EGL_VULKAN_QUEUE_FAMILIY_INDEX_ANGLE = 0x34D0;

    private const uint EGL_VULKAN_IMAGE_ANGLE = 0x34D3;
    private const int EGL_VULKAN_IMAGE_CREATE_INFO_HI_ANGLE = 0x34D4;
    private const int EGL_VULKAN_IMAGE_CREATE_INFO_LO_ANGLE = 0x34D5;

    private const uint GL_TEXTURE_2D = 0x0DE1;
    private const uint GL_TEXTURE_2D_ARRAY = 0x8C1A;
    private const uint GL_NO_ERROR = 0;

    private const nint EGL_NO_DISPLAY = 0;
    private const nint EGL_NO_CONTEXT = 0;
    private const nint EGL_NO_SURFACE = 0;

    private const int EGL_GL_COLORSPACE_KHR = 0x309D;
    private const int EGL_GL_COLORSPACE_SRGB_KHR = 0x3089;

    private const uint GL_UPPER_LEFT_EXT = 0x8CA2;
    private const uint GL_NEGATIVE_ONE_TO_ONE_EXT = 0x935E;


    private readonly nint _eglLibrary;
    private readonly nint _glesLibrary;

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

    private readonly EglGetPlatformDisplayExt _eglGetPlatformDisplay;
    private readonly EglQueryDisplayAttribExt _eglQueryDisplayAttrib;
    private readonly EglQueryDeviceAttribExt _eglQueryDeviceAttrib;

    private readonly EglLockVulkanQueueAngle? _eglLockVulkanQueue;
    private readonly EglUnlockVulkanQueueAngle? _eglUnlockVulkanQueue;

    private readonly EglCreateImageKhr _eglCreateImage;
    private readonly EglDestroyImageKhr _eglDestroyImage;

    private readonly GlGenTextures _glGenTextures;
    private readonly GlDeleteTextures _glDeleteTextures;
    private readonly GlBindTexture _glBindTexture;
    private readonly GlEglImageTargetTexStorageExt _glEglImageTargetTexStorage;
    private readonly GlGetError _glGetError;

    private GlClipControlExt _glClipControlExt = null!;

    private readonly List<ImportedVulkanImage> _importedImages = [];

    #endregion

    private bool _disposed;
    private GL? _gl;
    private readonly Dictionary<nint, ImportedVulkanImage> _images = [];

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

            _eglGetProcAddress =
                LoadExport<EglGetProcAddress>("eglGetProcAddress");

            _eglGetError =
                LoadExport<EglGetError>("eglGetError");

            _eglInitialize =
                LoadExport<EglInitialize>("eglInitialize");

            _eglTerminate =
                LoadExport<EglTerminate>("eglTerminate");

            _eglBindApi =
                LoadExport<EglBindApi>("eglBindAPI");

            _eglChooseConfig =
                LoadExport<EglChooseConfig>("eglChooseConfig");

            _eglCreateContext =
                LoadExport<EglCreateContext>("eglCreateContext");

            _eglDestroyContext =
                LoadExport<EglDestroyContext>("eglDestroyContext");

            _eglCreatePbufferSurface =
                LoadExport<EglCreatePbufferSurface>("eglCreatePbufferSurface");

            _eglCreateWindowSurface =
                LoadExport<EglCreateWindowSurface>("eglCreateWindowSurface");

            _eglDestroySurface =
                LoadExport<EglDestroySurface>("eglDestroySurface");

            _eglMakeCurrent =
                LoadExport<EglMakeCurrent>("eglMakeCurrent");

            _eglSwapBuffers =
                LoadExport<EglSwapBuffers>("eglSwapBuffers");

            _eglSwapInterval =
                LoadExport<EglSwapInterval>("eglSwapInterval");

            _eglGetPlatformDisplay =
                LoadEglProc<EglGetPlatformDisplayExt>(
                    "eglGetPlatformDisplayEXT");

            _eglQueryDisplayAttrib =
                LoadEglProc<EglQueryDisplayAttribExt>(
                    "eglQueryDisplayAttribEXT");

            _eglQueryDeviceAttrib =
                LoadEglProc<EglQueryDeviceAttribExt>(
                    "eglQueryDeviceAttribEXT");

            _eglLockVulkanQueue =
                TryLoadEglProc<EglLockVulkanQueueAngle>(
                    "eglLockVulkanQueueANGLE");

            _eglUnlockVulkanQueue =
                TryLoadEglProc<EglUnlockVulkanQueueAngle>(
                    "eglUnlockVulkanQueueANGLE");

            _glClipControlExt = LoadEglProc<GlClipControlExt>("glClipControlEXT");

            _eglCreateImage = LoadEglProc<EglCreateImageKhr>("eglCreateImageKHR");
            _eglDestroyImage = LoadEglProc<EglDestroyImageKhr>("eglDestroyImageKHR");

            _glGenTextures = LoadGlProc<GlGenTextures>("glGenTextures");
            _glDeleteTextures = LoadGlProc<GlDeleteTextures>("glDeleteTextures");
            _glBindTexture = LoadGlProc<GlBindTexture>("glBindTexture");
            _glEglImageTargetTexStorage =
                LoadGlProc<GlEglImageTargetTexStorageExt>("glEGLImageTargetTexStorageEXT");
            _glGetError = LoadGlProc<GlGetError>("glGetError");
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

    public void Initialize(
        IReadOnlyCollection<string> requiredInstanceExtensions,
        IReadOnlyCollection<string> requiredDeviceExtensions)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            if (!IsInitialized)
            {
                int* displayAttributes = stackalloc int[]
                {
                    EGL_PLATFORM_ANGLE_TYPE_ANGLE,
                    EGL_PLATFORM_ANGLE_TYPE_VULKAN_ANGLE,

                    //EGL_PLATFORM_ANGLE_DEBUG_LAYERS_ENABLED_ANGLE,
                    //EGL_TRUE,
                
                    EGL_NONE
                };

                Display = _eglGetPlatformDisplay(
                    EGL_PLATFORM_ANGLE_ANGLE,
                    0,
                    displayAttributes);

                CheckHandle(
                    Display,
                    EGL_NO_DISPLAY,
                    "eglGetPlatformDisplayEXT");

                int major;
                int minor;

                Check(
                    _eglInitialize(
                        Display,
                        &major,
                        &minor),
                    "eglInitialize");

                Check(
                    _eglBindApi(EGL_OPENGL_ES_API),
                    "eglBindAPI");

                int* configAttributes = stackalloc int[]
                {
                EGL_SURFACE_TYPE,
                EGL_PBUFFER_BIT | EGL_WINDOW_BIT,

                EGL_RENDERABLE_TYPE,
                EGL_OPENGL_ES3_BIT,

                EGL_RED_SIZE,
                8,

                EGL_GREEN_SIZE,
                8,

                EGL_BLUE_SIZE,
                8,

                EGL_ALPHA_SIZE,
                8,

                EGL_DEPTH_SIZE,
                24,

                EGL_STENCIL_SIZE,
                8,

                EGL_NONE
            };

                nint config;
                int configCount;

                Check(
                    _eglChooseConfig(
                        Display,
                        configAttributes,
                        &config,
                        1,
                        &configCount),
                    "eglChooseConfig");

                if (configCount == 0)
                {
                    throw new InvalidOperationException(
                        "ANGLE returned no matching EGLConfig.");
                }

                Config = config;

                int* contextAttributes = stackalloc int[]
                {
                EGL_CONTEXT_MAJOR_VERSION_KHR,
                3,

                EGL_CONTEXT_MINOR_VERSION_KHR,
                2,

                EGL_NONE
            };

                Context = _eglCreateContext(
                    Display,
                    config,
                    EGL_NO_CONTEXT,
                    contextAttributes);

                CheckHandle(
                    Context,
                    EGL_NO_CONTEXT,
                    "eglCreateContext");

                int* surfaceAttributes = stackalloc int[]
                {
                EGL_WIDTH,
                1,

                EGL_HEIGHT,
                1,

                EGL_NONE
            };

                Surface = _eglCreatePbufferSurface(
                    Display,
                    config,
                    surfaceAttributes);

                CheckHandle(
                    Surface,
                    EGL_NO_SURFACE,
                    "eglCreatePbufferSurface");

                Check(
                    _eglMakeCurrent(
                        Display,
                        Surface,
                        Surface,
                        Context),
                    "eglMakeCurrent");

                nint eglDevice;

                Check(
                    _eglQueryDisplayAttrib(
                        Display,
                        EGL_DEVICE_EXT,
                        &eglDevice),
                    "eglQueryDisplayAttribEXT(EGL_DEVICE_EXT)");

                if (eglDevice == 0)
                {
                    throw new InvalidOperationException(
                        "ANGLE returned a null EGLDevice.");
                }

                EglDevice = eglDevice;

                _gl = GL.GetApi(this);
            }

            VulkanInstanceHandle = QueryDevicePointer(EGL_VULKAN_INSTANCE_ANGLE);
            VulkanPhysicalDeviceHandle = QueryDevicePointer(EGL_VULKAN_PHYSICAL_DEVICE_ANGLE);
            VulkanDeviceHandle = QueryDevicePointer(EGL_VULKAN_DEVICE_ANGLE);
            VulkanQueueHandle = QueryDevicePointer(EGL_VULKAN_QUEUE_ANGLE);
            QueueFamilyIndex = (uint)QueryDeviceInteger(EGL_VULKAN_QUEUE_FAMILIY_INDEX_ANGLE);

            EnabledInstanceExtensions = QueryExtensionArray(EGL_VULKAN_INSTANCE_EXTENSIONS_ANGLE);
            EnabledDeviceExtensions = QueryExtensionArray(EGL_VULKAN_DEVICE_EXTENSIONS_ANGLE);

            ValidateExtensions(
                "Vulkan instance",
                requiredInstanceExtensions,
                EnabledInstanceExtensions);

            ValidateExtensions(
                "Vulkan device",
                requiredDeviceExtensions,
                EnabledDeviceExtensions);
        }
        catch
        {
            DestroyEglObjects();
            throw;
        }
    }

    public sealed class ImportedVulkanImage : IDisposable
    {
        private AngleVulkanContext? _owner;

        internal ImportedVulkanImage(AngleVulkanContext owner, nint eglImage, uint texture, TextureTarget target)
        {
            _owner = owner;
            EglImage = eglImage;
            Texture = texture;
            Target = target;
        }

        public nint EglImage { get; private set; }
        public uint Texture { get; private set; }
        public TextureTarget Target { get; }

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
        }
    }

    /// <summary>
    /// Imports an existing VkImage and creates a GL texture aliasing the same storage.
    /// No VkImage or Vulkan memory is created.
    /// </summary>
    public ImportedVulkanImage AttachVulkanImage(
        nint vkImage,
        int vkFormat,
        uint width,
        uint height,
        uint arrayLayers,
        uint mipLevels,
        uint sampleCount,
        ImageUsageFlags vkUsage,
        TextureTarget glTarget)
    {
        EnsureInitialized();

        if (_images.TryGetValue(vkImage, out var image))
            return image;

        VkImageCreateInfoNative imageInfo = new()
        {
            SType = VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            PNext = null,
            Flags = 0,
            ImageType = VK_IMAGE_TYPE_2D,
            Format = vkFormat,
            Extent = new VkExtent3DNative { Width = width, Height = height, Depth = 1 },
            MipLevels = mipLevels,
            ArrayLayers = arrayLayers,
            Samples = sampleCount,
            Tiling = VK_IMAGE_TILING_OPTIMAL,
            Usage = (uint)vkUsage,
            SharingMode = VK_SHARING_MODE_EXCLUSIVE,
            QueueFamilyIndexCount = 0,
            PQueueFamilyIndices = null,
            InitialLayout = VK_IMAGE_LAYOUT_UNDEFINED
        };

        ulong imageInfoAddress = (ulong)&imageInfo;

        int* attributes = stackalloc int[]
        {
            EGL_VULKAN_IMAGE_CREATE_INFO_HI_ANGLE, unchecked((int)(imageInfoAddress >> 32)),
            EGL_VULKAN_IMAGE_CREATE_INFO_LO_ANGLE, unchecked((int)imageInfoAddress),
            EGL_NONE
        };

        nint imageHandle = vkImage;
        nint eglImage = _eglCreateImage(
            Display, EGL_NO_CONTEXT, EGL_VULKAN_IMAGE_ANGLE, (nint)(&imageHandle), attributes);

        CheckHandle(eglImage, 0, "eglCreateImageKHR(EGL_VULKAN_IMAGE_ANGLE)");

        uint texture = 0;
        try
        {

            texture = _gl!.GenTexture();

            _gl.BindTexture(glTarget, texture);

            _glEglImageTargetTexStorage((uint)glTarget, eglImage, null);

            _gl.BindTexture(glTarget, 0);

            var result = new ImportedVulkanImage(this, eglImage, texture, glTarget);
            _importedImages.Add(result);

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

    public void CreateWindowSurface(nint nativeWindow)
    {
        if (Surface != EGL_NO_SURFACE)
        {
            _eglMakeCurrent(Display, EGL_NO_SURFACE, EGL_NO_SURFACE, EGL_NO_CONTEXT);
            _eglDestroySurface(Display, Surface);
        }

        int* attributes = stackalloc int[]
        {
            EGL_GL_COLORSPACE_KHR,
            EGL_GL_COLORSPACE_SRGB_KHR,
            EGL_NONE
        };

        Surface = _eglCreateWindowSurface(
            Display,
            Config,
            nativeWindow,
            attributes);

        CheckHandle(Surface, EGL_NO_SURFACE, "eglCreateWindowSurface");
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
        Check(
            _eglMakeCurrent(
                Display,
                EGL_NO_SURFACE,
                EGL_NO_SURFACE,
                EGL_NO_CONTEXT),
            "eglMakeCurrent(release)");
    }

    public void MakeCurrent()
    {
        EnsureInitialized();

        Check(
            _eglMakeCurrent(
                Display,
                Surface,
                Surface,
                Context),
            "eglMakeCurrent");
    }

    public void ClearCurrent()
    {
        EnsureInitialized();

        Check(
            _eglMakeCurrent(
                Display,
                EGL_NO_SURFACE,
                EGL_NO_SURFACE,
                EGL_NO_CONTEXT),
            "eglMakeCurrent(clear)");
    }

    public void LockVulkanQueue()
    {
        EnsureInitialized();

        if (_eglLockVulkanQueue is null)
        {
            throw new NotSupportedException(
                "eglLockVulkanQueueANGLE is unavailable.");
        }

        _eglLockVulkanQueue(Display);
    }

    public void UnlockVulkanQueue()
    {
        EnsureInitialized();

        if (_eglUnlockVulkanQueue is null)
        {
            throw new NotSupportedException(
                "eglUnlockVulkanQueueANGLE is unavailable.");
        }

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

        return extensions.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
    }

    private nint QueryDevicePointer(int attribute)
    {
        nint value;

        Check(
            _eglQueryDeviceAttrib(
                EglDevice,
                attribute,
                &value),
            $"eglQueryDeviceAttribEXT(0x{attribute:X})");

        if (value == 0)
        {
            throw new InvalidOperationException(
                $"ANGLE returned null for Vulkan attribute 0x{attribute:X}.");
        }

        return value;
    }

    private nint QueryDeviceInteger(int attribute)
    {
        nint value;

        Check(
            _eglQueryDeviceAttrib(
                EglDevice,
                attribute,
                &value),
            $"eglQueryDeviceAttribEXT(0x{attribute:X})");

        return value;
    }

    private IReadOnlyList<string> QueryExtensionArray(int attribute)
    {
        nint arrayPointer;

        Check(
            _eglQueryDeviceAttrib(
                EglDevice,
                attribute,
                &arrayPointer),
            $"eglQueryDeviceAttribEXT(0x{attribute:X})");

        if (arrayPointer == 0)
            return Array.Empty<string>();

        var result = new List<string>();

        nint* extensionPointers = (nint*)arrayPointer;

        for (int i = 0; extensionPointers[i] != 0; i++)
        {
            string? extension =
                Marshal.PtrToStringUTF8(extensionPointers[i]);

            if (!string.IsNullOrEmpty(extension))
                result.Add(extension);
        }

        return result;
    }

    private static void ValidateExtensions(
        string category,
        IReadOnlyCollection<string> required,
        IReadOnlyList<string> enabled)
    {
        if (required.Count == 0)
            return;

        var enabledSet = new HashSet<string>(
            enabled,
            StringComparer.Ordinal);

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

        throw new NotSupportedException(
            $"ANGLE did not enable the required {category} extensions: " +
            string.Join(", ", missing));
    }

    private static bool IsValidVkSampleCount(uint sampleCount)
    {
        return sampleCount is 1 or 2 or 4 or 8 or 16 or 32 or 64;
    }

    private void CheckGl(string operation)
    {
        uint error = _glGetError();
        if (error != GL_NO_ERROR)
            throw new InvalidOperationException($"{operation} failed with GL error 0x{error:X4}.");
    }

    private void DestroyImportedImage(ImportedVulkanImage image)
    {
        _importedImages.Remove(image);
        uint texture = image.Texture;
        nint eglImage = image.EglImage;

        if (texture != 0) _glDeleteTextures(1, &texture);
        if (eglImage != 0 && Display != EGL_NO_DISPLAY) _eglDestroyImage(Display, eglImage);

        image.ClearHandles();
    }

    private void DestroyImportedImages()
    {
        for (int i = _importedImages.Count - 1; i >= 0; i--)
        {
            ImportedVulkanImage image = _importedImages[i];
            uint texture = image.Texture;
            nint eglImage = image.EglImage;

            if (texture != 0) _glDeleteTextures(1, &texture);
            if (eglImage != 0 && Display != EGL_NO_DISPLAY) _eglDestroyImage(Display, eglImage);
            image.ClearHandles();
        }

        _importedImages.Clear();
    }

    private void EnsureInitialized()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!IsInitialized)
        {
            throw new InvalidOperationException(
                "ANGLE has not been initialized.");
        }
    }

    private void DestroyEglObjects()
    {
        if (Display != EGL_NO_DISPLAY)
        {
            DestroyImportedImages();

            _eglMakeCurrent(
                Display,
                EGL_NO_SURFACE,
                EGL_NO_SURFACE,
                EGL_NO_CONTEXT);

            if (Surface != EGL_NO_SURFACE)
            {
                _eglDestroySurface(
                    Display,
                    Surface);

                Surface = EGL_NO_SURFACE;
            }

            if (Context != EGL_NO_CONTEXT)
            {
                _eglDestroyContext(
                    Display,
                    Context);

                Context = EGL_NO_CONTEXT;
            }

            _eglTerminate(Display);

            Display = EGL_NO_DISPLAY;
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
        {
            throw new DllNotFoundException(
                $"Could not load ANGLE library '{name}'.");
        }

        return library;
    }

    private T LoadExport<T>(string name)
        where T : Delegate
    {
        nint address =
            NativeLibrary.GetExport(
                _eglLibrary,
                name);

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private T LoadEglProc<T>(string name)
        where T : Delegate
    {
        T? proc = TryLoadEglProc<T>(name);

        return proc ??
               throw new EntryPointNotFoundException(
                   $"ANGLE EGL function '{name}' is unavailable.");
    }

    private T? TryLoadEglProc<T>(string name)
        where T : Delegate
    {
        nint address = _eglGetProcAddress(name);

        if (address == 0 &&
            NativeLibrary.TryGetExport(
                _eglLibrary,
                name,
                out nint export))
        {
            address = export;
        }

        return address == 0
            ? null
            : Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private T LoadGlProc<T>(string name)
        where T : Delegate
    {
        nint address = _eglGetProcAddress(name);

        if (address == 0 && NativeLibrary.TryGetExport(_glesLibrary, name, out nint export))
            address = export;

        if (address == 0)
            throw new EntryPointNotFoundException($"ANGLE GL function '{name}' is unavailable.");

        return Marshal.GetDelegateForFunctionPointer<T>(address);
    }

    private void Check(int result, string operation)
    {
        if (result != EGL_FALSE)
            return;

        int error = _eglGetError();

        throw new InvalidOperationException(
            $"{operation} failed with EGL error 0x{error:X4}.");
    }

    private void CheckHandle(
        nint value,
        nint invalidValue,
        string operation)
    {
        if (value != invalidValue)
            return;

        int error = _eglGetError();

        throw new InvalidOperationException(
            $"{operation} failed with EGL error 0x{error:X4}.");
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

    public uint QueueIndex => 0;

    public GL? Gl => _gl;


    public IReadOnlyList<string> EnabledInstanceExtensions { get; private set; } =
        Array.Empty<string>();

    public IReadOnlyList<string> EnabledDeviceExtensions { get; private set; } =
        Array.Empty<string>();

    public bool IsInitialized => Display != EGL_NO_DISPLAY;


    #region INTEROP

    private const uint VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO = 14;
    private const int VK_IMAGE_TYPE_2D = 1;
    private const int VK_IMAGE_TILING_OPTIMAL = 0;
    private const int VK_SHARING_MODE_EXCLUSIVE = 0;
    private const int VK_IMAGE_LAYOUT_UNDEFINED = 0;

    [StructLayout(LayoutKind.Sequential)]
    private struct VkExtent3DNative
    {
        public uint Width;
        public uint Height;
        public uint Depth;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct VkImageCreateInfoNative
    {
        public uint SType;
        public void* PNext;
        public uint Flags;
        public int ImageType;
        public int Format;
        public VkExtent3DNative Extent;
        public uint MipLevels;
        public uint ArrayLayers;
        public uint Samples;
        public int Tiling;
        public uint Usage;
        public int SharingMode;
        public uint QueueFamilyIndexCount;
        public uint* PQueueFamilyIndices;
        public int InitialLayout;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nint EglCreateImageKhr(nint display, nint context, uint target, nint buffer, int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int EglDestroyImageKhr(nint display, nint image);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GlGenTextures(int count, uint* textures);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GlDeleteTextures(int count, uint* textures);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GlBindTexture(uint target, uint texture);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void GlEglImageTargetTexStorageExt(uint target, nint image, int* attributes);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint GlGetError();

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

    #endregion
}