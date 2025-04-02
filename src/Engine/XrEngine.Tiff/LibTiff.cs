using Common.Interop;
using System.Runtime.InteropServices;
using System.Text;

namespace XrEngine.Tiff
{
    public static class LibTiff
    {
        private const string LibTiffDll = "tiff";

        public enum TiffTag
        {
            ImageWidth = 256,          // Width of the image
            ImageLength = 257,         // Height of the image
            BitsPerSample = 258,       // Bits per sample
            Compression = 259,         // Compression scheme
            Photometric = 262,         // Photometric interpretation
            StripOffsets = 273,        // Strip offsets
            RowsPerStrip = 278,        // Rows per strip
            StripByteCounts = 279,     // Strip byte counts
            XResolution = 282,         // X resolution
            YResolution = 283,         // Y resolution
            PlanarConfig = 284,        // Planar configuration
            ResolutionUnit = 296,      // Resolution unit
            SampleFormat = 339,
            TileWidth = 322,
            TileHeight = 323,

            GeoTiePoints = 33922,      // GeoTIFF tie points
            GeoPixelScale = 33550,     // GeoTIFF pixel scale
            GeoTransMatrix = 34264,    // GeoTIFF transformation matrix
            GdalNoData = 42113,        // GDAL NoData value
            GdalMetadata = 42112,      // GDAL metadata 

            GeoKeyDirectory = 34735,
            GeoAsciiParamsTag = 34737,
            GeoDoubleParamsTag = 34736,

        }

        public enum GeoTag
        {
            // GeoTIFF Configuration Keys
            GTModelTypeGeoKey = 1024,         // Model Type: e.g., Projected Coordinate System
            GTRasterTypeGeoKey = 1025,        // Raster Type: e.g., Pixel, Vector
            GTCitationGeoKey = 1026,          // Citation: Documentation for the configuration

            // Geographic Coordinate System Parameter Keys
            GeographicTypeGeoKey = 2048,      // Geographic Type: e.g., WGS 84
            GeogCitationGeoKey = 2049,        // Citation: Documentation for the geographic coordinate system
            GeogGeodeticDatumGeoKey = 2050,   // Geodetic Datum: e.g., WGS 84
            GeogPrimeMeridianGeoKey = 2051,   // Prime Meridian: e.g., Greenwich
            GeogLinearUnitsGeoKey = 2052,     // Linear Units: e.g., Meter
            GeogLinearUnitSizeGeoKey = 2053,  // Linear Unit Size: e.g., 1.0
            GeogAngularUnitsGeoKey = 2054,    // Angular Units: e.g., Degree
            GeogAngularUnitSizeGeoKey = 2055, // Angular Unit Size: e.g., 0.0174532925199433 (radians)
            GeogEllipsoidGeoKey = 2056,       // Ellipsoid: e.g., WGS84
            GeogSemiMajorAxisGeoKey = 2057,   // Semi-Major Axis: e.g., 6378137.0
            GeogSemiMinorAxisGeoKey = 2058,   // Semi-Minor Axis: e.g., 6356752.314245
            GeogInvFlatteningGeoKey = 2059,   // Inverse Flattening: e.g., 298.257223563
            GeogAzimuthUnitsGeoKey = 2060,    // Azimuth Units: e.g., Degree
            GeogPrimeMeridianLongGeoKey = 2061, // Prime Meridian Longitude: e.g., 0.0

            // Projected Coordinate System Parameter Keys
            ProjectedCSTypeGeoKey = 3072,     // Projected Coordinate System Type: e.g., UTM
            PCSCitationGeoKey = 3073,         // Citation: Documentation for the projected coordinate system
            ProjectionGeoKey = 3074,          // Projection: e.g., Transverse Mercator
            ProjCoordTransGeoKey = 3075,      // Coordinate Transformation: e.g., UTM
            ProjLinearUnitsGeoKey = 3076,     // Linear Units: e.g., Meter
            ProjLinearUnitSizeGeoKey = 3077,  // Linear Unit Size: e.g., 1.0
            ProjStdParallel1GeoKey = 3078,    // Standard Parallel 1: e.g., 40.0
            ProjStdParallel2GeoKey = 3079,    // Standard Parallel 2: e.g., 41.0
            ProjNatOriginLongGeoKey = 3080,   // Natural Origin Longitude: e.g., -75.0
            ProjNatOriginLatGeoKey = 3081,    // Natural Origin Latitude: e.g., 40.0
            ProjFalseEastingGeoKey = 3082,    // False Easting: e.g., 500000.0
            ProjFalseNorthingGeoKey = 3083,   // False Northing: e.g., 0.0
            ProjFalseOriginLongGeoKey = 3084, // False Origin Longitude: e.g., -75.0
            ProjFalseOriginLatGeoKey = 3085,  // False Origin Latitude: e.g., 40.0
            ProjFalseOriginEastingGeoKey = 3086, // False Origin Easting: e.g., 500000.0
            ProjFalseOriginNorthingGeoKey = 3087, // False Origin Northing: e.g., 0.0
            ProjCenterLongGeoKey = 3088,      // Center Longitude: e.g., -75.0
            ProjCenterLatGeoKey = 3089,       // Center Latitude: e.g., 40.0
            ProjCenterEastingGeoKey = 3090,   // Center Easting: e.g., 500000.0
            ProjCenterNorthingGeoKey = 3091,  // Center Northing: e.g., 0.0
            ProjScaleAtNatOriginGeoKey = 3092, // Scale at Natural Origin: e.g., 1.0
            ProjScaleAtCenterGeoKey = 3093,   // Scale at Center: e.g., 1.0
            ProjAzimuthAngleGeoKey = 3094,    // Azimuth Angle: e.g., 0.0
            ProjStraightVertPoleLongGeoKey = 3095, // Straight Vertical Pole Longitude: e.g., -75.0

            // Vertical Coordinate System Parameter Keys
            VerticalCSTypeGeoKey = 4096,      // Vertical Coordinate System Type: e.g., Geopotential
            VerticalCitationGeoKey = 4097,    // Citation: Documentation for the vertical coordinate system
            VerticalDatumGeoKey = 4098,       // Vertical Datum: e.g., NAVD88
            VerticalUnitsGeoKey = 4099        // Vertical Units: e.g., Meter
        }

        /// <summary>
        /// Photometric interpretation values for TIFF images.
        /// </summary>
        public enum TiffPhotometric
        {
            /// <summary>
            /// Grayscale, where black is represented by 0.
            /// </summary>
            MinIsBlack = 0,

            /// <summary>
            /// Grayscale, where white is represented by 0.
            /// </summary>
            MinIsWhite = 1,

            /// <summary>
            /// RGB color model.
            /// </summary>
            RGB = 2,

            /// <summary>
            /// Color mapped with a palette.
            /// </summary>
            Palette = 3,

            /// <summary>
            /// Transparency mask.
            /// </summary>
            Mask = 4,

            /// <summary>
            /// CMYK color model (separated).
            /// </summary>
            Separated = 5,

            /// <summary>
            /// YCbCr color model.
            /// </summary>
            YCbCr = 6,

            /// <summary>
            /// CIE L*a*b* color model.
            /// </summary>
            CIELab = 8,

            /// <summary>
            /// ICC Lab color model.
            /// </summary>
            ICCLab = 9,

            /// <summary>
            /// ITU LAB color model.
            /// </summary>
            ITULab = 10,

            /// <summary>
            /// Logarithmic luminance.
            /// </summary>
            LogL = 32844,

            /// <summary>
            /// Logarithmic luminance with chrominance.
            /// </summary>
            LogLuv = 32845
        }

        public enum GeoRasterType
        {
            RasterPixelIsArea = 1,
            RasterPixelIsPoint = 2
        }

        public enum GeoModelType
        {

            Projected = 1,
            Geographic = 2,
            Geocentric = 3
        }

        public enum SampleFormat
        {
            UnsignedInteger = 1,
            SignedInteger = 2,
            FloatingPoint = 3,
            Void = 4,
            ComplexInteger = 5,
            ComplexFloatingPoint = 6
        }


        struct GeoDirectoryHeader
        {
            public ushort Version;
            public ushort Revision;
            public ushort MinorRevision;
            public ushort KeyCount;

        }

        struct GeoDirectoryRawEntry
        {
            public GeoTag Key;
            public ushort TagLocation;
            public ushort ValueCount;
            public ushort ValueOffset;
        }

        public struct GeoDirectoryEntry
        {
            public GeoTag Key;

            public object Value;
        }

        public struct Tiff
        {
            public nint Handle;

        }

        // TIFFOpen
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern Tiff TIFFOpen(string filename, string mode);

        // TIFFClose
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIFFClose(this Tiff tiff);

        // TIFFGetField
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int TIFFGetField(this Tiff tiff, TiffTag tag, void* value);

        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int TIFFGetField(this Tiff tiff, TiffTag tag, out int count, void* value);

        // TIFFSetField
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIFFSetField(this Tiff tiff, int tag, IntPtr value);

        // TIFFReadDirectory
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIFFReadDirectorythis(Tiff tiff);

        // TIFFWriteDirectory
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIFFWriteDirectory(this Tiff tiff);

        // TIFFReadScanline
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern unsafe int TIFFReadScanline(this Tiff tiff, void* buffer, int row, short plane);

        // TIFFWriteScanline
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIFFWriteScanline(this Tiff tiff, IntPtr buffer, int row, short plane);

        // TIFFPrintDirectory
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIFFPrintDirectory(this Tiff tiff, IntPtr output, int flags);

        // TIFFScanlineSize
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIFFScanlineSize(this Tiff tiff);

        // TIFFSetWarningHandler (Optional)
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIFFSetWarningHandler(this Tiff handler);

        // TIFFSetErrorHandler (Optional)
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TIFFSetErrorHandler(this Tiff handler);

        // TIFFTileSize
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TIFFTileSize(this Tiff tiff);

        //TIFFReadTile
        [DllImport(LibTiffDll, CallingConvention = CallingConvention.Cdecl)]
        public static unsafe extern int TIFFReadTile(this Tiff tiff, void* buffer, uint x, uint y, uint z, ushort plane);

        public static unsafe double[] GetDoubleArrayField(this Tiff tiff, TiffTag tag)
        {
            double* data;
            TIFFGetField(tiff, tag, out var count, &data);
            return new Span<double>(data, count).ToArray();
        }

        public static unsafe ushort[] GetShortArrayField(this Tiff tiff, TiffTag tag)
        {
            short* data;
            TIFFGetField(tiff, tag, out var count, &data);
            return new Span<ushort>(data, count).ToArray();
        }

        public static unsafe string GetStringField(this Tiff tiff, TiffTag tag)
        {
            byte* data;
            TIFFGetField(tiff, tag, out var count, &data);
            return Encoding.ASCII.GetString(new Span<byte>(data, count)).Trim('\0');
        }

        public static unsafe int GetIntField(this Tiff tiff, TiffTag tag)
        {
            int result;
            TIFFGetField(tiff, tag, &result);
            return result;
        }

        public static unsafe TextureData Read(this Tiff tiff, bool flipY = false)
        {
            var w = GetIntField(tiff, TiffTag.ImageWidth);
            var h = GetIntField(tiff, TiffTag.ImageLength);
            var tw = GetIntField(tiff, TiffTag.TileWidth);
            var th = GetIntField(tiff, TiffTag.TileHeight);
            var bps = GetIntField(tiff, TiffTag.BitsPerSample);
            var sf = (SampleFormat)GetIntField(tiff, TiffTag.SampleFormat);
            var pm = (TiffPhotometric)GetIntField(tiff, TiffTag.Photometric);


            var result = new TextureData
            {
                Width = (uint)w,
                Height = (uint)h
            };

            if (pm == TiffPhotometric.MinIsBlack || pm == TiffPhotometric.MinIsWhite)
            {
                if (sf == SampleFormat.UnsignedInteger || sf == 0)
                {
                    if (bps == 16)
                        result.Format = TextureFormat.GrayInt16;
                    else if (bps == 8)
                        result.Format = TextureFormat.GrayInt8;
                    else
                        throw new NotSupportedException();
                }
                else if (sf == SampleFormat.SignedInteger)
                {
                    if (bps == 16)
                        result.Format = TextureFormat.GrayRawSInt16;
                    else
                        throw new NotSupportedException();
                }
                else if (sf == SampleFormat.FloatingPoint)
                {
                    if (bps == 32)
                        result.Format = TextureFormat.GrayFloat32;
                    else
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();
            }
            else if (pm == TiffPhotometric.RGB)
            {
                if (bps == 8)
                {
                    bps *= 3;
                    result.Format = TextureFormat.Rgb24;
                }
                else
                    throw new NotSupportedException();
            }
            else
                throw new NotSupportedException();

            var ps = (bps / 8);
            var lineSize = w * ps;

            var buffer = MemoryBuffer.Create<byte>((uint)(lineSize * h));
            result.Data = buffer;

            using var data = buffer.MemoryLock();

            if (flipY)
            {
                if (tw > 0)
                {
                    var tileSize = (uint)TIFFTileSize(tiff);
                    var tileBuf = MemoryBuffer.Create<byte>(tileSize);

                    using var tileBufData = buffer.MemoryLock();

                    var tLineSize = tileSize / th;

                    for (uint y = 0; y < h; y += (uint)th) // Iterate over tiles vertically
                    {
                        for (uint x = 0; x < w; x += (uint)tw) // Iterate over tiles horizontally
                        {
                            TIFFReadTile(tiff, tileBufData, x, y, 0, 0);

                            var curTh = Math.Min(h - y, th);
                            var mainBufOfs = data.Data + (x * ps) + ((h - y - 1) * lineSize);
                            var tileBufOfs = tileBufData.Data;
                            var cutTLineSize = Math.Min(tLineSize, (w - x) * ps);

                            for (uint y1 = 0; y1 < curTh; y1++)
                            {
                                EngineNativeLib.CopyMemory((nint)tileBufOfs, (nint)mainBufOfs, (uint)cutTLineSize);
                                mainBufOfs -= lineSize;
                                tileBufOfs += tLineSize;
                            }

                        }
                    }
                }
                else
                {
                    for (var y = 0; y < h; y++)
                        TIFFReadScanline(tiff, data.Data + ((h - y - 1) * lineSize), y, 0);
                }

            }
            else
            {
                if (tw > 0)
                {
                    var tileSize = (uint)TIFFTileSize(tiff);
                    var tileBuf = MemoryBuffer.Create<byte>(tileSize);

                    using var tileBufData = buffer.MemoryLock();

                    var tLineSize = tileSize / th;

                    for (uint y = 0; y < h; y += (uint)th) // Iterate over tiles vertically
                    {
                        for (uint x = 0; x < w; x += (uint)tw) // Iterate over tiles horizontally
                        {
                            TIFFReadTile(tiff, tileBufData, x, y, 0, 0);

                            var curTh = Math.Min(h - y, th);
                            var mainBufOfs = data.Data + (x * ps) + (y * lineSize);
                            var tileBufOfs = tileBufData.Data;
                            var cutTLineSize = Math.Min(tLineSize, (w - x) * ps);

                            for (uint y1 = 0; y1 < curTh; y1++)
                            {
                                EngineNativeLib.CopyMemory((nint)tileBufOfs, (nint)mainBufOfs, (uint)cutTLineSize);
                                mainBufOfs += lineSize;
                                tileBufOfs += tLineSize;
                            }

                        }
                    }
                }

                for (var y = 0; y < h; y++)
                    TIFFReadScanline(tiff, data.Data + (y * lineSize), y, 0);
            }


            return result;
        }

        public static unsafe GeoDirectoryEntry[] GetGeoDirectory(this Tiff tiff)
        {
            var data = GetShortArrayField(tiff, TiffTag.GeoKeyDirectory);

            var header = new GeoDirectoryHeader
            {
                Version = data[0],
                Revision = data[1],
                MinorRevision = data[2],
                KeyCount = data[3]
            };

            var result = new List<GeoDirectoryEntry>();

            var ascii = GetStringField(tiff, TiffTag.GeoAsciiParamsTag);

            var doubles = GetDoubleArrayField(tiff, TiffTag.GeoDoubleParamsTag);

            object GetValue(GeoDirectoryRawEntry entry)
            {
                if (entry.TagLocation == 0)
                    return entry.ValueOffset;

                if (entry.TagLocation == (ushort)TiffTag.GeoAsciiParamsTag)
                    return ascii.Substring(entry.ValueOffset, entry.ValueCount - 1);

                if (entry.TagLocation == (ushort)TiffTag.GeoDoubleParamsTag)
                {
                    if (entry.ValueCount == 1)
                        return doubles[entry.ValueOffset];
                    return doubles.AsSpan(entry.ValueOffset, entry.ValueCount).ToArray();
                }

                throw new NotSupportedException();
            }

            for (var i = 4; i < data.Length; i += 4)
            {
                var entry = new GeoDirectoryRawEntry
                {
                    Key = (GeoTag)data[i],
                    TagLocation = data[i + 1],
                    ValueCount = data[i + 2],
                    ValueOffset = data[i + 3]
                };

                result.Add(new GeoDirectoryEntry
                {
                    Key = entry.Key,
                    Value = GetValue(entry)
                });
            }

            return result.ToArray();

        }

    }
}
