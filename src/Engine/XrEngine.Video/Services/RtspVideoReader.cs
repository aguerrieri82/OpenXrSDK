using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualCamera.IPCamera;
using XrEngine.Video.Abstraction;
using XrMath;

namespace XrEngine.Video
{
    public class RtspVideoReader : IVideoReader 
    {
        RtspClient? _client;
        private RtspSession? _session;
        private RtspStream? _videoStream;
        private RtpH264Client? _h264Stream;

        private Thread? _readThread;
        private ConcurrentQueue<byte[]> _readBuffer;
        private IVideoCodec? _videoCodec;
        private string _streamName;
        private int _frameCount;
        private DateTime _frameCountStart;
        private FrameBuffer _dstBuffer;

        public RtspVideoReader()
        {
            UdpPort = 1400;
            _readBuffer = new ConcurrentQueue<byte[]>();    
        }

        public void Open(Uri uri)
        {
            _client = new RtspClient();
            _client.Connect(uri.Host, uri.Port);

            _streamName = uri.PathAndQuery.Substring(1);

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':');
                _client.Authenticate(_streamName, parts[0], parts[1]);
            }

            var streams = _client.Describe(_streamName);
        
            _videoStream = streams.FirstOrDefault(a => a.Type == RtspStreamType.Video);

            if (_videoStream == null)
                throw new InvalidOperationException();

            if (_videoStream.Data != null && _videoStream.Data.Count > 0)
            {
                var format = new VideoFormat();
                new SpsDecoder().Decode(_videoStream.Data[0], ref format);
                FrameSize = new Size2I((uint)format.Width, (uint)format.Height);
            }

            _session = _client.Setup(_videoStream, UdpPort);

            if (_session == null)
                throw new InvalidOperationException();

            if (_videoStream.Format == "H264")
            {
                _videoCodec = Context.RequireInstance<IVideoCodec>();
                _videoCodec.Open(VideoCodecMode.Decode, "video/h264", new VideoFormat
                {
                    Width = (int)FrameSize.Width,
                    Height = (int)FrameSize.Height,
                    ImageFormat = ImageFormat.Rgb32
                });

                _h264Stream = new RtpH264Client(UdpPort);
                _h264Stream.Open();

                _readThread = new Thread(ReadLoop);
                _readThread.Name = "Rtsp Video Read Loop";
                _readThread.Start();

                _dstBuffer = FrameBuffer.Allocate((int)(FrameSize.Width * FrameSize.Height * 4));
            }
            else
                throw new NotSupportedException();

            if (!_client.Play(_streamName, _session!))
                throw new InvalidOperationException();

        }

        protected void ReadLoop()
        {
            while (_h264Stream != null)
            {
                try
                {
                    var nalData = _h264Stream?.ReadNalUnit();

                    if (nalData != null)
                    {
                        lock (_readBuffer!)
                            _readBuffer.Enqueue(nalData);
                    }

                }
                catch
                {

                }
            }
        }

        public bool TryDecodeNextFrame(TextureData data)
        {
            Debug.Assert(_videoCodec != null);  

            if (_readBuffer == null || !_readBuffer.TryDequeue(out var buffer))
                return false;

            var srcBuffer = new FrameBuffer() { ByteArray = buffer };


            try
            {
                if (_videoCodec.Convert(srcBuffer, ref _dstBuffer))
                {
                    data.Width = FrameSize.Width;
                    data.Height = FrameSize.Height;
                    data.Format = TextureFormat.Rgba32;
                    data.Data = new Memory<byte>(_dstBuffer.ByteArray);

                    _frameCount++;

                    var time = (DateTime.Now - _frameCountStart).TotalSeconds;
                    if ((DateTime.Now - _frameCountStart).TotalSeconds > 3)
                    {
                        Log.Info(this, "Fps: {0}", (_frameCount / time));
                        _frameCount = 0;
                        _frameCountStart = DateTime.Now;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Warn(this, "{0} ({1})", ex.Message, srcBuffer.ByteArray[3]);
            }

            return false;
        }

        public void Close()
        {
            if (_client != null)
            {
                if (_session != null && _videoStream != null)
                {
                    _client.TearDown(_streamName, _session);
                    _videoStream = null;
                    _session = null;
                }

                _client.Close();
                _client = null;
            }

            if (_h264Stream != null)
            {
                _h264Stream.Close();
                _h264Stream = null;
            }

            if (_readThread != null)
            {
                _readThread.Join();
                _readThread = null;
            }

            if (_videoCodec != null)
            {
                _videoCodec.Dispose();
                _videoCodec = null;
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        public int UdpPort { get; set; }

        public double FrameRate => 0;

        public Size2I FrameSize {get; protected set;}

        public Texture2D? OutTexture { get; set; }
    }
}
