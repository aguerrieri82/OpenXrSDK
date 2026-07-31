#if GLES
using Silk.NET.OpenGLES;
using GlStencilFunction = Silk.NET.OpenGLES.StencilFunction;
#else
using Silk.NET.OpenGL;
using GlStencilFunction = Silk.NET.OpenGL.StencilFunction;
#endif

using XrMath;
using System.Runtime.CompilerServices;



namespace XrEngine.OpenGL
{
    public class GlState
    {
        [ThreadStatic]
        static GlState? _current;

        private readonly GL _gl;
        private readonly bool _hasShadingRate;
        private bool _stencilDirty;

        public GlState(GL gl)
        {
            _gl = gl;
            _hasShadingRate = _gl.IsExtensionPresent("GL_EXT_fragment_shading_rate");
            _current = this;
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
            ActiveShadingRate = null;
            TexturesSlots.Clear();
            BufferSlots.Clear();

            for (var i = 0; i < SamplerSlots.Length; i++)
                SamplerSlots[i] = 0;

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

            if (ActiveShadingRate != null)
                SetShadingRate(ActiveShadingRate.Value, true);

            for (var i = 0; i < SamplerSlots.Length; i++)
                BindSampler(SamplerSlots[i], i, true);
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
                res = new uint[MAX_TEX_SLOTS];
                
                Array.Fill(res, uint.MaxValue);

                TexturesSlots[target] = res;
            }
            return res;
        }


        public void BindTexture(TextureTarget target, uint texId, bool force = false)
        {
            ActiveTexture ??= (_gl.GetInteger(GetPName.ActiveTexture) - (int)GLEnum.Texture0);

            var slots = GetTextureSlots(target);

            var curSlotValue = slots[ActiveTexture.Value];

            if (curSlotValue != texId || !force)
            {
                _gl.BindTexture(target, texId);
                slots[ActiveTexture.Value] = texId;
            }

            if (EnableDebug)
            {
                var realTex = _gl.GetActiveTextureBinding(target);
                if (realTex != texId)
                    Log.Warn(this, "Inconsistent TEX cache for {0} - slot {3}: Found {1} - Expected {2}", target, realTex, texId, ActiveTexture);
            }
        }

        public void SetActiveTexture(int slot, bool force = false)
        {
            if (ActiveTexture != slot || force)
            {
                _gl.ActiveTexture(TextureUnit.Texture0 + slot);
                ActiveTexture = slot;
            }

            if (EnableDebug)
            {
                var realActive = (_gl.GetInteger(GetPName.ActiveTexture) - (int)GLEnum.Texture0);
                if (realActive != slot)
                    Log.Warn(this, "Inconsistent ACTIVE-TEX cache: Found {0} - Expected {1}", realActive, slot);
            }
        }

        public void LoadTexture(uint texId, TextureTarget target, int slot, bool force = false)
        {
            SetActiveTexture(slot, force);
            BindTexture(target, texId, force);
        }

        public void LoadTexture(GlTexture glTex, int slot, bool force = false)
        {
            LoadTexture(glTex.Handle, glTex.Target, slot, force);
            
            if (glTex.Sampler != null)
                BindSampler(glTex.Sampler, slot);
            else
                BindSampler(0, slot);

            glTex.Slot = slot;
        }

        public void SetShadingRate(int rate, bool force = false)
        {
            if (!_hasShadingRate)
                return;

            if (rate != ActiveShadingRate || force)
            {
                var realRate = rate switch
                {
                    1 => ShadingRate.Rate1X1PixelsExt,
                    2 => ShadingRate.Rate2X2PixelsExt,
                    4 => ShadingRate.Rate4X4PixelsExt,
                    _ => throw new NotSupportedException()
                };
                _gl.ShadingRateExt.ShadingRate(realRate);
            }

            ActiveShadingRate = rate;
        }

        public bool IsFeatureEnabled(EnableCap cap, bool useCache = true)
        {
            if (!useCache || !Features.TryGetValue(cap, out var value))
            {
                value = _gl.IsEnabled(cap);
                Features[cap] = value;
            }

            return value;
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
                    else if (value == AlphaMode.Min)
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
            bool changed;

            if (target == FramebufferTarget.Framebuffer)
            {
                changed =
                    !FrameBufferTargets.TryGetValue(FramebufferTarget.ReadFramebuffer, out var read) || read != value ||
                    !FrameBufferTargets.TryGetValue(FramebufferTarget.DrawFramebuffer, out var draw) || draw != value;
            }
            else
            {
                changed = !FrameBufferTargets.TryGetValue(target, out var current) || current != value;
            }

            if (!changed && !force)
                return;

            _gl.BindFramebuffer(target, value);

            switch (target)
            {
                case FramebufferTarget.Framebuffer:
                    FrameBufferTargets[FramebufferTarget.Framebuffer] = value;
                    FrameBufferTargets[FramebufferTarget.ReadFramebuffer] = value;
                    FrameBufferTargets[FramebufferTarget.DrawFramebuffer] = value;
                    break;

                case FramebufferTarget.DrawFramebuffer:
                    FrameBufferTargets[FramebufferTarget.DrawFramebuffer] = value;

                    // GL_FRAMEBUFFER_BINDING aliases GL_DRAW_FRAMEBUFFER_BINDING.
                    FrameBufferTargets[FramebufferTarget.Framebuffer] = value;
                    break;

                case FramebufferTarget.ReadFramebuffer:
                    FrameBufferTargets[FramebufferTarget.ReadFramebuffer] = value;
                    break;
            }
        }

        public void BindBuffer(BufferTargetARB target, uint value, bool force = false)
        {
            if (!BufferTargets.TryGetValue(target, out var cur) || cur != value || force)
            {
                BufferTargets[target] = value;

                _gl.BindBuffer(target, value);
            }

            if (EnableDebug)
            {
                var realActive = _gl.GetActiveBufferBinding(target);
                if (realActive != value)
                    Log.Warn(this, "Inconsistent BUF cache for {0}: Found {1} - Expected {2}", target, realActive, value);
            }
        }

        public void BindSampler(GlSampler sampler, int slot, bool force = false)
        {
            BindSampler(sampler.Handle, slot, force);
            sampler.Slot = slot;
        }

        public void BindSampler(uint samplerId, int slot, bool force = false)
        {
            if (force || SamplerSlots[slot] != samplerId)
            {
                _gl.BindSampler((uint)slot, samplerId);
                SamplerSlots[slot] = samplerId;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint[] GetBufferSlots(BufferTargetARB target)
        {
            if (!BufferSlots.TryGetValue(target, out var res))
            {
                res = new uint[MAX_BUFFER_SLOTS];
                BufferSlots[target] = res;
            }

            return res;
        }

        public void RemoveTextureRef(uint handle)
        {
            foreach (var slots in TexturesSlots)
            {
                for (var i = 0; i < slots.Value.Length; i++)
                {
                    if (slots.Value[i] == handle)
                        slots.Value[i] = 0;
                }
            }
        }

        public void RemoveBufferRef(uint handle)
        {
            foreach (var slots in BufferSlots)
            {
                for (var i = 0; i < slots.Value.Length; i++)
                {
                    if (slots.Value[i] == handle)
                    {
                        slots.Value[i] = 0;
                        _gl.BindBufferBase(slots.Key, (uint)i, 0);
                    }

                }
            }

            var targets = BufferTargets.Keys;

            foreach (var key in targets)
            {
                if (BufferTargets[key] == handle)
                {
                    _gl.BindBuffer(key, 0);
                    BufferTargets[key] = 0;
                }
     
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LoadBufferRange(IGlBuffer buffer, int slot, int offset, uint sizeBytes)
        {
            LoadBufferRange(buffer, slot, buffer.Target, offset, sizeBytes);
        }


        public void LoadBufferRange(IGlBuffer buffer, int slot, BufferTargetARB target, int offset, uint sizeBytes)
        {
            var slots = GetBufferSlots(target);

            var curSlotValue = slots[slot];

            BufferTargets[target] = buffer.Handle;

            _gl.BindBufferRange(target, (uint)slot, buffer.Handle, offset, sizeBytes);

            buffer.ActiveSlot = slot;

            slots[slot] = buffer.Handle;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LoadBuffer(IGlBuffer buffer, int slot, bool force = false)
        {
            LoadBuffer(buffer, slot, buffer.Target, force);
        }

        public void LoadBuffer(IGlBuffer buffer, int slot, BufferTargetARB target, bool force = false)
        {
            var slots = GetBufferSlots(target);

            var curSlotValue = slots[slot];

            if (curSlotValue == buffer.Handle && !force)
                return;

            BufferTargets[target] = buffer.Handle;

            _gl.BindBufferBase(target, (uint)slot, buffer.Handle);

            buffer.ActiveSlot = slot;

            slots[slot] = buffer.Handle;
        }

        public void ResetTextures()
        {
            TexturesSlots.Clear();
            ActiveTexture = null;
        }

        public void ConfigureCaps(ShaderMaterial material)
        {
            SetCullFace(material.CullFront ? TriangleFace.Front : TriangleFace.Back);
            SetUseDepth(material.UseDepth);
            SetWriteDepth(material.WriteDepth);
            SetDoubleSided(material.DoubleSided);
            SetWriteColor(material.WriteColor);
            SetAlphaMode(material.Alpha);
            SetWireframe(material is WireframeMaterial);

            SetStencilFunc((GlStencilFunction)material.StencilFunction);
            SetWriteStencil(material.WriteStencil);
            SetStencilRef(material.CompareStencilMask);

            EnableFeature(EnableCap.ClipDistance0, material.UseClipDistance);

            if (material.PolygonOffset.Y != 0)
            {
                EnableFeature(EnableCap.PolygonOffsetFill, true);
                _gl.PolygonOffset(material.PolygonOffset.X, material.PolygonOffset.Y);
            }
            else
                EnableFeature(EnableCap.PolygonOffsetFill, false);

            if (material is ILineMaterial line)
                SetLineWidth(line.LineWidth);

            Commit();
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

        public int? ActiveShadingRate;





        public readonly Dictionary<EnableCap, bool> Features = [];

        public readonly Dictionary<TextureTarget, uint[]> TexturesSlots = [];

        public readonly Dictionary<BufferTargetARB, uint[]> BufferSlots = [];

        public readonly Dictionary<FramebufferTarget, uint> FrameBufferTargets = [];

        public readonly Dictionary<BufferTargetARB, uint> BufferTargets = [];

        public readonly uint[] SamplerSlots = new uint[MAX_TEX_SLOTS];

        public static GlState Current => _current ?? throw new InvalidOperationException("No current state for this thread");

        public static readonly DrawBufferMode[] DRAW_COLOR_0 = [DrawBufferMode.ColorAttachment0];

        public static readonly DrawBufferMode[] DRAW_BACK = [DrawBufferMode.Back];

        public static readonly DrawBufferMode[] DRAW_NONE = [DrawBufferMode.None];

        public const int MAX_TEX_SLOTS = 64;

        public const int MAX_BUFFER_SLOTS = 64;

        public bool EnableDebug = false;
    }
}
