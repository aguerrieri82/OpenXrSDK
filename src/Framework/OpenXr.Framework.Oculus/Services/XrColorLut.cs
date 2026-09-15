using Silk.NET.OpenXR;

namespace OpenXr.Framework.Oculus
{

    public class XrColorLut : IDisposable
    {
        readonly XrApp _app;
        readonly METAPassthroughColorLut _ext;
        private PassthroughColorLutMETA _handle;

        internal unsafe XrColorLut(XrApp app, METAPassthroughColorLut ext, PassthroughFB passthrough, uint resolution, PassthroughColorLutChannelsMETA channels, byte[] data)
        {
            _app = app;
            _ext = ext;

            fixed (byte* pData = data)
            {
                var info = new PassthroughColorLutCreateInfoMETA
                {
                    Type = StructureType.PassthroughColorLutCreateInfoMeta,
                    Channels = channels,
                    Resolution = resolution,
                    Data = new PassthroughColorLutDataMETA
                    {
                        BufferSize = (uint)data.Length,
                        Buffer = pData
                    }
                };

                _app.CheckResult(_ext.CreatePassthroughColorLutMETA(passthrough, ref info, ref _handle), "CreatePassthroughColorLutMETA");
            }

            Resolution = resolution;
            Channels = channels;
            BufferSize = data.Length;
        }

        public unsafe void Update(byte[] data)
        {
            if (_handle.Handle == 0)
                throw new ObjectDisposedException(nameof(XrColorLut));

            if (data.Length != BufferSize)
                throw new ArgumentException($"LUT data must contain {BufferSize} bytes", nameof(data));

            fixed (byte* pData = data)
            {
                var info = new PassthroughColorLutUpdateInfoMETA
                {
                    Type = StructureType.PassthroughColorLutUpdateInfoMeta,
                    Data = new PassthroughColorLutDataMETA
                    {
                        BufferSize = (uint)data.Length,
                        Buffer = pData
                    }
                };

                _app.CheckResult(_ext.UpdatePassthroughColorLutMETA(_handle, ref info), "UpdatePassthroughColorLutMETA");
            }
        }

        public void Dispose()
        {
            if (_handle.Handle == 0)
                return;

            _app.CheckResult(_ext.DestroyPassthroughColorLutMETA(_handle), "DestroyPassthroughColorLutMETA");
            _handle.Handle = 0;

            GC.SuppressFinalize(this);
        }

        public static implicit operator PassthroughColorLutMETA(XrColorLut value)
        {
            return value._handle;
        }

        public uint Resolution { get; }

        public PassthroughColorLutChannelsMETA Channels { get; }

        public int BufferSize { get; }

        public PassthroughColorLutMETA Handle => _handle;
    }
}