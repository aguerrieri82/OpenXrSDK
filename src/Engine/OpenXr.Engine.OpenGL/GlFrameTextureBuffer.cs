﻿using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenXr.Engine.OpenGL
{
    public class GlFrameTextureBuffer : GlFrameBuffer
    {
        public GlFrameTextureBuffer(GL gl, GlTexture2D color)
            : base(gl)
        {
            _handle = _gl.GenFramebuffer();

            Color = color;
            CreateDepth();
            //Attach();
        }

        [MemberNotNull(nameof(Depth))]
        protected void CreateDepth()
        {
            Depth = new GlTexture2D(_gl);
            Depth.MagFilter = Color.MagFilter;
            Depth.MinFilter = Color.MinFilter;
            Depth.WrapT = Color.WrapT;
            Depth.WrapS = Color.WrapS;
            Depth.MaxLevel = Color.MaxLevel;
            Depth.BaseLevel = Color.BaseLevel;
            Depth.Create(Color.Width, Color.Height, TextureFormat.Depth24Float);
        }

        public override void Bind()
        {
            _gl.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _handle);

            _gl.FramebufferTexture2D(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                Color, 0);

            _gl.DrawBuffer(DrawBufferMode.ColorAttachment0);

            _gl.FramebufferTexture2D(
                FramebufferTarget.DrawFramebuffer,
                FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D,
                Depth, 0);

            var status = _gl.CheckFramebufferStatus(FramebufferTarget.DrawFramebuffer);

            if (status != GLEnum.FramebufferComplete)
            {
                throw new Exception($"Frame buffer state invalid: {status}");
            }
        }

        public void InvalidateDepth()
        {
            _gl.InvalidateFramebuffer(FramebufferTarget.DrawFramebuffer, [InvalidateFramebufferAttachment.DepthAttachment]);
        }

        public GlTexture2D Color;

        public GlTexture2D Depth;

    }
}
