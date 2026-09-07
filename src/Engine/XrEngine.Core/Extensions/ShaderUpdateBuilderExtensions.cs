using System.Text;

namespace XrEngine
{
    public static class ShaderUpdateBuilderExtensions
    {
        extension(ShaderUpdateBuilder self)
        {
            public void SetIncludesSlot(string name, params string[] includes)
            {
                self.SetSlot(name, () =>
                {
                    var sb = new StringBuilder();

                    foreach (var include in includes)
                        sb.Append("#include \"").Append(include).AppendLine("\"");

                    return sb.ToString();
                });
            }

            public void SetVsIncludes(params string[] includes)
            {
                self.SetIncludesSlot(ShaderSlots.VertexIncludes, includes);
            }

            public void SetFsIncludes(params string[] includes)
            {
                self.SetIncludesSlot(ShaderSlots.FragmentIncludes, includes);
            }

            public void SetFragmentLoader(string code)
            {
                self.SetSlot(ShaderSlots.FragmentLoader, code);
            }

            public void SetVertexLocalTransform(string code)
            {
                self.SetSlot(ShaderSlots.VertexLocalTransforms, code);
            }

            public void SetSlot(string name, string code)
            {
                self.SetSlot(name, () => code);
            }

            public void LoadCameraBuffer()
            {
                self.LoadBuffer<CameraUniforms>((ctx, ref update) =>
                {
                    System.Diagnostics.Debug.Assert(ctx.PassCamera != null);

                    var camera = ctx.PassCamera;

                    update.Value = new CameraUniforms
                    {
                        Exposure = camera.Exposure,
                        ActiveEye = camera.ActiveEye,
                        ViewSize = camera.ViewSize,
                        NearPlane = camera.Near,
                        FarPlane = camera.Far,
                        FrustumPlane1 = ctx.FrustumPlanes[0],
                        FrustumPlane2 = ctx.FrustumPlanes[1],
                        FrustumPlane3 = ctx.FrustumPlanes[2],
                        FrustumPlane4 = ctx.FrustumPlanes[3],
                        FrustumPlane5 = ctx.FrustumPlanes[4],
                        FrustumPlane6 = ctx.FrustumPlanes[5],
                        View = camera.View,
                        Proj = camera.Projection
                    };

                    if (camera.Eyes == null)
                    {
                        update.Value.Eyes[0] = new CameraViewUniforms
                        {
                            ViewProj = camera.ViewProjection,
                            Position = camera.WorldPosition,
                            ViewProjInv = camera.ViewProjectionInverse
                        };
                    }
                    else
                    {
                        for (var i = 0; i < 2; i++)
                        {
                            ref readonly var eye = ref camera.Eyes[i];
                            update.Value.Eyes[i].ViewProj = eye.ViewProj;
                            update.Value.Eyes[i].Position = eye.World.Translation;
                            update.Value.Eyes[i].ViewProjInv = eye.ViewProjInv;
                        }
                    }

                    var light = ctx.ShadowMapProvider?.LightCamera?.ViewProjection;
                    if (light != null)
                        update.Value.LightSpaceMatrix = light.Value;

                    return true;

                }, UniformsSlots.Camera, BufferStore.Shader);
            }
        }

    }
}
