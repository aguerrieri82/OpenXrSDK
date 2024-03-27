using Android.Opengl;

namespace OpenXr.Framework.Android
{
    public class OpenGLESContext
    {
        public static OpenGLESContext Create(bool debugMode)
        {
            int[] numConfigs = new int[1];
            int[] value = new int[1];
            int[] maj = new int[1];
            int[] min = new int[1];

            var display = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);

            EGL14.EglInitialize(display, maj, 0, min, 0);

            EGLConfig[] configs = new EGLConfig[1024];


            if (!EGL14.EglGetConfigs(display, configs, 0, 1024, numConfigs, 0))
            {
                throw new Exception($"EglGetConfigs: {EGL14.EglGetError()}");
            }

            int[] configAttribs =
            {
                EGL14.EglRedSize,
                8,
                EGL14.EglGreenSize,
                8,
                EGL14.EglBlueSize,
                8,
                EGL14.EglAlphaSize,
                8,
                EGL14.EglDepthSize,
                16,
                EGL14.EglStencilSize,
                0,
                EGL14.EglSamples,
                0,
                EGL14.EglNone
            };

            EGLConfig? config = null;

            bool isConfigValid = false;

            for (int i = 0; i < numConfigs[0]; i++)
            {

                EGL14.EglGetConfigAttrib(display, configs[i], EGL14.EglRenderableType, value, 0);
                if ((value[0] & EGL15.EglOpenglEs3Bit) == 0)
                    continue;

                EGL14.EglGetConfigAttrib(display, configs[i], EGL14.EglSurfaceType, value, 0);
                if ((value[0] & (EGL14.EglWindowBit | EGL14.EglPbufferBit)) != (EGL14.EglWindowBit | EGL14.EglPbufferBit))
                    continue;

                int j = 0;
                for (j = 0; configAttribs[j] != EGL14.EglNone; j += 2)
                {
                    EGL14.EglGetConfigAttrib(display, configs[i], configAttribs[j], value, 0);
                    if (value[0] != configAttribs[j + 1])
                    {
                        if (configAttribs[j] == EGL14.EglDepthSize)
                        {
                            System.Diagnostics.Debug.WriteLine(value[0]);
                        }
                        break;
                    }
                }
                if (configAttribs[j] == EGL14.EglNone)
                {
                    config = configs[i];
                    isConfigValid = true;
                    break;
                }
            }

            if (!isConfigValid)
                throw new Exception("config not found");

            var context = EGL14.EglCreateContext(
                display,
                config,
                EGL14.EglNoContext,
                [
                    EGL15.EglContextMajorVersion, 3,
                   // EGL15.EglContextMinorVersion, 2,
                    EGL15.EglContextOpenglDebug, (debugMode ? EGL14.EglTrue : EGL14.EglFalse),
                   // EGL15.EglContextOpenglProfileMask, EGL15.EglContextOpenglCoreProfileBit,
                    EGL14.EglNone
                ],
                0);

            if (context == EGL14.EglNoContext)
                throw new Exception("EglCreateContext");

            var surface = EGL14.EglCreatePbufferSurface(
                display,
                config,
                [EGL14.EglWidth, 16, EGL14.EglHeight, 16, EGL14.EglNone],
                0
            );

            if (surface == EGL14.EglNoSurface)
            {
                EGL14.EglDestroyContext(display, context);
                throw new Exception("EglCreatePbufferSurface");
            }

            if (!EGL14.EglMakeCurrent(display, surface, surface, context))
            {
                EGL14.EglDestroyContext(display, context);
                EGL14.EglDestroySurface(display, surface);
                throw new Exception("EglMakeCurrent");
            }


            //EGL14.EglSwapInterval(display, 0);


            return new OpenGLESContext
            {
                Display = display,
                Config = config,
                Surface = surface,
                Context = context
            };

        }

        public void MakeCurrent()
        {
            EGL14.EglMakeCurrent(Display, Surface, Surface, Context);
        }

        public EGLConfig? Config;

        public EGLDisplay? Display;

        public EGLSurface? Surface;

        public EGLContext? Context;
    }
}
