using System.Diagnostics;
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
        private IVideoCodec? _videoCodec;
        private string? _streamName;
        private int _frameCount;
        private DateTime _frameCountStart;
        private FrameBuffer _dstBuffer;
        private DateTime _lastPingTime;


        public RtspVideoReader()
        {
            UdpPort = 1400;

        }

        public void Open(Uri uri)
        {
            //_out = new FileStream("d:\\out.h264", FileMode.Create, FileAccess.Write);   

            Log.Debug(this, "Rtsp: Connect");

            _client = new RtspClient();
            _client.Connect(uri.Host, uri.Port);

            _streamName = uri.PathAndQuery.Substring(1);

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':');
                _client.Authenticate(_streamName, parts[0], parts[1]);
            }

            _streamName = uri.ToString();


            Log.Debug(this, "Rtsp: Describe");

            var streams = _client.Describe(_streamName);

            _videoStream = streams.FirstOrDefault(a => a.Type == RtspStreamType.Video);

            if (_videoStream == null)
                throw new InvalidOperationException();


            Log.Debug(this, "Rtsp: Setup");

            _session = _client.Setup(_videoStream, UdpPort);

            if (_session == null)
                throw new InvalidOperationException();

            if (_videoStream.Format == "H264")
            {
                var extraData = new MemoryStream();

                if (_videoStream.Data != null && _videoStream.Data.Count > 0)
                {
                    var format = new VideoFormat();
                    SpsDecoder.Decode(_videoStream.Data[0], ref format);
                    FrameSize = new Size2I((uint)format.Width, (uint)format.Height);
                    foreach (var item in _videoStream.Data)
                    {
                        extraData.Write(RtpH264Client.NAL_START);
                        extraData.Write(item);
                    }
                }

                _videoCodec = Context.RequireInstance<IVideoCodec>();
                _videoCodec.OutTexture = OutTexture;

                Log.Debug(this, "Rtsp: Open Codec");

                _videoCodec.Open(VideoCodecMode.Decode, "video/avc", new VideoFormat
                {
                    Width = (int)FrameSize.Width,
                    Height = (int)FrameSize.Height,
                    ImageFormat = ImageFormat.Rgb32
                }, extraData.ToArray());


                Log.Debug(this, "Rtsp: Open Client");

                _h264Stream = new RtpH264Client(_session.ClientPort);
                _h264Stream.Open();
            }
            else
                throw new NotSupportedException();

            if (!_client.Play(_streamName, _session!))
                throw new InvalidOperationException();

            _dstBuffer = FrameBuffer.Allocate((int)(FrameSize.Width * FrameSize.Height * 4));
        }

        protected void ReadLoop()
        {
            Log.Debug(this, "Rtsp: Read Thread Started");

            var timeout = _session!.SessionTimeout.TotalSeconds * 0.5;
            if (timeout == 0)
                timeout = 20;

            /*
            if (_videoStream?.Data != null)
            {
                foreach (var data in _videoStream.Data)
                {
                    var newData = new byte[data.Length + 4];
                    newData[3] = 1;
                    Buffer.BlockCopy(data, 0, newData, 4, data.Length);
                    _videoCodec!.EnqueueBuffer(new FrameBuffer() { ByteArray = newData });
                }
            }
            */

            while (_h264Stream != null && _session != null)
            {
                try
                {
                    if ((DateTime.Now - _lastPingTime).TotalSeconds > timeout)
                    {
                        Log.Debug(this, "Rtsp: Ping");
                        _client?.GetParameter(_streamName!, _session);
                        _lastPingTime = DateTime.Now;
                    }

                    var nalData = _h264Stream?.ReadNalUnit();
                    if (nalData != null)
                    {
                        //_out.Write(nalData);    
                        //_out.Flush();   
                        var buffer = new FrameBuffer() { ByteArray = nalData };
                        _videoCodec!.EnqueueBuffer(buffer);
                        //_videoCodec!.DequeueBuffer(ref _dstBuffer);

                    }
                }
                catch (Exception)
                {
                    //Log.Error(this, ex);
                }
            }
        }

        public bool TryDecodeNextFrame(TextureData data)
        {
            if (_readThread == null)
            {
                _readThread = new Thread(ReadLoop);
                _readThread.Name = "XrEngine Rtsp Video Read";
                _readThread!.Start();
            }

            Debug.Assert(_videoCodec != null);

            try
            {
                if (_videoCodec.DequeueBuffer(ref _dstBuffer))
                {
                    if ((_videoCodec.Caps & VideoCodecCaps.DecodeTexture) == 0)
                    {
                        data.Width = FrameSize.Width;
                        data.Height = FrameSize.Height;
                        data.Format = TextureFormat.Rgba32;
                        data.Data = MemoryBuffer.Create(_dstBuffer.ByteArray);
                    }

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
                Log.Warn(this, "{0})", ex.Message);
            }

            return false;
        }

        public void Close()
        {
            if (_client != null)
            {
                if (_session != null && _streamName != null)
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

        public Size2I FrameSize { get; protected set; }

        public Texture2D? OutTexture { get; set; }
    }
}
