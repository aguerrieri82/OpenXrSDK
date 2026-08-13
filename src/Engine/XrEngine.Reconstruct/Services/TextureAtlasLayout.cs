using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using XrEngine.OpenGL;
using XrMath;

namespace XrEngine.Reconstruct
{
    public readonly struct TextureAtlasEntry
    {
        public TextureAtlasEntry(
            int imageIndex,
            Vector2 sourceOrigin,
            Vector2 sourceAxisX,
            Vector2 sourceAxisY,
            float sourceWidth,
            float sourceHeight,
            int atlasX,
            int atlasY,
            int atlasWidth,
            int atlasHeight,
            bool atlasRotated)
        {
            ImageIndex = imageIndex;

            SourceOrigin = sourceOrigin;
            SourceAxisX = sourceAxisX;
            SourceAxisY = sourceAxisY;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;

            AtlasX = atlasX;
            AtlasY = atlasY;
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
            AtlasRotated = atlasRotated;
        }

        public Vector2 GetSourcePoint(float x, float y)
        {
            return SourceOrigin + SourceAxisX * x + SourceAxisY * y;
        }

        public Vector2 SourceP0 => SourceOrigin;

        public Vector2 SourceP1 => SourceOrigin + SourceAxisX * SourceWidth;

        public Vector2 SourceP2 => SourceOrigin + SourceAxisX * SourceWidth + SourceAxisY * SourceHeight;

        public Vector2 SourceP3 => SourceOrigin + SourceAxisY * SourceHeight;

        public int ImageIndex { get; }

        public Vector2 SourceOrigin { get; }

        public Vector2 SourceAxisX { get; }

        public Vector2 SourceAxisY { get; }

        public float SourceWidth { get; }

        public float SourceHeight { get; }

        public int AtlasX { get; }

        public int AtlasY { get; }

        public int AtlasWidth { get; }

        public int AtlasHeight { get; }

        public bool AtlasRotated { get; }
    }

    public sealed class TextureAtlasLayout
    {
        private readonly Dictionary<int, TextureAtlasEntry> _entriesByImageIndex;

        public TextureAtlasLayout(
            int atlasWidth,
            int atlasHeight,
            long fullMemory,
            long orientedBoundsMemory,
            long atlasMemory,
            IReadOnlyList<TextureAtlasEntry> entries)
        {
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
            FullMemory = fullMemory;
            OrientedBoundsMemory = orientedBoundsMemory;
            AtlasMemory = atlasMemory;
            Entries = entries;
            _entriesByImageIndex = entries.ToDictionary(a => a.ImageIndex);
        }
        public bool TryGetEntry(int imageIndex, out TextureAtlasEntry entry)
        {
            return _entriesByImageIndex.TryGetValue(imageIndex, out entry);
        }

        public int AtlasWidth { get; }

        public int AtlasHeight { get; }

        public long FullMemory { get; }

        public long OrientedBoundsMemory { get; }

        public long AtlasMemory { get; }

        public long SavedMemory => FullMemory - AtlasMemory;

        public IReadOnlyList<TextureAtlasEntry> Entries { get; }
    }

    public sealed class TextureAtlasLayoutBuilderParams
    {
        public TextureAtlasLayoutBuilderParams()
        {
            TextureWidth = 1280;
            TextureHeight = 1280;
            Padding = 2;
            SourceBorderPixels = 2;
            BytesPerPixel = 4;
            SourceTextureCount = 0;
            RectAlignment = 4;
            WidthSearchStep = 64;
            MaxWidthSearchSteps = 512;
            AllowAtlasRotation = true;
        }

        /// <summary>
        /// Width of each source texture layer in pixels.
        ///
        /// Must match the real source camera texture width, because source UVs are converted to source
        /// pixel coordinates before oriented bounds are computed.
        /// </summary>
        public int TextureWidth { get; set; }

        /// <summary>
        /// Height of each source texture layer in pixels.
        ///
        /// Must match the real source camera texture height.
        /// </summary>
        public int TextureHeight { get; set; }

        /// <summary>
        /// Atlas-space empty border around each packed source rectangle.
        ///
        /// This separates copied source regions so filtering does not immediately sample a neighboring
        /// packed region.
        ///
        /// Suggested:
        /// 2 for compact legacy atlas packing;
        /// 4+ if atlas filtering artifacts appear.
        /// </summary>
        public int Padding { get; set; }

        /// <summary>
        /// Extra source-image border included around the actually used UV region before packing.
        ///
        /// This copies a small amount of neighboring source image around the used polygon, reducing edge
        /// sampling artifacts after remapping into the atlas.
        ///
        /// Suggested:
        /// 1-2 normally.
        /// </summary>
        public int SourceBorderPixels { get; set; }

        /// <summary>
        /// Bytes per source pixel, used only for memory/statistics estimates.
        ///
        /// RGBA8 = 4, RGB24 = 3.
        /// Does not change the render texture format.
        /// </summary>
        public int BytesPerPixel { get; set; }

        /// <summary>
        /// Total number of source textures used for memory/statistics reporting.
        ///
        /// If 0, the builder uses the number of texture entries actually referenced by the geometry.
        /// Set this when the texture array contains more source images than the current geometry uses.
        /// </summary>
        public int SourceTextureCount { get; set; }

        /// <summary>
        /// Pixel alignment for packed rectangles and final atlas dimensions.
        ///
        /// Suggested:
        /// 4 for normal use.
        /// </summary>
        public int RectAlignment { get; set; }

        /// <summary>
        /// Width increment used while searching candidate atlas widths.
        ///
        /// Smaller values can find a tighter atlas but increase packing cost.
        /// Larger values are faster but may waste atlas area.
        ///
        /// Suggested:
        /// 64 normally.
        /// </summary>
        public int WidthSearchStep { get; set; }

        /// <summary>
        /// Maximum number of atlas-width candidates tested.
        ///
        /// If the possible width range is too large, the effective search step is increased so the search
        /// stays bounded.
        ///
        /// Suggested:
        /// 512 for normal/offline generation.
        /// </summary>
        public int MaxWidthSearchSteps { get; set; }

        /// <summary>
        /// Allows packed source rectangles to rotate by 90 degrees.
        ///
        /// Usually improves packing efficiency.
        /// Disable only when debugging atlas orientation or when fixed source orientation is required.
        /// </summary>
        public bool AllowAtlasRotation { get; set; }
    }

    public sealed class TextureAtlasLayoutBuilder
    {
        #region Private Structs

        private struct OrientedRect
        {
            public int ImageIndex;

            public Vector2 Origin;

            public Vector2 AxisX;

            public Vector2 AxisY;

            public float Width;

            public float Height;

            public int ContentWidth;

            public int ContentHeight;

            public long Area => (long)ContentWidth * ContentHeight;
        }

        private struct PackItem
        {
            public OrientedRect Rect;

            public int Width;

            public int Height;

            public int RotatedWidth;

            public int RotatedHeight;
        }

        private struct PackedItem
        {
            public OrientedRect Rect;

            public int X;

            public int Y;

            public int Width;

            public int Height;

            public bool Rotated;
        }

        private struct PackResult
        {
            public int Width;

            public int Height;

            public List<PackedItem> Items;

            public long Area => (long)Width * Height;
        }

        private struct SkylineNode
        {
            public int X;

            public int Y;

            public int Width;
        }

        #endregion

        private int _textureWidth;
        private int _textureHeight;
        private int _padding;
        private int _sourceBorderPixels;
        private int _bytesPerPixel;
        private int _sourceTextureCount;
        private int _rectAlignment;
        private int _widthSearchStep;
        private int _maxWidthSearchSteps;
        private bool _allowAtlasRotation;

        public TextureAtlasLayoutBuilder()
        {
            SetParams(new TextureAtlasLayoutBuilderParams());
        }

        public void SetParams(TextureAtlasLayoutBuilderParams parameters)
        {
            _textureWidth = parameters.TextureWidth;
            _textureHeight = parameters.TextureHeight;
            _padding = parameters.Padding;
            _sourceBorderPixels = parameters.SourceBorderPixels;
            _bytesPerPixel = parameters.BytesPerPixel;
            _sourceTextureCount = parameters.SourceTextureCount;
            _rectAlignment = parameters.RectAlignment;
            _widthSearchStep = parameters.WidthSearchStep;
            _maxWidthSearchSteps = parameters.MaxWidthSearchSteps;
            _allowAtlasRotation = parameters.AllowAtlasRotation;
        }

        public async Task<Texture2D> GenerateAtlasTextureAsync(
            IReadOnlyList<Geometry3D<VertexData>> geometries,
            Texture2D texArray,
            float[] exposures)
        {
            var layout = Build(geometries);
            var geo = CreateAtlasGeometry(layout);

            RemapGeometryUvs(geometries, layout);

            var gl = OpenGLRender.Current!.GL;

            var texture = new Texture2D()
            {
                MinFilter = ScaleFilter.Linear,
                MagFilter = ScaleFilter.Linear,
                Format = TextureFormat.Rgb8,
                Width = (uint)layout.AtlasWidth,
                Height = (uint)layout.AtlasHeight,
            };

            texture.NotifyChanged();

            await EngineApp.RenderThread;

            var glTex = texture.ToGlTexture();

            GlImageProc.PrepareFrameBuffer(gl, glTex);

            string[] features = [];

            var hasExposure = exposures.Length > 0;

            if (hasExposure)
                features = [$"IMG_COUNT {exposures.Length}", "USE_EXPOSURE"];

            var prog = GlImageProc.LoadProgram(
                gl,
                "[XrEngine.Reconstruct]multi_tex.frag",
                "image_proc.vert",
                features,
                []);

            prog.LoadTexture(texArray, 0);

            if (hasExposure)
                prog.SetUniform("uExposure", exposures!);

            var mesh = new TriangleMesh(geo);

            using var vs = new GlVertexSourceHandler<VertexData, uint>(gl, mesh);

            vs.Update();

            var glState = GlState.Current;

            glState.SetView(new Rect2I
            {
                Width = glTex.Width,
                Height = glTex.Height
            });

            glState.SetAlphaMode(AlphaMode.Opaque);
            glState.SetWriteDepth(false);
            glState.SetUseDepth(false);
            glState.SetWriteColor(true);
            glState.Commit();

            vs.Bind();
            vs.Draw();

            XrEngine.Log.Info(this, "Final atlas {0}x{1}", layout.AtlasWidth, layout.AtlasHeight);

            return texture;
        }

        public TextureAtlasLayout Build(IReadOnlyList<Geometry3D<VertexData>> geometries)
        {
            var usedPoints = CollectUsedPoints(geometries);

            var rects = new List<OrientedRect>();

            foreach (var item in usedPoints)
            {
                if (item.Value.Count == 0)
                    continue;

                rects.Add(CreateOrientedRect(item.Key, item.Value));
            }

            var pack = Pack(rects);

            var entries = pack.Items
                .OrderBy(a => a.Rect.ImageIndex)
                .Select(a =>
                {
                    var atlasWidth = a.Rotated ? a.Rect.ContentHeight : a.Rect.ContentWidth;
                    var atlasHeight = a.Rotated ? a.Rect.ContentWidth : a.Rect.ContentHeight;

                    return new TextureAtlasEntry(
                        a.Rect.ImageIndex,
                        a.Rect.Origin,
                        a.Rect.AxisX,
                        a.Rect.AxisY,
                        a.Rect.Width,
                        a.Rect.Height,
                        a.X + _padding,
                        a.Y + _padding,
                        atlasWidth,
                        atlasHeight,
                        a.Rotated);
                })
                .ToArray();

            var sourceTextureCount = _sourceTextureCount > 0 ? _sourceTextureCount : usedPoints.Count;

            var fullMemory = (long)sourceTextureCount * _textureWidth * _textureHeight * _bytesPerPixel;
            var orientedBoundsMemory = rects.Sum(a => a.Area * _bytesPerPixel);
            var atlasMemory = (long)pack.Width * pack.Height * _bytesPerPixel;

            var layout = new TextureAtlasLayout(
                pack.Width,
                pack.Height,
                fullMemory,
                orientedBoundsMemory,
                atlasMemory,
                entries);

            LogLayout(layout);

            return layout;
        }

        public SimpleGeometry3D CreateAtlasGeometry(TextureAtlasLayout layout)
        {
            var vertices = new VertexData[layout.Entries.Count * 4];
            var indices = new uint[layout.Entries.Count * 6];

            var vertexOffset = 0;
            var indexOffset = 0;

            foreach (var entry in layout.Entries)
            {
                var atlasX0 = entry.AtlasX;
                var atlasY0 = entry.AtlasY;
                var atlasX1 = entry.AtlasX + entry.AtlasWidth;
                var atlasY1 = entry.AtlasY + entry.AtlasHeight;

                var p0 = new Vector2(atlasX0, atlasY0);
                var p1 = new Vector2(atlasX1, atlasY0);
                var p2 = new Vector2(atlasX1, atlasY1);
                var p3 = new Vector2(atlasX0, atlasY1);

                var tangent = new Vector4(entry.ImageIndex, 0.0f, 0.0f, 0.0f);

                vertices[vertexOffset + 0] = new VertexData
                {
                    Pos = ToAtlasClipPosition(p0, layout.AtlasWidth, layout.AtlasHeight),
                    Normal = Vector3.UnitZ,
                    UV = AtlasPixelToSourceUv(entry, p0),
                    Tangent = tangent
                };

                vertices[vertexOffset + 1] = new VertexData
                {
                    Pos = ToAtlasClipPosition(p1, layout.AtlasWidth, layout.AtlasHeight),
                    Normal = Vector3.UnitZ,
                    UV = AtlasPixelToSourceUv(entry, p1),
                    Tangent = tangent
                };

                vertices[vertexOffset + 2] = new VertexData
                {
                    Pos = ToAtlasClipPosition(p2, layout.AtlasWidth, layout.AtlasHeight),
                    Normal = Vector3.UnitZ,
                    UV = AtlasPixelToSourceUv(entry, p2),
                    Tangent = tangent
                };

                vertices[vertexOffset + 3] = new VertexData
                {
                    Pos = ToAtlasClipPosition(p3, layout.AtlasWidth, layout.AtlasHeight),
                    Normal = Vector3.UnitZ,
                    UV = AtlasPixelToSourceUv(entry, p3),
                    Tangent = tangent
                };

                indices[indexOffset + 0] = (uint)(vertexOffset + 0);
                indices[indexOffset + 1] = (uint)(vertexOffset + 1);
                indices[indexOffset + 2] = (uint)(vertexOffset + 2);

                indices[indexOffset + 3] = (uint)(vertexOffset + 0);
                indices[indexOffset + 4] = (uint)(vertexOffset + 2);
                indices[indexOffset + 5] = (uint)(vertexOffset + 3);

                vertexOffset += 4;
                indexOffset += 6;
            }

            return new SimpleGeometry3D
            {
                VerticesArray = vertices,
                Indices = indices,
                ActiveComponents = VertexComponent.Position | VertexComponent.UV0 | VertexComponent.Tangent
            };
        }

        public void RemapGeometryUvs(IReadOnlyList<Geometry3D<VertexData>> geometries, TextureAtlasLayout layout)
        {
            foreach (var geometry in geometries)
            {
                var vertices = geometry.Vertices;

                if (vertices == null || vertices.Length == 0)
                    continue;

                for (var i = 0; i < vertices.Length; i++)
                {
                    var vertex = vertices[i];
                    var imageIndex = GetImageIndex(vertex);

                    if (!layout.TryGetEntry(imageIndex, out var entry))
                        continue;

                    vertex.UV = RemapUv(vertex.UV, entry, layout.AtlasWidth, layout.AtlasHeight);
                    vertices[i] = vertex;
                }
            }
        }

        public Vector2 RemapUv(Vector2 sourceUv, TextureAtlasEntry entry, int atlasWidth, int atlasHeight)
        {
            var sourcePixel = new Vector2(
                sourceUv.X * _textureWidth,
                sourceUv.Y * _textureHeight);

            var delta = sourcePixel - entry.SourceOrigin;

            var localX = Vector2.Dot(delta, entry.SourceAxisX);
            var localY = Vector2.Dot(delta, entry.SourceAxisY);

            var atlasPixel = SourceLocalToAtlasPixel(entry, localX, localY);

            return new Vector2(
                atlasPixel.X / atlasWidth,
                atlasPixel.Y / atlasHeight);
        }

        private Vector2 AtlasPixelToSourceUv(TextureAtlasEntry entry, Vector2 atlasPixel)
        {
            var local = AtlasPixelToSourceLocal(entry, atlasPixel);
            var sourcePixel = entry.GetSourcePoint(local.X, local.Y);

            return new Vector2(
                sourcePixel.X / _textureWidth,
                sourcePixel.Y / _textureHeight);
        }

        private static Vector2 AtlasPixelToSourceLocal(TextureAtlasEntry entry, Vector2 atlasPixel)
        {
            var x = atlasPixel.X - entry.AtlasX;
            var y = atlasPixel.Y - entry.AtlasY;

            if (!entry.AtlasRotated)
            {
                return new Vector2(
                    x / entry.AtlasWidth * entry.SourceWidth,
                    y / entry.AtlasHeight * entry.SourceHeight);
            }

            return new Vector2(
                y / entry.AtlasHeight * entry.SourceWidth,
                (1.0f - x / entry.AtlasWidth) * entry.SourceHeight);
        }

        private static Vector2 SourceLocalToAtlasPixel(TextureAtlasEntry entry, float localX, float localY)
        {
            if (!entry.AtlasRotated)
            {
                return new Vector2(
                    entry.AtlasX + localX / entry.SourceWidth * entry.AtlasWidth,
                    entry.AtlasY + localY / entry.SourceHeight * entry.AtlasHeight);
            }

            return new Vector2(
                entry.AtlasX + (1.0f - localY / entry.SourceHeight) * entry.AtlasWidth,
                entry.AtlasY + localX / entry.SourceWidth * entry.AtlasHeight);
        }

        private static Vector3 ToAtlasClipPosition(Vector2 atlasPixel, int atlasWidth, int atlasHeight)
        {
            return new Vector3(
                atlasPixel.X / atlasWidth * 2.0f - 1.0f,
                atlasPixel.Y / atlasHeight * 2.0f - 1.0f,
                0.0f);
        }

        private Dictionary<int, List<Vector2>> CollectUsedPoints(IReadOnlyList<Geometry3D<VertexData>> geometries)
        {
            var result = new Dictionary<int, List<Vector2>>();

            foreach (var geometry in geometries)
            {
                var vertices = geometry.Vertices;
                var indices = geometry.Indices;

                if (vertices == null || vertices.Length == 0)
                    continue;

                if (indices != null && indices.Length > 0)
                {
                    for (var i = 0; i + 2 < indices.Length; i += 3)
                    {
                        var i0 = (int)indices[i + 0];
                        var i1 = (int)indices[i + 1];
                        var i2 = (int)indices[i + 2];

                        if (i0 < 0 || i0 >= vertices.Length ||
                            i1 < 0 || i1 >= vertices.Length ||
                            i2 < 0 || i2 >= vertices.Length)
                        {
                            continue;
                        }

                        var v0 = vertices[i0];
                        var v1 = vertices[i1];
                        var v2 = vertices[i2];

                        var imageIndex = GetImageIndex(v0);

                        if (GetImageIndex(v1) != imageIndex || GetImageIndex(v2) != imageIndex)
                            continue;

                        if (!IsUvDefined(v0.UV) || !IsUvDefined(v1.UV) || !IsUvDefined(v2.UV))
                            continue;

                        AddPoint(result, imageIndex, ToPixel(v0.UV));
                        AddPoint(result, imageIndex, ToPixel(v1.UV));
                        AddPoint(result, imageIndex, ToPixel(v2.UV));
                    }
                }
                else
                {
                    foreach (var vertex in vertices)
                    {
                        if (!IsUvDefined(vertex.UV))
                            continue;

                        AddPoint(result, GetImageIndex(vertex), ToPixel(vertex.UV));
                    }
                }
            }

            return result;
        }

        private OrientedRect CreateOrientedRect(int imageIndex, List<Vector2> points)
        {
            var hull = CreateConvexHull(points);

            if (hull.Count == 1)
            {
                return new OrientedRect
                {
                    ImageIndex = imageIndex,
                    Origin = hull[0] - new Vector2(0.5f + _sourceBorderPixels),
                    AxisX = Vector2.UnitX,
                    AxisY = Vector2.UnitY,
                    Width = 1.0f + _sourceBorderPixels * 2.0f,
                    Height = 1.0f + _sourceBorderPixels * 2.0f,
                    ContentWidth = Align((int)MathF.Ceiling(1.0f + _sourceBorderPixels * 2.0f), _rectAlignment),
                    ContentHeight = Align((int)MathF.Ceiling(1.0f + _sourceBorderPixels * 2.0f), _rectAlignment)
                };
            }

            var bestArea = float.MaxValue;
            var bestOrigin = Vector2.Zero;
            var bestAxisX = Vector2.UnitX;
            var bestAxisY = Vector2.UnitY;
            var bestWidth = 0.0f;
            var bestHeight = 0.0f;

            for (var i = 0; i < hull.Count; i++)
            {
                var p0 = hull[i];
                var p1 = hull[(i + 1) % hull.Count];

                var edge = p1 - p0;

                if (edge.LengthSquared() <= 0.000001f)
                    continue;

                var axisX = Vector2.Normalize(edge);
                var axisY = new Vector2(-axisX.Y, axisX.X);

                var minX = float.MaxValue;
                var minY = float.MaxValue;
                var maxX = float.MinValue;
                var maxY = float.MinValue;

                foreach (var point in hull)
                {
                    var x = Vector2.Dot(point, axisX);
                    var y = Vector2.Dot(point, axisY);

                    if (x < minX)
                        minX = x;

                    if (y < minY)
                        minY = y;

                    if (x > maxX)
                        maxX = x;

                    if (y > maxY)
                        maxY = y;
                }

                ExpandThinAxis(ref minX, ref maxX);
                ExpandThinAxis(ref minY, ref maxY);

                minX -= _sourceBorderPixels;
                minY -= _sourceBorderPixels;
                maxX += _sourceBorderPixels;
                maxY += _sourceBorderPixels;

                var width = maxX - minX;
                var height = maxY - minY;
                var area = width * height;

                if (area >= bestArea)
                    continue;

                bestArea = area;
                bestAxisX = axisX;
                bestAxisY = axisY;
                bestOrigin = axisX * minX + axisY * minY;
                bestWidth = width;
                bestHeight = height;
            }

            return new OrientedRect
            {
                ImageIndex = imageIndex,
                Origin = bestOrigin,
                AxisX = bestAxisX,
                AxisY = bestAxisY,
                Width = bestWidth,
                Height = bestHeight,
                ContentWidth = Align((int)MathF.Ceiling(bestWidth), _rectAlignment),
                ContentHeight = Align((int)MathF.Ceiling(bestHeight), _rectAlignment)
            };
        }

        private PackResult Pack(List<OrientedRect> rects)
        {
            if (rects.Count == 0)
            {
                return new PackResult
                {
                    Width = 1,
                    Height = 1,
                    Items = new List<PackedItem>()
                };
            }

            var items = rects
                .Select(a => new PackItem
                {
                    Rect = a,
                    Width = a.ContentWidth + _padding * 2,
                    Height = a.ContentHeight + _padding * 2,
                    RotatedWidth = a.ContentHeight + _padding * 2,
                    RotatedHeight = a.ContentWidth + _padding * 2
                })
                .OrderByDescending(a => Math.Max(a.Width * a.Height, a.RotatedWidth * a.RotatedHeight))
                .ThenByDescending(a => Math.Max(a.Width, a.Height))
                .ToList();

            var totalWidth = items.Sum(a => a.Width);
            var totalArea = items.Sum(a => (long)a.Width * a.Height);

            var minWidth = items.Max(a =>
                _allowAtlasRotation
                    ? Math.Min(a.Width, a.RotatedWidth)
                    : a.Width);

            var range = Math.Max(0, totalWidth - minWidth);
            var step = _widthSearchStep;

            if (range / Math.Max(1, step) > _maxWidthSearchSteps)
                step = Align(range / _maxWidthSearchSteps, _widthSearchStep);

            var best = new PackResult
            {
                Width = totalWidth,
                Height = int.MaxValue,
                Items = new List<PackedItem>()
            };

            for (var width = Align(minWidth, step); width <= totalWidth; width += step)
            {
                if (!TryPack(items, width, out var candidate))
                    continue;

                if (candidate.Area < best.Area)
                {
                    best = candidate;
                    continue;
                }

                if (candidate.Area == best.Area &&
                    Math.Max(candidate.Width, candidate.Height) < Math.Max(best.Width, best.Height))
                {
                    best = candidate;
                }
            }

            var squareGuess = Align((int)Math.Ceiling(Math.Sqrt(totalArea)), step);

            if (TryPack(items, Math.Max(minWidth, squareGuess), out var squareCandidate))
            {
                if (squareCandidate.Area < best.Area)
                    best = squareCandidate;
            }

            best.Width = Align(best.Width, _rectAlignment);
            best.Height = Align(best.Height, _rectAlignment);

            return best;
        }

        private bool TryPack(List<PackItem> items, int atlasWidth, out PackResult result)
        {
            var nodes = new List<SkylineNode>
        {
            new SkylineNode
            {
                X = 0,
                Y = 0,
                Width = atlasWidth
            }
        };

            var packed = new List<PackedItem>(items.Count);
            var atlasHeight = 0;

            foreach (var item in items)
            {
                var bestNode = -1;
                var bestX = 0;
                var bestY = 0;
                var bestWidth = 0;
                var bestHeight = 0;
                var bestRotated = false;
                var bestBottom = int.MaxValue;

                FindBestPosition(nodes, atlasWidth, item, item.Width, item.Height, false, ref bestNode, ref bestX, ref bestY, ref bestWidth, ref bestHeight, ref bestRotated, ref bestBottom);

                if (_allowAtlasRotation && item.Width != item.RotatedWidth)
                    FindBestPosition(nodes, atlasWidth, item, item.RotatedWidth, item.RotatedHeight, true, ref bestNode, ref bestX, ref bestY, ref bestWidth, ref bestHeight, ref bestRotated, ref bestBottom);

                if (bestNode < 0)
                {
                    result = default;
                    return false;
                }

                AddSkylineLevel(nodes, bestNode, bestX, bestY, bestWidth, bestHeight);

                packed.Add(new PackedItem
                {
                    Rect = item.Rect,
                    X = bestX,
                    Y = bestY,
                    Width = bestWidth,
                    Height = bestHeight,
                    Rotated = bestRotated
                });

                var bottom = bestY + bestHeight;

                if (bottom > atlasHeight)
                    atlasHeight = bottom;
            }

            result = new PackResult
            {
                Width = atlasWidth,
                Height = atlasHeight,
                Items = packed
            };

            return true;
        }

        private static void FindBestPosition(
            List<SkylineNode> nodes,
            int atlasWidth,
            PackItem item,
            int width,
            int height,
            bool rotated,
            ref int bestNode,
            ref int bestX,
            ref int bestY,
            ref int bestWidth,
            ref int bestHeight,
            ref bool bestRotated,
            ref int bestBottom)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (!CanFit(nodes, i, atlasWidth, width, height, out var y))
                    continue;

                var bottom = y + height;

                if (bottom > bestBottom)
                    continue;

                if (bottom == bestBottom && nodes[i].X >= bestX)
                    continue;

                bestNode = i;
                bestX = nodes[i].X;
                bestY = y;
                bestWidth = width;
                bestHeight = height;
                bestRotated = rotated;
                bestBottom = bottom;
            }
        }

        private static bool CanFit(List<SkylineNode> nodes, int index, int atlasWidth, int width, int height, out int y)
        {
            var x = nodes[index].X;

            y = nodes[index].Y;

            if (x + width > atlasWidth)
                return false;

            var widthLeft = width;
            var i = index;

            while (widthLeft > 0)
            {
                if (i >= nodes.Count)
                    return false;

                if (nodes[i].Y > y)
                    y = nodes[i].Y;

                widthLeft -= nodes[i].Width;
                i++;
            }

            return true;
        }

        private static void AddSkylineLevel(List<SkylineNode> nodes, int index, int x, int y, int width, int height)
        {
            nodes.Insert(index, new SkylineNode
            {
                X = x,
                Y = y + height,
                Width = width
            });

            for (var i = index + 1; i < nodes.Count; i++)
            {
                var previous = nodes[i - 1];
                var current = nodes[i];

                var previousEnd = previous.X + previous.Width;

                if (current.X >= previousEnd)
                    break;

                var shrink = previousEnd - current.X;

                current.X += shrink;
                current.Width -= shrink;

                if (current.Width <= 0)
                {
                    nodes.RemoveAt(i);
                    i--;
                }
                else
                {
                    nodes[i] = current;
                    break;
                }
            }

            for (var i = 0; i + 1 < nodes.Count; i++)
            {
                if (nodes[i].Y != nodes[i + 1].Y)
                    continue;

                var merged = nodes[i];
                merged.Width += nodes[i + 1].Width;

                nodes[i] = merged;
                nodes.RemoveAt(i + 1);

                i--;
            }
        }

        private List<Vector2> CreateConvexHull(List<Vector2> points)
        {
            var sorted = points
                .OrderBy(a => a.X)
                .ThenBy(a => a.Y)
                .ToList();

            if (sorted.Count <= 1)
                return sorted;

            var hull = new List<Vector2>();

            foreach (var point in sorted)
            {
                while (hull.Count >= 2 && Vector2.Cross(hull[hull.Count - 1] - hull[hull.Count - 2], point - hull[hull.Count - 1]) <= 0.0f)
                    hull.RemoveAt(hull.Count - 1);

                hull.Add(point);
            }

            var lowerCount = hull.Count;

            for (var i = sorted.Count - 2; i >= 0; i--)
            {
                var point = sorted[i];

                while (hull.Count > lowerCount && Vector2.Cross(hull[hull.Count - 1] - hull[hull.Count - 2], point - hull[hull.Count - 1]) <= 0.0f)
                    hull.RemoveAt(hull.Count - 1);

                hull.Add(point);
            }

            if (hull.Count > 1)
                hull.RemoveAt(hull.Count - 1);

            return hull;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector2 ToPixel(Vector2 uv)
        {
            return new Vector2(
                uv.X * _textureWidth,
                uv.Y * _textureHeight);
        }

        private static void AddPoint(Dictionary<int, List<Vector2>> result, int imageIndex, Vector2 point)
        {
            if (!result.TryGetValue(imageIndex, out var points))
            {
                points = [];
                result[imageIndex] = points;
            }

            points.Add(point);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetImageIndex(VertexData vertex)
        {
            return (int)MathF.Round(vertex.Tangent.X);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsUvDefined(Vector2 uv)
        {
            return uv.X >= 0.0f && uv.X <= 1.0f &&
                   uv.Y >= 0.0f && uv.Y <= 1.0f;
        }

        private static void ExpandThinAxis(ref float min, ref float max)
        {
            if (max - min >= 1.0f)
                return;

            var center = (min + max) * 0.5f;

            min = center - 0.5f;
            max = center + 0.5f;
        }

        private void LogLayout(TextureAtlasLayout layout)
        {
            LogLine($"Atlas layout");
            LogLine($"  source texture: {_textureWidth}x{_textureHeight}, bpp={_bytesPerPixel}");
            LogLine($"  source textures: {(_sourceTextureCount > 0 ? _sourceTextureCount : layout.Entries.Count)}");
            LogLine($"  entries: {layout.Entries.Count}");
            LogLine($"  atlas: {layout.AtlasWidth}x{layout.AtlasHeight}");

            LogLine($"  full memory: {ToMb(layout.FullMemory):0.00} MB");
            LogLine($"  oriented bounds memory: {ToMb(layout.OrientedBoundsMemory):0.00} MB");
            LogLine($"  atlas memory: {ToMb(layout.AtlasMemory):0.00} MB");
            LogLine($"  saved memory: {ToMb(layout.SavedMemory):0.00} MB ({GetPercent(layout.SavedMemory, layout.FullMemory):0.0}%)");
            LogLine($"  atlas efficiency: {GetPercent(layout.OrientedBoundsMemory, layout.AtlasMemory):0.0}%");

            foreach (var entry in layout.Entries)
            {
                LogLine(
                    $"  image {entry.ImageIndex}: " +
                    $"srcQuad " +
                    $"[{Format(entry.SourceP0)} {Format(entry.SourceP1)} {Format(entry.SourceP2)} {Format(entry.SourceP3)}] " +
                    $"srcSize {entry.SourceWidth:0.0}x{entry.SourceHeight:0.0} -> " +
                    $"atlas {entry.AtlasX},{entry.AtlasY} {entry.AtlasWidth}x{entry.AtlasHeight} " +
                    $"rot={(entry.AtlasRotated ? 90 : 0)}");
            }
        }

        private void LogLine(string message)
        {
            if (Log != null)
                Log(message);
            else
                Debug.WriteLine(message);
        }

        private static string Format(Vector2 value)
        {
            return $"{value.X:0.0},{value.Y:0.0}";
        }

        private static int Align(int value, int alignment)
        {
            if (alignment <= 1)
                return value;

            return (value + alignment - 1) / alignment * alignment;
        }

        private static double ToMb(long bytes)
        {
            return bytes / 1024.0 / 1024.0;
        }

        private static double GetPercent(long value, long total)
        {
            if (total == 0)
                return 0.0;

            return value * 100.0 / total;
        }

        public Action<string>? Log { get; set; }
    }
}