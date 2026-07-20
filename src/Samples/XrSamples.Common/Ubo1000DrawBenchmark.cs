using Silk.NET.OpenGL;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 144)]
public struct ModelUniforms
{
    [FieldOffset(0)]
    public Matrix4x4 WorldMatrix;

    [FieldOffset(64)]
    public Matrix4x4 NormalMatrix;

    [FieldOffset(128)]
    public int DrawId;
}

public sealed unsafe class UboSsbo1000DrawBenchmark : IDisposable
{
    private const int DrawCount = 1000;

    private const uint UboBinding = 0;
    private const uint SsboBinding = 0;

    private readonly GL _gl;
    private readonly bool _gles;

    private uint _vao;
    private uint _vbo;

    private uint _programUbo;
    private uint _programSsboRange;
    private uint _programSsboIndexed;

    private int _uDrawIndexLocation;

    private uint[] _perDrawUbos = Array.Empty<uint>();

    private uint _rangeSsbo;
    private uint _indexedSsbo;

    private ModelUniforms[] _models = Array.Empty<ModelUniforms>();
    private ModelUniforms[] _updateScratch = Array.Empty<ModelUniforms>();

    private int _modelSize;
    private int _ssboRangeStride;

    private int _uboOffsetAlignment;
    private int _ssboOffsetAlignment;
    private int _maxUniformBlockSize;

    public UboSsbo1000DrawBenchmark(GL gl, bool gles)
    {
        _gl = gl;
        _gles = gles;
    }

    public void Init()
    {
        _modelSize = Unsafe.SizeOf<ModelUniforms>();

        _gl.GetInteger(GetPName.UniformBufferOffsetAlignment, out _uboOffsetAlignment);
        _gl.GetInteger(GetPName.ShaderStorageBufferOffsetAlignment, out _ssboOffsetAlignment);
        _gl.GetInteger(GetPName.MaxUniformBlockSize, out _maxUniformBlockSize);
        //_gl.GetInteger(GetPName.MaxShaderStorageBlockSize, out _maxShaderStorageBlockSize);

        _ssboRangeStride = AlignUp(_modelSize, _ssboOffsetAlignment);

        Console.WriteLine("UBO/SSBO benchmark limits:");
        Console.WriteLine($"  DrawCount                                  : {DrawCount}");
        Console.WriteLine($"  sizeof(ModelUniforms)                      : {_modelSize}");
        Console.WriteLine($"  GL_UNIFORM_BUFFER_OFFSET_ALIGNMENT         : {_uboOffsetAlignment}");
        Console.WriteLine($"  GL_SHADER_STORAGE_BUFFER_OFFSET_ALIGNMENT  : {_ssboOffsetAlignment}");
        Console.WriteLine($"  SSBO BindBufferRange stride                : {_ssboRangeStride}");
        Console.WriteLine($"  GL_MAX_UNIFORM_BLOCK_SIZE                  : {_maxUniformBlockSize}");
        Console.WriteLine($"  Indexed SSBO total size                    : {_modelSize * DrawCount}");
        Console.WriteLine($"  Ranged SSBO total size                     : {_ssboRangeStride * DrawCount}");
        Console.WriteLine();

        CreateGeometry();
        CreateModelData();

        _programUbo = CreateProgram(
            MakeVertexUboSource(),
            MakeFragmentSource());

        BindUniformBlock(_programUbo, "ModelBlock", UboBinding);

        _programSsboRange = CreateProgram(
            MakeVertexSsboRangeSource(),
            MakeFragmentSource());

        BindShaderStorageBlock(_programSsboRange, "ModelBlock", SsboBinding);

        _programSsboIndexed = CreateProgram(
            MakeVertexSsboIndexedSource(),
            MakeFragmentSource());

        BindShaderStorageBlock(_programSsboIndexed, "ModelsBlock", SsboBinding);

        _uDrawIndexLocation = _gl.GetUniformLocation(_programSsboIndexed, "uDrawIndex");

        if (_uDrawIndexLocation < 0)
            throw new InvalidOperationException("Uniform uDrawIndex not found or optimized out.");

        CreatePerDrawUbos();
        CreateRangeSsbo();
        CreateIndexedSsbo();

        _updateScratch = new ModelUniforms[DrawCount];
    }

    public void RunDrawBenchmark(int iterations = 200, int warmupIterations = 20)
    {
        _gl.Disable(EnableCap.DepthTest);
        _gl.Disable(EnableCap.Blend);
        _gl.Disable(EnableCap.CullFace);

        for (var i = 0; i < warmupIterations; i++)
        {
            DrawA_PerDrawUbo();
            DrawB_SsboBindRange();
            DrawC_SsboIndexed();
        }

        _gl.Finish();

        Console.WriteLine("=== DRAW BENCHMARK ===");
        Console.WriteLine();

        MeasureDraw(
            "A - 1000 single UBOs, BindBufferBase per draw",
            iterations,
            DrawA_PerDrawUbo);

        MeasureDraw(
            "B - one large SSBO, BindBufferRange per draw",
            iterations,
            DrawB_SsboBindRange);

        MeasureDraw(
            "C - one large SSBO, BindBufferBase once, uniform index per draw",
            iterations,
            DrawC_SsboIndexed);

        Console.WriteLine();
    }

    public void RunUpdateBenchmark(int iterations = 200)
    {
        Console.WriteLine("=== UPDATE BENCHMARK ===");
        Console.WriteLine("No draw calls. Measures buffer upload/update paths only.");
        Console.WriteLine();

        RunUpdateCase(iterations, "0 changed", Array.Empty<int>());

        RunUpdateCase(iterations, "1 changed scattered", MakeScatteredChangedIndices(1));
        RunUpdateCase(iterations, "2 changed scattered", MakeScatteredChangedIndices(2));
        RunUpdateCase(iterations, "4 changed scattered", MakeScatteredChangedIndices(4));
        RunUpdateCase(iterations, "8 changed scattered", MakeScatteredChangedIndices(8));
        RunUpdateCase(iterations, "16 changed scattered", MakeScatteredChangedIndices(16));
        RunUpdateCase(iterations, "32 changed scattered", MakeScatteredChangedIndices(32));
        RunUpdateCase(iterations, "100 changed scattered", MakeScatteredChangedIndices(100));
        RunUpdateCase(iterations, "300 changed scattered", MakeScatteredChangedIndices(300));
        RunUpdateCase(iterations, "500 changed scattered", MakeScatteredChangedIndices(500));
        RunUpdateCase(iterations, "1000 changed", MakeScatteredChangedIndices(1000));

        RunUpdateCase(iterations, "4 changed contiguous", MakeContiguousChangedIndices(100, 4));
        RunUpdateCase(iterations, "8 changed contiguous", MakeContiguousChangedIndices(100, 8));
        RunUpdateCase(iterations, "16 changed contiguous", MakeContiguousChangedIndices(100, 16));
        RunUpdateCase(iterations, "32 changed contiguous", MakeContiguousChangedIndices(100, 32));
        RunUpdateCase(iterations, "100 changed contiguous", MakeContiguousChangedIndices(100, 100));
        RunUpdateCase(iterations, "300 changed contiguous", MakeContiguousChangedIndices(100, 300));

        Console.WriteLine();
    }

    public void RunAll(int drawIterations = 200, int updateIterations = 200, int warmupIterations = 20)
    {
        RunDrawBenchmark(drawIterations, warmupIterations);
        RunUpdateBenchmark(updateIterations);
    }

    private void MeasureDraw(string name, int iterations, Action draw)
    {
        _gl.Finish();

        var sw = Stopwatch.StartNew();

        for (var i = 0; i < iterations; i++)
            draw();

        _gl.Finish();

        sw.Stop();

        var totalMs = sw.Elapsed.TotalMilliseconds;
        var frameMs = totalMs / iterations;
        var drawUs = frameMs * 1000.0 / DrawCount;

        Console.WriteLine(name);
        Console.WriteLine($"  total     : {totalMs:F3} ms");
        Console.WriteLine($"  avg frame : {frameMs:F4} ms");
        Console.WriteLine($"  avg draw  : {drawUs:F4} us");
    }

    private void MeasureUpdate(string name, int iterations, Action<int> update)
    {
        _gl.Finish();

        var sw = Stopwatch.StartNew();

        for (var frame = 0; frame < iterations; frame++)
            update(frame);

        _gl.Finish();

        sw.Stop();

        var totalMs = sw.Elapsed.TotalMilliseconds;
        var avgMs = totalMs / iterations;

        Console.WriteLine(name);
        Console.WriteLine($"  total : {totalMs:F3} ms");
        Console.WriteLine($"  avg   : {avgMs:F4} ms");
    }

    private void RunUpdateCase(int iterations, string label, int[] changedIndices)
    {
        Console.WriteLine($"--- {label} ---");
        Console.WriteLine($"changed count: {changedIndices.Length}");

        MeasureUpdate(
            "A - separate UBOs, update changed buffers",
            iterations,
            frame => UpdateA_PerDrawUbos(changedIndices, frame));

        MeasureUpdate(
            "B1 - SSBO full contiguous update once",
            iterations,
            UpdateB1_SsboFull);

        MeasureUpdate(
            "B2 - SSBO individual range updates",
            iterations,
            frame => UpdateB2_SsboIndividualRanges(changedIndices, frame));

        MeasureUpdate(
            "B3 - SSBO merged range updates",
            iterations,
            frame => UpdateB3_SsboMergedRanges(changedIndices, frame));

        Console.WriteLine();
    }

    private void DrawA_PerDrawUbo()
    {
        _gl.UseProgram(_programUbo);
        _gl.BindVertexArray(_vao);

        for (var i = 0; i < DrawCount; i++)
        {
            _gl.BindBufferBase(
                BufferTargetARB.UniformBuffer,
                UboBinding,
                _perDrawUbos[i]);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        _gl.BindVertexArray(0);
    }

    private void DrawB_SsboBindRange()
    {
        _gl.UseProgram(_programSsboRange);
        _gl.BindVertexArray(_vao);

        for (var i = 0; i < DrawCount; i++)
        {
            _gl.BindBufferRange(
                BufferTargetARB.ShaderStorageBuffer,
                SsboBinding,
                _rangeSsbo,
                i * _ssboRangeStride,
                (nuint)_modelSize);

            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        _gl.BindVertexArray(0);
    }

    private void DrawC_SsboIndexed()
    {
        _gl.UseProgram(_programSsboIndexed);
        _gl.BindVertexArray(_vao);

        _gl.BindBufferBase(
            BufferTargetARB.ShaderStorageBuffer,
            SsboBinding,
            _indexedSsbo);

        for (var i = 0; i < DrawCount; i++)
        {
            _gl.Uniform1(_uDrawIndexLocation, i);
            _gl.DrawArrays(PrimitiveType.Triangles, 0, 3);
        }

        _gl.BindVertexArray(0);
    }

    private void UpdateA_PerDrawUbos(int[] changedIndices, int frame)
    {
        if (changedIndices.Length == 0)
            return;

        for (var n = 0; n < changedIndices.Length; n++)
        {
            var i = changedIndices[n];

            var model = MakeUpdatedModel(i, frame);

            _gl.BindBuffer(BufferTargetARB.UniformBuffer, _perDrawUbos[i]);

            _gl.BufferSubData(
                BufferTargetARB.UniformBuffer,
                0,
                (nuint)_modelSize,
                &model);
        }

        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    private void UpdateB1_SsboFull(int frame)
    {
        for (var i = 0; i < DrawCount; i++)
            _updateScratch[i] = MakeUpdatedModel(i, frame);

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, _indexedSsbo);

        fixed (ModelUniforms* p = _updateScratch)
        {
            _gl.BufferSubData(
                BufferTargetARB.ShaderStorageBuffer,
                0,
                (nuint)(_modelSize * DrawCount),
                p);
        }

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
    }

    private void UpdateB2_SsboIndividualRanges(int[] changedIndices, int frame)
    {
        if (changedIndices.Length == 0)
            return;

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, _indexedSsbo);

        for (var n = 0; n < changedIndices.Length; n++)
        {
            var i = changedIndices[n];

            var model = MakeUpdatedModel(i, frame);

            _gl.BufferSubData(
                BufferTargetARB.ShaderStorageBuffer,
                i * _modelSize,
                (nuint)_modelSize,
                &model);
        }

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
    }

    private void UpdateB3_SsboMergedRanges(int[] changedIndices, int frame)
    {
        if (changedIndices.Length == 0)
            return;

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, _indexedSsbo);

        var runStart = changedIndices[0];
        var previous = changedIndices[0];

        for (var n = 1; n <= changedIndices.Length; n++)
        {
            var flush =
                n == changedIndices.Length ||
                changedIndices[n] != previous + 1;

            if (flush)
            {
                UploadIndexedSsboRun(runStart, previous, frame);

                if (n < changedIndices.Length)
                {
                    runStart = changedIndices[n];
                    previous = changedIndices[n];
                }
            }
            else
            {
                previous = changedIndices[n];
            }
        }

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
    }

    private void UploadIndexedSsboRun(int startIndex, int endIndexInclusive, int frame)
    {
        var count = endIndexInclusive - startIndex + 1;

        for (var i = 0; i < count; i++)
            _updateScratch[i] = MakeUpdatedModel(startIndex + i, frame);

        fixed (ModelUniforms* p = _updateScratch)
        {
            _gl.BufferSubData(
                BufferTargetARB.ShaderStorageBuffer,
                startIndex * _modelSize,
                (nuint)(count * _modelSize),
                p);
        }
    }

    private void CreateGeometry()
    {
        float[] vertices =
        {
            -0.01f, -0.01f, 0.0f,
             0.01f, -0.01f, 0.0f,
             0.00f,  0.01f, 0.0f,
        };

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

        fixed (float* pVertices = vertices)
        {
            _gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * sizeof(float)),
                pVertices,
                BufferUsageARB.StaticDraw);
        }

        _gl.EnableVertexAttribArray(0);

        _gl.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            null);

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
        _gl.BindVertexArray(0);
    }

    private void CreateModelData()
    {
        _models = new ModelUniforms[DrawCount];

        for (var i = 0; i < DrawCount; i++)
            _models[i] = MakeUpdatedModel(i, 0);
    }

    private void CreatePerDrawUbos()
    {
        _perDrawUbos = new uint[DrawCount];

        fixed (uint* pBuffers = _perDrawUbos)
        {
            _gl.GenBuffers(DrawCount, pBuffers);
        }

        for (var i = 0; i < DrawCount; i++)
        {
            _gl.BindBuffer(BufferTargetARB.UniformBuffer, _perDrawUbos[i]);

            fixed (ModelUniforms* pModel = &_models[i])
            {
                _gl.BufferData(
                    BufferTargetARB.UniformBuffer,
                    (nuint)_modelSize,
                    pModel,
                    BufferUsageARB.DynamicDraw);
            }
        }

        _gl.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    private void CreateRangeSsbo()
    {
        _rangeSsbo = _gl.GenBuffer();

        var totalSize = _ssboRangeStride * DrawCount;
        var temp = new byte[totalSize];

        fixed (byte* pTemp = temp)
        fixed (ModelUniforms* pModels = _models)
        {
            for (var i = 0; i < DrawCount; i++)
            {
                System.Buffer.MemoryCopy(
                    pModels + i,
                    pTemp + i * _ssboRangeStride,
                    _modelSize,
                    _modelSize);
            }

            _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, _rangeSsbo);

            _gl.BufferData(
                BufferTargetARB.ShaderStorageBuffer,
                (nuint)totalSize,
                pTemp,
                BufferUsageARB.DynamicDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
    }

    private void CreateIndexedSsbo()
    {
        _indexedSsbo = _gl.GenBuffer();

        var totalSize = _modelSize * DrawCount;

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, _indexedSsbo);

        fixed (ModelUniforms* pModels = _models)
        {
            _gl.BufferData(
                BufferTargetARB.ShaderStorageBuffer,
                (nuint)totalSize,
                pModels,
                BufferUsageARB.DynamicDraw);
        }

        _gl.BindBuffer(BufferTargetARB.ShaderStorageBuffer, 0);
    }

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        var vs = CompileShader(ShaderType.VertexShader, vertexSource);
        var fs = CompileShader(ShaderType.FragmentShader, fragmentSource);

        var program = _gl.CreateProgram();

        _gl.AttachShader(program, vs);
        _gl.AttachShader(program, fs);

        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);

        if (status == 0)
        {
            var log = _gl.GetProgramInfoLog(program);
            throw new InvalidOperationException("Program link failed:\n" + log);
        }

        _gl.DetachShader(program, vs);
        _gl.DetachShader(program, fs);
        _gl.DeleteShader(vs);
        _gl.DeleteShader(fs);

        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);

        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);

        if (status == 0)
        {
            var log = _gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"{type} compile failed:\n{log}\n\nSource:\n{source}");
        }

        return shader;
    }

    private void BindUniformBlock(uint program, string blockName, uint bindingPoint)
    {
        var blockIndex = _gl.GetUniformBlockIndex(program, blockName);

        if (blockIndex == uint.MaxValue)
            throw new InvalidOperationException($"Uniform block not found: {blockName}");

        _gl.UniformBlockBinding(program, blockIndex, bindingPoint);
    }

    private void BindShaderStorageBlock(uint program, string blockName, uint bindingPoint)
    {
        var blockIndex = _gl.GetProgramResourceIndex(
            program,
            ProgramInterface.ShaderStorageBlock,
            blockName);

        if (blockIndex == uint.MaxValue)
            throw new InvalidOperationException($"Shader storage block not found: {blockName}");

        _gl.ShaderStorageBlockBinding(program, blockIndex, bindingPoint);
    }

    private string MakeVertexUboSource()
    {
        if (_gles)
        {
            return
@"#version 310 es
precision highp float;

layout(location = 0) in vec3 aPosition;

layout(std140) uniform ModelBlock
{
    mat4 WorldMatrix;
    mat4 NormalMatrix;
    int DrawId;
};

void main()
{
    vec3 p = aPosition;
    p.x += float(DrawId) * 0.000001;

    gl_Position = WorldMatrix * vec4(p, 1.0);
}";
        }

        return
@"#version 330 core

layout(location = 0) in vec3 aPosition;

layout(std140) uniform ModelBlock
{
    mat4 WorldMatrix;
    mat4 NormalMatrix;
    int DrawId;
};

void main()
{
    vec3 p = aPosition;
    p.x += float(DrawId) * 0.000001;

    gl_Position = WorldMatrix * vec4(p, 1.0);
}";
    }

    private string MakeVertexSsboRangeSource()
    {
        if (_gles)
        {
            return
@"#version 310 es
precision highp float;

layout(location = 0) in vec3 aPosition;

struct ModelUniforms
{
    mat4 WorldMatrix;
    mat4 NormalMatrix;
    int DrawId;
};

layout(std430) readonly buffer ModelBlock
{
    ModelUniforms Model;
};

void main()
{
    vec3 p = aPosition;
    p.x += float(Model.DrawId) * 0.000001;

    gl_Position = Model.WorldMatrix * vec4(p, 1.0);
}";
        }

        return
@"#version 430 core

layout(location = 0) in vec3 aPosition;

struct ModelUniforms
{
    mat4 WorldMatrix;
    mat4 NormalMatrix;
    int DrawId;
};

layout(std430) readonly buffer ModelBlock
{
    ModelUniforms Model;
};

void main()
{
    vec3 p = aPosition;
    p.x += float(Model.DrawId) * 0.000001;

    gl_Position = Model.WorldMatrix * vec4(p, 1.0);
}";
    }

    private string MakeVertexSsboIndexedSource()
    {
        if (_gles)
        {
            return
@"#version 310 es
precision highp float;

layout(location = 0) in vec3 aPosition;

struct ModelUniforms
{
    mat4 WorldMatrix;
    mat4 NormalMatrix;
    int DrawId;
};

layout(std430) readonly buffer ModelsBlock
{
    ModelUniforms Models[];
};

uniform int uDrawIndex;

void main()
{
    ModelUniforms m = Models[uDrawIndex];

    vec3 p = aPosition;
    p.x += float(m.DrawId) * 0.000001;

    gl_Position = m.WorldMatrix * vec4(p, 1.0);
}";
        }

        return
@"#version 430 core

layout(location = 0) in vec3 aPosition;

struct ModelUniforms
{
    mat4 WorldMatrix;
    mat4 NormalMatrix;
    int DrawId;
};

layout(std430) readonly buffer ModelsBlock
{
    ModelUniforms Models[];
};

uniform int uDrawIndex;

void main()
{
    ModelUniforms m = Models[uDrawIndex];

    vec3 p = aPosition;
    p.x += float(m.DrawId) * 0.000001;

    gl_Position = m.WorldMatrix * vec4(p, 1.0);
}";
    }

    private string MakeFragmentSource()
    {
        if (_gles)
        {
            return
@"#version 310 es
precision highp float;

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 1.0, 1.0, 1.0);
}";
        }

        return
@"#version 430 core

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0, 1.0, 1.0, 1.0);
}";
    }

    private static ModelUniforms MakeUpdatedModel(int i, int frame)
    {
        var t = frame * 0.001f;

        var x = ((i % 50) - 25) * 0.035f;
        var y = ((i / 50) - 10) * 0.035f;

        x += MathF.Sin(t + i * 0.01f) * 0.001f;
        y += MathF.Cos(t + i * 0.01f) * 0.001f;

        var world =
            Matrix4x4.CreateScale(1.0f) *
            Matrix4x4.CreateTranslation(x, y, 0.0f);

        return new ModelUniforms
        {
            WorldMatrix = world,
            NormalMatrix = Matrix4x4.Identity,
            DrawId = i + frame
        };
    }

    private static int[] MakeContiguousChangedIndices(int start, int count)
    {
        count = Math.Clamp(count, 0, DrawCount);

        if (start + count > DrawCount)
            start = DrawCount - count;

        var result = new int[count];

        for (var i = 0; i < count; i++)
            result[i] = start + i;

        return result;
    }

    private static int[] MakeScatteredChangedIndices(int count)
    {
        count = Math.Clamp(count, 0, DrawCount);

        var result = new int[count];

        if (count == 0)
            return result;

        var used = new bool[DrawCount];

        var step = 997;
        var value = 17;

        for (var i = 0; i < count; i++)
        {
            while (used[value])
                value = (value + 1) % DrawCount;

            result[i] = value;
            used[value] = true;

            value = (value + step) % DrawCount;
        }

        Array.Sort(result);
        return result;
    }

    private static int AlignUp(int value, int alignment)
    {
        return ((value + alignment - 1) / alignment) * alignment;
    }

    public void Dispose()
    {
        if (_perDrawUbos.Length > 0)
        {
            fixed (uint* p = _perDrawUbos)
            {
                _gl.DeleteBuffers((uint)_perDrawUbos.Length, p);
            }

            _perDrawUbos = Array.Empty<uint>();
        }

        if (_rangeSsbo != 0)
        {
            _gl.DeleteBuffer(_rangeSsbo);
            _rangeSsbo = 0;
        }

        if (_indexedSsbo != 0)
        {
            _gl.DeleteBuffer(_indexedSsbo);
            _indexedSsbo = 0;
        }

        if (_vbo != 0)
        {
            _gl.DeleteBuffer(_vbo);
            _vbo = 0;
        }

        if (_vao != 0)
        {
            _gl.DeleteVertexArray(_vao);
            _vao = 0;
        }

        if (_programUbo != 0)
        {
            _gl.DeleteProgram(_programUbo);
            _programUbo = 0;
        }

        if (_programSsboRange != 0)
        {
            _gl.DeleteProgram(_programSsboRange);
            _programSsboRange = 0;
        }

        if (_programSsboIndexed != 0)
        {
            _gl.DeleteProgram(_programSsboIndexed);
            _programSsboIndexed = 0;
        }
    }
}