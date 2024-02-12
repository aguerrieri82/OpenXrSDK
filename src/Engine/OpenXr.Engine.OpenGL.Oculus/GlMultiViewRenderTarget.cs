﻿#if GLES
using Silk.NET.OpenGLES;
#else
using Silk.NET.OpenGL;
#endif

using OpenXr.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace OpenXr.Engine.OpenGL.Oculus
{

    public struct SceneMatrices
    {
        public Matrix4x4 View1;
        public Matrix4x4 View2;
        public Matrix4x4 Projection1;
        public Matrix4x4 Projection2;

    }

    public class GlMultiViewRenderTarget : GlTextureRenderTarget, IGlProgramFactory, IMultiViewTarget
    {
        SceneMatrices _matrices;

        protected GlMultiViewRenderTarget(GL gl, uint textId, uint sampleCount)
            : base(gl, textId, sampleCount)
        {
        }

        protected override GlFrameBuffer CreateFrameBuffer(uint texId, uint sampleCount)
        {
            return new GlMultiViewFrameBuffer(_gl, texId, sampleCount);
        }

        public static GlMultiViewRenderTarget Attach(GL gl, uint texId, uint sampleCount)
        {
            if (!_targets.TryGetValue(texId, out var target))
            {
                target = new GlMultiViewRenderTarget(gl, texId, sampleCount);
                _targets[texId] = target;
            }

            return (GlMultiViewRenderTarget)target;
        }

        public void SetCameraTransforms(XrCameraTransform[] eyes)
        {
            _matrices.Projection1 = eyes[0].Projection;
            _matrices.Projection2 = eyes[1].Projection;
            _matrices.View1 = eyes[0].View;
            _matrices.View2 = eyes[1].View;
        }

        public GlProgram CreateProgram(GL gl, string vSource, string fSource, GlRenderOptions options)
        {
            options.ShaderExtensions ??= [];

            if (!options.ShaderExtensions.Contains("GL_OVR_multiview2"))
                options.ShaderExtensions.Add("GL_OVR_multiview2");

            return new GlMultiViewProgram(gl, () => _matrices, vSource, fSource, options);    
        }
    }
}
