using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.Lighting
{
    public class LightFieldProvider : Behavior<Group3D>, ILightFieldProvider
    {
        string? _profile;
        VoxelGridDesc _grid;
        GpuMeshVoxelizer? _gpuVoxelizer;
        VoxelLightBaker _backer;
        Bounds3 _lastBounds;
        VoxelLightFieldView? _lightField;
        readonly LightFieldData _fieldData;
        float _voxelSize;
        double _lastBuildTime;

        public LightFieldProvider()
        {
            _backer = new VoxelLightBaker();
            _fieldData = new LightFieldData();
            MaxUpdateInterval = TimeSpan.FromSeconds(5);
        }

        public LightFieldData GetLightField()
        {
            return _fieldData;
        }

        protected override void Update(RenderContext ctx)
        {
            if ((ctx.Time - _lastBuildTime) >= MaxUpdateInterval.TotalSeconds)
            {
                Rebuild();
                _lastBuildTime = ctx.Time;
            }

            base.Update(ctx);
        }

        public void Rebuild()
        {
            _gpuVoxelizer ??= new GpuMeshVoxelizer(OpenGLRender.Current!.GL);

            bool meshDirty = false;

            var bounds = new Bounds3Builder();

            foreach (var mesh in _host!.Descendants<TriangleMesh>())
            {
                bounds.Add(mesh.WorldBounds);

                if (!mesh.TryComponent<LightFieldReceiver>(out var rec))
                    continue;

                if (rec.NeedUpdate)
                    meshDirty = true;
            }

            bool gridDirty = false;

            var curBounds = bounds.Result;

            if (curBounds != _lastBounds || _fieldData.VoxelSize != _voxelSize)
            {
                _grid = new VoxelGridDesc
                {
                    Origin = curBounds.Min,
                    VoxelSize = _voxelSize,
                    Size = new Vector3I(
                        (int)Math.Ceiling(curBounds.Size.X / _voxelSize),
                        (int)Math.Ceiling(curBounds.Size.Y / _voxelSize),
                        (int)Math.Ceiling(curBounds.Size.Z / _voxelSize))
                };

                _backer.SetGrid(_grid);
                _gpuVoxelizer.SetGrid(_grid);   

                _lastBounds = curBounds;

                _fieldData.Origin = _grid.Origin;
                _fieldData.Size = _grid.Size;
                _fieldData.VoxelSize = VoxelSize;

                gridDirty = true;
            }

            if (meshDirty || gridDirty)
            {
                _backer.ClearScene();

                foreach (var mesh in _host!.Descendants<TriangleMesh>())
                {
                    if (!mesh.TryComponent<LightFieldReceiver>(out var rec))
                        continue;

                    if (!rec.IsOccluder)
                        continue;

                    if (rec.NeedUpdate || gridDirty)
                        rec.UpdateVoxels(_gpuVoxelizer);

                    Debug.Assert(rec.Voxels != null);

                    _backer.AddMesh(rec.Voxels);
                }
            }

            bool lightDirty = meshDirty || gridDirty;

            if (!lightDirty)
            {
                foreach (var light in _host!.Descendants<Light>())
                {
                    if (!light.TryComponent<LightFieldEmitter>(out var emit))
                        continue;

                    if (meshDirty || emit.NeedUpdate)
                        lightDirty = true;
                }
            }

            if (lightDirty)
            {
                _backer.ClearLightField();

                foreach (var light in _host!.Descendants<Light>())
                {
                    if (!light.TryComponent<LightFieldEmitter>(out var emit))
                        continue;

                    if (meshDirty || gridDirty || emit.NeedUpdate)
                        emit.UpdateLight(_backer);

                    Debug.Assert(emit.Contributions != null);

                    _backer.AccumulateLight(emit.Contributions);
                }

                if (_fieldData.Textures != null)
                {
                    foreach (var tex in _fieldData.Textures)
                        tex.Dispose();
                }

                _lightField = _backer.GetLightField(true);

                _fieldData.Textures = _backer.CreateTextures();
            }
        }


        public void LoadProfile(string profile)
        {
            var json = Embedded.GetString(profile + ".json");
            
            var param = JsonSerializer.Deserialize<VoxelLightBakeParams>(json, new JsonSerializerOptions
            {
                IncludeFields = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });

            _backer.SetParams(param);

            _profile = profile;
        }


        public TimeSpan MaxUpdateInterval { get; set; } 

        public string? Profile => _profile;

        public float VoxelSize
        {
            get => _voxelSize;
            set => _voxelSize = value;
        }

        public float SpecularStrength
        {
            get => _fieldData.SpecularStrength;
            set => _fieldData.SpecularStrength = value;
        }

        public float DiffuseStrength
        {
            get => _fieldData.DiffuseStrength;
            set => _fieldData.DiffuseStrength = value;
        }
    }
}
