using glTFLoader.Schema;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using XrEngine;

public class GltfContent
{
    public glTFLoader.Schema.Gltf? Root { get; set; }

    public IList<byte[]>? Binaries { get; set; }
}

public class GltfExportOptions
{

}

public class GltfExporter
{

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GlbHeader
    {
        public GlbHeader()
        {
        }

        public uint Magic = 0x46546C67;

        public uint Version = 2;

        public uint Size = 0;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GlbChunk
    {
        public uint Length;

        public uint Type;

    }

    public struct GlbChunkInfo
    {
        public GlbChunkInfo(byte[] data, uint type)
        {
            int padding = (4 - (data.Length % 4)) % 4;

            Data = data;
            Padding = new byte[padding];
            Type = type;

            if (type == 0x4E4F534A)
            {
                for (int i = 0; i < padding; i++)
                    Padding[i] = 0x20;
            }
        }

        public byte[] Data;

        public byte[] Padding;

        public uint Type;
    }

    private MemoryStream? _binStream;
    private Gltf _root;


    protected void ExportGeometry(Geometry3D geometry)
    {

    }

    public GltfContent Export(EngineObject obj, GltfExportOptions options)
    {
        GltfContent result = new GltfContent();
        _binStream = new MemoryStream();
        _root = new glTFLoader.Schema.Gltf();

        Geometry3D?[] geometries = obj is Object3D object3D ? object3D
            .DescendantsOrSelf()
            .OfType<TriangleMesh>()
            .Select(a => a.Geometry)
            .Where(a => a != null)
            .ToArray() : [];

        foreach (Geometry3D? geometry in geometries)
            ExportGeometry(geometry!);


        return new GltfContent
        {
            Root = _root,
            Binaries = [_binStream.ToArray()]
        };
    }

    public void Export(EngineObject obj, string outPath, GltfExportOptions options)
    {
        using FileStream file = File.OpenWrite(outPath);
        Export(obj, file, options);
    }

    public void Export(EngineObject obj, Stream outStream, GltfExportOptions options)
    {
        GltfContent content = Export(obj, options);

        List<GlbChunkInfo> chunks = new List<GlbChunkInfo>();

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(content.Root, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        });

        chunks.Add(new GlbChunkInfo(json, 0x4E4F534A));

        foreach (byte[] bin in content.Binaries ?? [])
            chunks.Add(new GlbChunkInfo(bin, 0x004E4942));

        GlbHeader header = new GlbHeader();
        header.Size = (uint)(Marshal.SizeOf(header) +
            Marshal.SizeOf<GlbChunk>() *
            chunks.Count + chunks.Sum(a => a.Data.Length + a.Padding.Length));

        outStream.WriteStruct(header);

        foreach (GlbChunkInfo chunkInfo in chunks)
        {
            GlbChunk chunk = new GlbChunk()
            {
                Length = (uint)(chunkInfo.Data.Length + chunkInfo.Padding.Length),
                Type = chunkInfo.Type
            };
            outStream.WriteStruct(chunk);
            outStream.Write(chunkInfo.Data);
            outStream.Write(chunkInfo.Padding);
        }
    }
}