#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif


using XrMath;

namespace XrEngine.OpenGL
{
    public class GlState
    {
        private GL _gl;

        public GlState(GL gl)
        {
            _gl = gl;
        }


        public void Reset()
        {
            WriteDepth = null;
            UseDepth = null;
            DoubleSided = null;
            WriteColor = null;
            ActiveProgram = null;
            Wireframe = null;
            Alpha = null;
            LineWidth = null;
            View = null;
            ActiveProgram = null;
            CullFace = null;
            ClearDepth = null;
            ClearColor = null;
            ActiveTexture = null;
            TexturesSlots.Clear();
            TexturesTargets.Clear();
            Features.Clear();   
        }

        public void Restore()
        {
            if (ActiveProgram.HasValue)
                SetActiveProgram(ActiveProgram.Value, true);
            
            if (View.HasValue)
                SetView(View.Value, true);
            
            if (WriteDepth.HasValue)
                SetWriteDepth(WriteDepth.Value, true);

            if (UseDepth.HasValue)
                SetUseDepth(UseDepth.Value, true);
            
            if (DoubleSided.HasValue)
                SetDoubleSided(DoubleSided.Value, true);
            
            if (WriteColor.HasValue)
                SetWriteColor(WriteColor.Value, true);
            
            if (ActiveProgram.HasValue)
                SetActiveProgram(ActiveProgram.Value);
            
            if (Wireframe.HasValue)
                SetWireframe(Wireframe.Value, true);
            
            if (Alpha.HasValue)
                SetAlphaMode(Alpha.Value, true);

            if (LineWidth.HasValue)
                SetLineWidth(LineWidth.Value, true);

            if (CullFace.HasValue)
                SetCullFace(CullFace.Value, true);

            if (ClearDepth.HasValue)
                SetClearDepth(ClearDepth.Value, true);

            if (ClearColor.HasValue)
                SetClearColor(ClearColor.Value, true);

            foreach (var feature in Features)
                EnableFeature(feature.Key, feature.Value, true);

            foreach (var texture in TexturesTargets)
                BindTexture(texture.Key, texture.Value, true);

            //ActiveTexture

            /*
            foreach (var texture in Textures)
                BindTexture(texture.Value, texture.Key, true);  
            */
        }

        public void SetClearColor(Color color, bool force = false)
        {
            if (ClearColor != color || force)
            {
                _gl.ClearColor(color.R, color.G, color.B, color.A);
                ClearColor = color;
            }
        }

        public void SetClearDepth(float value, bool force = false)
        {
            if (ClearDepth != value || force)
            {
                _gl.ClearDepth(value);
                ClearDepth = value;
            }
        }

        public bool SetActiveProgram(uint program, bool force = false)
        {
            if (ActiveProgram != program || force)
            {
                _gl.UseProgram(program);
                ActiveProgram = program;
                return true;
            }
            return false;
        }

        public void SetView(Rect2I value, bool force = false)
        {
            if (View == null || !View.Equals(value) || force)
            {
                _gl.Viewport(value.X, value.Y, value.Width, value.Height);
                _gl.Scissor(value.X, value.Y, value.Width, value.Height);

                View = value;
            }
        }

        public void BindTexture(TextureTarget target, uint texId, bool force = true)
        {
            if (!TexturesTargets.TryGetValue(target, out var value) || value != texId || force)
            {
                _gl.BindTexture(target, texId);

                TexturesTargets[target] = texId;

                if (ActiveTexture != null)
                    TexturesSlots[ActiveTexture.Value] = texId;
            }
            return;
        }

        public void SetActiveTexture(uint texId, TextureTarget target, int slot, bool force = true)
        {
            if (TexturesSlots.TryGetValue(slot, out var id) && id == texId && !force)
                return;

            if (ActiveTexture != slot || force)
            {
                _gl.ActiveTexture(TextureUnit.Texture0 + slot);
                ActiveTexture = slot;
            }

            BindTexture(target, texId, force);
        }

        public void SetActiveTexture(GlTexture glTex, int slot, bool force = false)
        {
            SetActiveTexture(glTex.Handle, glTex.Target, slot, force);   
        }

        public void EnableFeature(EnableCap cap, bool value, bool force = false)
        {
            if (Features.TryGetValue(cap, out var enabled) && enabled == value && !force)
                return;

            if (value)
                _gl.Enable(cap);
            else
                _gl.Disable(cap);

            Features[cap] = value;  
        }

        public void SetUseDepth(bool value, bool force = false)
        {
            if (value != UseDepth || force)
            {
                if (!value)
                    _gl.DepthFunc(DepthFunction.Always);
                else
                    _gl.DepthFunc(DepthFunction.Lequal);

                UseDepth = value;
            }
        }

        public void SetDoubleSided(bool value, bool force = false)
        {
            if (DoubleSided != value || force)
            {
                EnableFeature(EnableCap.CullFace, !value);
                DoubleSided = value;
            }
        }

        public void SetWriteDepth(bool value, bool force = false)
        {
            if (WriteDepth != value || force)
            {
                _gl.DepthMask(value);
                WriteDepth = value;
            }
        }

        public void SetAlphaMode(AlphaMode value, bool force = false)
        {
            if (Alpha != value || force)
            {
                EnableFeature(EnableCap.Blend, value != AlphaMode.Opaque);
                Alpha = value;
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
        }

        public void SetWriteColor(bool value, bool force = false)
        {
            if (WriteColor != value || force)
            {
                if (!value)
                    _gl.ColorMask(false, false, false, false);
                else
                    _gl.ColorMask(true, true, true, true);

                WriteColor = value;
            }
        }

        public void SetLineWidth(float value, bool force = false)
        {
            if (LineWidth != value || force)
            {
                _gl.LineWidth(value);
                LineWidth = value;
            }
        }

        public void SetCullFace(TriangleFace value, bool force = false)
        {
            if (CullFace != value || force)
            {
                _gl.CullFace(value);
                CullFace = value;
            }
        }

        public void SetWireframe(bool value, bool force = false)
        {
#if !GLES
            if (value != Wireframe || force)
            {
                if (value)
                    _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
                else
                {
                    _gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
                    _gl.CullFace(TriangleFace.Back);
                }
                Wireframe = value;
            }
#endif
        }

        public float? ClearDepth;

        public TriangleFace? CullFace;

        public bool? WriteDepth;
        
        public bool? UseDepth;
        
        public bool? DoubleSided;
        
        public bool? WriteColor;
        
        public uint? ActiveProgram;
        
        public bool? Wireframe;
        
        public AlphaMode? Alpha;
        
        public Rect2I? View;

        public float? LineWidth;

        public Color? ClearColor;

        public int? ActiveTexture;    

        public readonly Dictionary<EnableCap, bool> Features = [];

        public readonly Dictionary<int, uint> TexturesSlots = [];

        public readonly Dictionary<TextureTarget, uint> TexturesTargets = [];
    }
}
