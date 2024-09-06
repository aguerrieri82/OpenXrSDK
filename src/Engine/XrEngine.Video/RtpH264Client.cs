using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using XrEngine.Video.Abstraction;
using static XrEngine.Ktx2Reader;

namespace XrEngine.Video
{
    public class RtpH264Client
    {
        protected UdpClient? _client;
        protected int _port;
        protected IPEndPoint? _endPoint;
        private bool _ppsRec;
        private bool _spsRec;
        protected int _lastSeqNumber;
        protected bool _isFormatReceived;

        public RtpH264Client(int port)
        {
            _port = port;
            Timeout = TimeSpan.FromSeconds(0);
        }

        public void Open()
        {
            _client = new UdpClient(_port);
            _client.Client.ReceiveTimeout = (int)Timeout.TotalMilliseconds;
            _endPoint = new IPEndPoint(IPAddress.Any, _port);
            _ppsRec = false;
            _spsRec = false;
        }

        public void Close()
        {
            _client?.Dispose();
            _client = null;
        }

        public byte[]? ReadNalUnit()
        {
            if (_client == null)
                throw new InvalidOperationException();

            var stream = new MemoryStream();
            bool isStarted = false;
            while (true)
            {
                var packet = _client.Receive(ref _endPoint);

                int version = GetRTPHeaderValue(packet, 0, 1);
                int padding = GetRTPHeaderValue(packet, 2, 2);
                int extension = GetRTPHeaderValue(packet, 3, 3);
                int csrcCount = GetRTPHeaderValue(packet, 4, 7);
                int marker = GetRTPHeaderValue(packet, 8, 8);
                int payloadType = GetRTPHeaderValue(packet, 9, 15);
                int sequenceNum = GetRTPHeaderValue(packet, 16, 31);
                int timestamp = GetRTPHeaderValue(packet, 32, 63);
                int ssrcId = GetRTPHeaderValue(packet, 64, 95);

                if (padding == 1)
                    throw new NotSupportedException();

                bool isValidSeq = _lastSeqNumber == 0 || sequenceNum == _lastSeqNumber + 1;
                _lastSeqNumber = sequenceNum;

                if (!isValidSeq)
                {
                    Debug.WriteLine("-----------Skipped");
                    return null;
                }
          
                int fragment_type = packet[12] & 0x1F;

                if (fragment_type == 28)
                {
                    int start_bit = packet[13] & 0x80;
                    int end_bit = packet[13] & 0x40;

                    if (start_bit == 0 && end_bit == 0 && !isStarted)
                        return null;

                    if (start_bit != 0)
                    {
                        if (isStarted)
                            return null;
                        isStarted = true;
                        var nalHeader = (packet[12] & 0xE0) | (packet[13] & 0x1F);
                        stream.WriteByte(0);
                        stream.WriteByte(0);
                        stream.WriteByte(1);
                        stream.WriteByte((byte)nalHeader);
                    }
                    
                    stream.Write(packet, 14, packet.Length - 14);

                    if (end_bit != 0)
                    {
                        if (!isStarted)
                            return null;
                        break;
                    }
                }
                else if (fragment_type >= 1 && fragment_type <= 23)
                {
                  
                    if (fragment_type == 7)
                    {
                        /*
                        if (_spsRec)
                            return null;*/
                        var frameData = new Byte[packet.Length - 12];
                        Buffer.BlockCopy(packet, 12, frameData, 0, packet.Length - 12);
                        SpsDecoder decoder = new SpsDecoder();
                        VideoFormat format = new VideoFormat();
                        decoder.Decode(frameData, ref format);
                        Format = format;
                        _spsRec = true;
                    }

                    if (fragment_type == 8)
                    {   /*
                        if (_ppsRec)
                            return null;*/
                        _ppsRec = true;
                    }

                    stream.WriteByte(0);
                    stream.WriteByte(0);
                    stream.WriteByte(1);
                    stream.Write(packet, 12, packet.Length - 12);
                    break;
                }
                else if (fragment_type == 24)
                {
                    throw new NotSupportedException();
                }

                if (marker == 1)
                    break;
            }
            var result = stream.ToArray();

            return result;

        }

        private static int GetRTPHeaderValue(byte[] packet, int startBit, int endBit)
        {
            int result = 0;

            int length = endBit - startBit + 1;

            for (int i = startBit; i <= endBit; i++)
            {
                int byteIndex = i / 8;
                int bitShift = 7 - (i % 8);
                result += ((packet[byteIndex] >> bitShift) & 1) * (int)Math.Pow(2, length - i + startBit - 1);
            }
            return result;
        }

        public TimeSpan Timeout { get; set; }

        public VideoFormat Format { get; set; }
    }
}
