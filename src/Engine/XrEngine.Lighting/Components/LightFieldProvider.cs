using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.Lighting
{
    public class LightFieldProvider : AsyncBehavior<Group3D>, ILightFieldProvider
    {
        string? _profile;
        VoxelGridDesc _grid;
        Bounds3 _lastBounds;
        VoxelLightFieldView? _lightField;
        float _voxelSize;
        double _lastBuildTime;
        int _paddding;
        int _profileVersion;
        int _lastProfileVersion;
        readonly VoxelLightBaker _backer;
        readonly GpuMeshVoxelizer _gpuVoxelizer;
        readonly IGlContext _workerCtx;
        readonly LightFieldData _fieldData;
        readonly IGlContextProvider _ctxProvider;
        readonly HashSet<LightFieldEmitter> _activeEmitters = [];
        readonly HashSet<LightFieldReceiver> _activeOccluders = [];

        public LightFieldProvider()
        {
            _paddding = 1;

            _voxelSize = 0.05f;

            _backer = new VoxelLightBaker();
            _fieldData = new LightFieldData();

            _ctxProvider = Context.Require<IGlContextProvider>();
            _workerCtx = _ctxProvider.CreateShared();
            _gpuVoxelizer = new GpuMeshVoxelizer(_workerCtx.Gl);

            MaxUpdateInterval = 0;

            LoadProfile("Occlusions");

            Context.Implement<ILightFieldProvider>(this);
        }

        public LightFieldData GetLightField()
        {
            return _fieldData;
        }

        protected override async Task UpdateAsync(RenderContext ctx)
        {
            if (MaxUpdateInterval > 0 && (ctx.Time - _lastBuildTime) >= MaxUpdateInterval)
            {
                await Task.Run(RebuildAsync);

                _lastBuildTime = ctx.Time;
            }
        }


        [Action]
        public async Task RebuildAsync()
        {
            if (_ctxProvider.Current == null)
            {
                _workerCtx.Take();
                OpenGLRender.Current ??= new OpenGLRender(_workerCtx.Gl);
            }


            bool profileDirty = _lastProfileVersion != _profileVersion;
            bool gridDirty = false;
            bool meshDirty = false;

            try
            {
                var bounds = new Bounds3Builder();

                foreach (var mesh in _host!.Descendants<TriangleMesh>())
                {
                    if (!mesh.TryComponent<LightFieldReceiver>(out var rec))
                        continue;

                    if (!rec.IsEnabled || !rec.IsOccluder)
                    {
                        if (_activeOccluders.Remove(rec))
                            meshDirty = true;
                    }
                    else
                    {
                        if (_activeOccluders.Add(rec))
                            meshDirty = true;
                    }

                    if (!rec.IsEnabled)
                        continue;

                    bounds.Add(mesh.WorldBounds);

                    if (rec.NeedUpdate)
                        meshDirty = true;
                }

                var curBounds = bounds.Result;

                if (curBounds != _lastBounds || _fieldData.VoxelSize != _voxelSize)
                {
                    var padSize = curBounds.Size + new Vector3(_paddding * _voxelSize * 2);

                    _grid = new VoxelGridDesc
                    {
                        Origin = curBounds.Min - new Vector3(_paddding * _voxelSize),
                        VoxelSize = _voxelSize,
                        Size = new Vector3I(
                            (int)Math.Ceiling(padSize.X / _voxelSize),
                            (int)Math.Ceiling(padSize.Y / _voxelSize),
                            (int)Math.Ceiling(padSize.Z / _voxelSize))
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

                        if (!rec.IsOccluder || !rec.IsEnabled)
                            continue;

                        if (rec.NeedUpdate || gridDirty)
                            rec.UpdateVoxels(_gpuVoxelizer);

                        Debug.Assert(rec.Voxels != null);

                        _backer.AddMesh(rec.Voxels);
                    }
                }
            }
            finally
            {
                if (_ctxProvider.Current == _workerCtx)
                    _workerCtx.Release();
            }

            bool lightDirty = meshDirty || gridDirty || profileDirty;

            foreach (var light in _host!.Descendants<Light>())
            {
                if (!light.TryComponent<LightFieldEmitter>(out var emit))
                    continue;

                if (!emit.IsEnabled)
                {
                    if (_activeEmitters.Remove(emit))
                        lightDirty = true;
                    continue;
                }
                else
                {
                    if (_activeEmitters.Add(emit))
                        lightDirty = true;
                }

                if (emit.NeedUpdate)
                    lightDirty = true;
            }

            if (lightDirty)
            {
                _backer.ClearLightField();

                foreach (var light in _host!.Descendants<Light>())
                {
                    if (!light.TryComponent<LightFieldEmitter>(out var emit))
                        continue;

                    if (!emit.IsEnabled)
                        continue;

                    if (meshDirty || gridDirty || emit.NeedUpdate || profileDirty)
                        emit.UpdateLight(this);

                    if (emit.Contributions != null)
                    {
                        Log.Info(this, "Accumulate {0}", light.Name ?? light.GetType().Name);
                        _backer.AccumulateLight(emit.Contributions);
                    }
                }

                await EngineApp.MainThread;
             
                Extract();
            }

            _lastProfileVersion = _profileVersion;
        }

        public void Extract()
        {
            Log.Info(this, "Extracting light field");

            if (_fieldData.Textures != null)
            {
                foreach (var tex in _fieldData.Textures)
                    tex.Dispose();
            }

            _lightField = _backer.GetLightField(true);

            _fieldData.Textures = _backer.CreateTextures();

            Log.Debug(this, "Light field loaded");
        }

        public void LoadProfile(VoxelLightBakeParams profile)
        {
            if (profile.Equals(_backer.Params))
                return;

            _backer.SetParams(profile);
            _profile = "";
            _profileVersion++;
        }

        public void LoadProfile(string profile)
        {
            var json = Embedded.GetString<LightFieldProvider>(profile + ".json");
            
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
            _profileVersion++;
        }

        [Action]
        public void Export(string path)
        {
            var textures = _backer.CreateTextures();

            Directory.CreateDirectory(path);

            var writer = PvrTranscoder.Instance;

            for (var i = 0; i < textures.Count; i++)
            {
                using var fs = File.OpenWrite(Path.Combine(path, $"Tex_{i}.pvr"));

                writer.SaveTexture(fs, textures[i].Data!);
            }
        }

        public void Import(string path)
        {
            if (!Directory.Exists(path))
                return;

            var reader = PvrTranscoder.Instance;

            var textures = new List<Texture3D>();

            var files = Directory.GetFiles(path, "*.pvr")
                .OrderBy(a => int.Parse(Path.GetFileNameWithoutExtension(a).Split('_')[1]))
                .ToArray();


            foreach (var file in files)
            {
                using var fs = File.OpenRead(file);
                var data = reader.LoadTexture(fs);

                TextureFormat format;
                TextureType type = TextureType.Unspecified;

                if ((textures.Count % 2) == 0)
                    format = TextureFormat.Rgb9e5Float;
                else
                {
                    type = TextureType.NormalMap;
                    format = TextureFormat.RgbFloat16;
                }

                var tex = new Texture3D()
                {
                    Format = format,
                    MipLevelCount = 0,
                    MinFilter = ScaleFilter.Nearest,
                    MagFilter = ScaleFilter.Linear,
                    Type = type
                };

                tex.LoadData(data);

                textures.Add(tex);
            }

            _fieldData.Textures = textures.ToArray();
        }

        public VoxelLightBaker Baker => _backer;

        public float MaxUpdateInterval { get; set; } 

        public string? Profile => _profile;

        public float VoxelSize
        {
            get => _voxelSize;
            set => _voxelSize = value;
        }

        [Range(0, 1, 0.01f)]
        public float SpecularStrength
        {
            get => _fieldData.SpecularStrength;
            set => _fieldData.SpecularStrength = value;
        }

        [Range(0, 1, 0.01f)]
        public float DiffuseStrength
        {
            get => _fieldData.DiffuseStrength;
            set => _fieldData.DiffuseStrength = value;
        }
    }
}
