#if GLES
using Silk.NET.OpenGLES;
using GlStencilFunction = Silk.NET.OpenGLES.StencilFunction;
#else
using Silk.NET.OpenGL;
using GlStencilFunction = Silk.NET.OpenGL.StencilFunction;
#endif

using XrMath;

namespace XrEngine.OpenGL
{
    public class GlState
    {
        private readonly GL _gl;
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
            ColorMask = null;
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
            FrameBufferTargets.Clear();
            BufferTargets.Clear();
            Features.Clear();
            VertexArray = null;
            TexturesSlots.Clear();
            BufferSlots.Clear();
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

            if (ColorMask.HasValue)
                SetColorMask((ColorMask.Value & 1) != 0, (ColorMask.Value & 2) != 0, (ColorMask.Value & 4) != 0, (ColorMask.Value & 8) != 0, true);

            if (ActiveProgram.HasValue)
                SetActiveProgram(ActiveProgram.Value, true);

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

            if (VertexArray.HasValue)
                BindVertexArray(VertexArray.Value, true);

            foreach (var feature in Features)
                EnableFeature(feature.Key, feature.Value, true);

            foreach (var fb in FrameBufferTargets)
                BindFrameBuffer(fb.Key, fb.Value, true);

            foreach (var fb in BufferTargets)
                BindBuffer(fb.Key, fb.Value, true);

            if (WriteStencil.HasValue)
                SetWriteStencil(WriteStencil.Value, true);

            if (StencilRef.HasValue)
                SetStencilRef(StencilRef.Value, true);

            if (StencilFunc.HasValue)
                SetStencilFunc(StencilFunc.Value, true);
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

                //_gl.Scissor(value.X, value.Y, value.Width, value.Height);

                View = value;
            }
        }

        public void BindVertexArray(uint value, bool force = false)
        {
            if (VertexArray != value || force)
            {
                _gl.BindVertexArray(value);
                VertexArray = value;
            }
        }

        uint[] GetTextureSlots(TextureTarget target)
        {
            if (!TexturesSlots.TryGetValue(target, out var res))
            {
                res = new uint[32];
                TexturesSlots[target] = res;
            }
            return res;
        }

        public void BindTexture(TextureTarget target, uint texId, bool force = false)
        {
            ActiveTexture ??= (_gl.GetInteger(GetPName.ActiveTexture) - (int)GLEnum.Texture0);

            var slots = GetTextureSlots(target);

            var curSlotValue = slots[ActiveTexture.Value];

            if (curSlotValue == texId && !force)
                return;

            _gl.BindTexture(target, texId);

            slots[ActiveTexture.Value] = texId;
        }

        public void SetActiveTexture(int slot, bool force = false)
        {
            if (ActiveTexture != slot || force)
            {
                _gl.ActiveTexture(TextureUnit.Texture0 + slot);
                ActiveTexture = slot;
            }
        }

        public void LoadTexture(uint texId, TextureTarget target, int slot, bool force = false)
        {
            var curSlotValue = GetTextureSlots(target)[slot];

            if (curSlotValue == texId && !force)
                return;

            SetActiveTexture(slot, force);

            BindTexture(target, texId, force);
        }

        public void LoadTexture(GlTexture glTex, int slot, bool force = false)
        {
            LoadTexture(glTex.Handle, glTex.Target, slot, force);
            glTex.Slot = slot;
        }

        public bool IsFeatureEnabled(EnableCap cap, bool defValue = false)
        {
            if (Features.TryGetValue(cap, out var value))
                return value;
            return defValue;
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
            EnableFeature(EnableCap.CullFace, !value);
        }

        public void SetWriteDepth(bool value, bool force = false)
        {
            if (WriteDepth != value || force)
            {
                _gl.DepthMask(value);
                WriteDepth = value;
            }
        }

        public void Commit()
        {
            UpdateStencil();

            EnableFeature(EnableCap.DepthTest, WriteDepth == true || UseDepth == true);
        }

        public void SetAlphaMode(AlphaMode value, bool force = false)
        {
            if (Alpha != value || force)
            {
                Alpha = value;

                EnableFeature(EnableCap.Blend, (value & AlphaMode.Opaque) == 0);
                //EnableFeature(EnableCap.SampleAlphaToCoverage, value == AlphaMode.Mask);

                if (value != AlphaMode.Opaque)
                {
                    if (value == AlphaMode.Add)
                    {
                        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
                        _gl.BlendFunc(BlendingFactor.One, BlendingFactor.One);
                    }
                    else if(value == AlphaMode.Min)
                    {
                        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        _gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.Min);
                    }
                    else if (value == AlphaMode.Max)
                    {
                        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                        _gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.Max);
                    }
                    else if (value == AlphaMode.Punch)
                    {
                        _gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,
                                              BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);

                        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
                    }
                    else if (value == AlphaMode.Over)
                    {
                        _gl.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.Zero,
                                              BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);

                        _gl.BlendEquationSeparate(BlendEquationModeEXT.FuncAdd, BlendEquationModeEXT.Max);

                    }
                    else
                    {
                        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);

                        _gl.BlendFuncSeparate(
                            BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,
                            BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                    }

                }
          
            }
        }

        public void SetColorMask(bool r, bool g, bool b, bool a, bool force = false)
        {
            var mask = (r ? 1 : 0) + ((g ? 1 : 0) << 1) + ((b ? 1 : 0) << 2) + ((a ? 1 : 0) << 3);
            if (ColorMask != mask || force)
            {
                _gl.ColorMask(r, g, b, a);
                ColorMask = mask;
            }
        }

        public void SetWriteColor(bool value, bool force = false)
        {
            SetColorMask(value, value, value, value, force);
        }

        public void SetLineWidth(float value, bool force = true)
        {
            if (LineWidth != value || force)
            {
                _gl.LineWidth(value);
                EnableFeature(EnableCap.LineSmooth, true);
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

        public uint GetActiveFrameBuffer(FramebufferTarget target)
        {
            return FrameBufferTargets[target];
        }

        public void BindFrameBuffer(FramebufferTarget target, uint value, bool force = false)
        {
            if (!FrameBufferTargets.TryGetValue(target, out var cur) || cur != value || force)
            {
                FrameBufferTargets[target] = value;

                if (target == FramebufferTarget.Framebuffer)
                {
                    FrameBufferTargets[FramebufferTarget.ReadFramebuffer] = value;
                    FrameBufferTargets[FramebufferTarget.DrawFramebuffer] = value;
                }
                else if (FrameBufferTargets.TryGetValue(FramebufferTarget.Framebuffer, out var curValue) && curValue != value)
                    FrameBufferTargets[FramebufferTarget.Framebuffer] = 0;


                _gl.BindFramebuffer(target, value);
            }
        }

        public void BindBuffer(BufferTargetARB target, uint value, bool force = false)
        {
            if (!BufferTargets.TryGetValue(target, out var cur) || cur != value || force)
            {
                BufferTargets[target] = value;

                _gl.BindBuffer(target, value);
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
                    _gl.StencilFunc((GLEnum)StencilFunc.Value, StencilRef.Value, StencilRef.Value);
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
                }
                Wireframe = value;
            }
#endif
        }

        uint[] GetBufferSlots(BufferTargetARB target)
        {
            if (!BufferSlots.TryGetValue(target, out var res))
            {
                res = new uint[32];
                BufferSlots[target] = res;
            }

            return res;
        }

        public void SetActiveBuffer(IGlBuffer buffer, int slot, bool force = false)
        {
            SetActiveBuffer(buffer, slot, buffer.Target, force);
        }

        public void SetActiveBuffer(IGlBuffer buffer, int slot, BufferTargetARB target, bool force = false)
        {
            var slots = GetBufferSlots(target);

            var curSlotValue = slots[slot];

            if (curSlotValue == buffer.Handle && !force)
                return;

            _gl.BindBufferBase(target, (uint)slot, buffer.Handle);
            buffer.ActiveSlot = slot;

            slots[slot] = buffer.Handle;
        }

        public void ResetTextures()
        {
            TexturesSlots.Clear();
            ActiveTexture = null;
        }

        public float? ClearDepth;

        public Color? ClearColor;

        public byte? ClearStencil;

        public TriangleFace? CullFace;

        public bool? WriteDepth;

        public bool? UseDepth;

        public int? ColorMask;

        public uint? ActiveProgram;

        public bool? Wireframe;

        public AlphaMode? Alpha;

        public Rect2I? View;

        public float? LineWidth;

        public int? ActiveTexture;

        public byte? WriteStencil;

        public byte? StencilRef;

        public GlStencilFunction? StencilFunc;

        public uint? VertexArray;

        public readonly Dictionary<EnableCap, bool> Features = [];

        public readonly Dictionary<TextureTarget, uint[]> TexturesSlots = [];

        public readonly Dictionary<BufferTargetARB, uint[]> BufferSlots = [];

        public readonly Dictionary<FramebufferTarget, uint> FrameBufferTargets = [];

        public readonly Dictionary<BufferTargetARB, uint> BufferTargets = [];

        [ThreadStatic]
        public static GlState? Current;

        public static readonly DrawBufferMode[] DRAW_COLOR_0 = [DrawBufferMode.ColorAttachment0];

        public static readonly DrawBufferMode[] DRAW_BACK = [DrawBufferMode.Back];

        public static readonly DrawBufferMode[] DRAW_NONE = [DrawBufferMode.None];

    }
}
