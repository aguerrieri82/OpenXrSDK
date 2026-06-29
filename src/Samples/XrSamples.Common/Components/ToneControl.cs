#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using XrEngine;
using XrEngine.OpenGL;


namespace XrSamples.Components
{
    public class ToneControl : BaseComponent<Scene3D>
    {
        private bool _fbRgb;
        private bool _texSRgb;
        private ToneMapMode _toneMap;
        private bool _showPbr;
        void Update()
        {
            GlState.Current!.EnableFeature(EnableCap.FramebufferSrgb, FbRgb);
            
            PbrV2Material.SHADER.ToneMap = _toneMap;
            
            PbrV2Material.SHADER.NotifyChanged(ChangeType.Render);

            Changed?.Invoke();
        }

        public bool FbRgb
        {
            get => _fbRgb;
            set
            {
                _fbRgb = value;
                Update();
            }
        }

        public bool TexSRgb
        {
            get => _texSRgb;
            set
            {
                _texSRgb = value;
                Update();
            }
        }

        public ToneMapMode ToneMap
        {
            get => _toneMap;
            set
            {
                _toneMap = value;
                Update();
            }
        }

        public bool ShowPbr
        {
            get => _showPbr;
            set
            {
                _showPbr = value;
                Update();
            }
        }

        public Action? Changed;


    }
}
