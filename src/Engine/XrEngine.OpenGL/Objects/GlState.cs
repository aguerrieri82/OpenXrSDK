#if GLES
using Silk.NET.OpenGLES;
using GlStencilFunction = Silk.NET.OpenGL.StencilFunction;  
#else
using Silk.NET.OpenGL;
using GlStencilFunction = Silk.NET.OpenGL.StencilFunction;    
#endif



using XrMath;

namespace XrEngine.OpenGL
{
    public class GlState
    {
        private GL _gl;
        private bool _stencilDirty;

        public GlState(GL gl)
        {
            _gl = gl;
            Current = this; 
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
            ClearStencil = null;
            ActiveTexture = null;
            WriteStencil = null;
            StencilFunc = null;
            StencilRef = null;
            FrameBuffer = null;
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

            if (ClearStencil.HasValue)
                SetClearStencil(ClearStencil.Value, true);

            if (ClearColor.HasValue)
                SetClearColor(ClearColor.Value, true);

            foreach (var feature in Features)
                EnableFeature(feature.Key, feature.Value, true);

            foreach (var texture in TexturesTargets)
                BindTexture(texture.Key, texture.Value, true);

            if (WriteStencil.HasValue)
                SetWriteStencil(WriteStencil.Value, true);

            if (StencilRef.HasValue)
                SetStencilRef(StencilRef.Value, true);

            if (StencilFunc.HasValue)
                SetStencilFunc(StencilFunc.Value, true);

            if (FrameBuffer.HasValue)
                BindFrameBuffer(FrameBuffer.Value, true);

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

        public void SetClearStencil(byte value, bool force = false)
        {
            if (ClearStencil != value || force)
            {
                _gl.ClearStencil(value);
                ClearStencil = value;
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

        public void BindTexture(TextureTarget target, uint texId, bool force = false)
        {
            if (!TexturesTargets.TryGetValue(target, out var value) || value != texId || force)
            {
                _gl.BindTexture(target, texId);

                TexturesTargets[target] = texId;
            }
            else
            {
                /*
                int curBound = 0;

                if (target == TextureTarget.Texture2D)
                    _gl.GetInteger(GetPName.TextureBinding2D, out curBound);

                if (target == TextureTarget.TextureCubeMap)
                    _gl.GetInteger(GetPName.TextureBindingCubeMap, out curBound);

                Debug.Assert(curBound == texId);
                */
            }

            if (ActiveTexture != null)
                TexturesSlots[ActiveTexture.Value] = texId;

            return;
        }

        public void SetActiveTexture(uint texId, TextureTarget target, int slot, bool force = false)
        {
            bool forceBind = force;

            if (ActiveTexture != slot || force)
            {
                _gl.ActiveTexture(TextureUnit.Texture0 + slot);
                ActiveTexture = slot;
                forceBind = true;   
            }

            BindTexture(target, texId, forceBind);
        }

        public void SetActiveTexture(GlTexture glTex, int slot, bool force = false)
        {
            SetActiveTexture(glTex.Handle, glTex.Target, slot, force);
            glTex.Slot = slot;
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

        public void SetWriteStencil(byte? value, bool force = false)
        {
            if (WriteStencil != value || force)
            {
                WriteStencil = value;
                _stencilDirty = true;

            }
        }

        public void SetStencilRef(byte? value, bool force = false)
        {
            if (StencilRef != value || force)
            {
                StencilRef = value;
                _stencilDirty = true;
        
            }
        }

        public void SetStencilFunc(GlStencilFunction value, bool force = false)
        {
            if (value != StencilFunc || force)
            {
                StencilFunc = value;
                _stencilDirty = true;
            }
        }

        public void BindFrameBuffer(uint value, bool force = false)
        {
            if (FrameBuffer != value || force)
            {
                FrameBuffer = value;
                _gl.BindFramebuffer(FramebufferTarget.Framebuffer, value);
            }
        }

        public void UpdateStencil()
        {
            if (!_stencilDirty)
                return;
            
            _stencilDirty = false;

            if ((StencilFunc == null || StencilRef == null) && WriteStencil == null)
            {
                EnableFeature(EnableCap.StencilTest, false);
            }
            else
            {
                EnableFeature(EnableCap.StencilTest, true);

                if (StencilFunc == null || StencilRef == null)
                {
                    _gl.StencilOp(StencilOp.Keep, StencilOp.Replace, StencilOp.Replace);
                    _gl.StencilFunc(GLEnum.Always, WriteStencil!.Value, 0xFF);
                }
                else
                {
                    _gl.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Keep);
                    _gl.StencilFunc((GLEnum)StencilFunc.Value, StencilRef.Value, 0xFF);
                }
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

        public Color? ClearColor;

        public byte? ClearStencil;

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

        public int? ActiveTexture;

        public byte? WriteStencil;

        public byte? StencilRef;

        public GlStencilFunction? StencilFunc;

        public uint? FrameBuffer;

        public readonly Dictionary<EnableCap, bool> Features = [];

        public readonly Dictionary<int, uint> TexturesSlots = [];

        public readonly Dictionary<TextureTarget, uint> TexturesTargets = [];

        [ThreadStatic]
        public static GlState? Current;

        public static readonly DrawBufferMode[] DRAW_COLOR_0 = [DrawBufferMode.ColorAttachment0];

        public static readonly DrawBufferMode[] DRAW_NONE = [DrawBufferMode.None];

    }
}
