using System.Net;
using System.Net.Sockets;
using XrEngine.Video.Abstraction;

namespace XrEngine.Video
{
    public class RtpH264Client
    {
        public static readonly byte[] NAL_START = [0, 0, 0, 1];

        protected UdpClient? _client;
        protected int _clientPort;

        protected IPEndPoint? _endPoint;
        protected bool _ppsRec;
        protected bool _spsRec;
        protected MemoryStream? _readStream;
        protected int _lastSeqNumber;
        protected bool _isFormatReceived;
        protected DateTime _lastReportTime;

        public RtpH264Client(int clientPort)
        {
            _clientPort = clientPort;

            Timeout = TimeSpan.FromSeconds(0);
        }

        public void Open()
        {
            _client = new UdpClient(_clientPort);
            _client.Client.ReceiveTimeout = (int)Timeout.TotalMilliseconds;
            _client.Client.ReceiveBufferSize = 1024 * 1024 * 10;
            _endPoint = new IPEndPoint(IPAddress.Any, _clientPort);
            _ppsRec = false;
            _spsRec = false;
            _readStream = new MemoryStream();
            //_file = new BinaryWriter(new FileStream("d:\\stream.raw", FileMode.Create, FileAccess.Write));
        }

        public void Close()
        {
            _client?.Dispose();
            _client = null;
            _readStream?.Dispose();
            _readStream = null;
        }

        private byte[] PrefixNALUnit(byte[] nalUnit)
        {
            // Prefix with 0x00 0x00 0x01
            byte[] prefixedNALUnit = new byte[nalUnit.Length + 3];
            prefixedNALUnit[0] = 0x00;
            prefixedNALUnit[1] = 0x00;
            prefixedNALUnit[2] = 0x01;
            Array.Copy(nalUnit, 0, prefixedNALUnit, 3, nalUnit.Length);

            return prefixedNALUnit;
        }

        private byte[]? DecodeNALUnit(byte[] rtpPacket)
        {
            // RTP header is typically 12 bytes
            int rtpHeaderSize = 12;

            if (rtpPacket.Length <= rtpHeaderSize)
                return null;

            byte[] payload = new byte[rtpPacket.Length - rtpHeaderSize];
            Array.Copy(rtpPacket, rtpHeaderSize, payload, 0, payload.Length);

            // First byte of payload is the NAL unit header
            byte nalUnitHeader = payload[0];

            // Check if it's a single NAL unit packet
            if ((nalUnitHeader & 0x1F) != 28)  // If it's not an FU-A (Fragmented Unit)
            {
                return PrefixNALUnit(payload);
            }
            else
            {
                // Handle Fragmentation Units (FU-A)
                byte fuHeader = payload[1];
                byte nalType = (byte)(nalUnitHeader & 0xE0 | fuHeader & 0x1F);

                // Start of fragmented NAL unit
                if ((fuHeader & 0x80) != 0)
                {
                    // Start bit is set
                    byte[] nalUnit = new byte[payload.Length - 2 + 1];
                    nalUnit[0] = nalType;
                    Array.Copy(payload, 2, nalUnit, 1, payload.Length - 2);
                    return PrefixNALUnit(nalUnit);
                }
                else
                {
                    // Continuation or End of fragmented NAL unit
                    byte[] nalUnit = new byte[payload.Length - 2];
                    Array.Copy(payload, 2, nalUnit, 0, payload.Length - 2);
                    return PrefixNALUnit(nalUnit);
                }
            }
        }


        public byte[]? ReadNalUnit()
        {
            if (_client == null || _readStream == null)
                throw new InvalidOperationException();

            _readStream.SetLength(0);

            bool isStarted = false;
            byte[] packet;

            while (true)
            {
                packet = _client.Receive(ref _endPoint);
                /*
                _file.Write(packet.Length);
                _file.Write(packet);
                _file.Flush();
                */
                //return DecodeNALUnit(packet);

                int version = GetRTPHeaderValue(packet, 0, 1);
                int padding = GetRTPHeaderValue(packet, 2, 2);
                int extension = GetRTPHeaderValue(packet, 3, 3);
                int csrcCount = GetRTPHeaderValue(packet, 4, 7);
                int marker = GetRTPHeaderValue(packet, 8, 8);
                int payloadType = GetRTPHeaderValue(packet, 9, 15);
                int sequenceNum = GetRTPHeaderValue(packet, 16, 31);
                int timestamp = GetRTPHeaderValue(packet, 32, 63);
                int ssrcId = GetRTPHeaderValue(packet, 64, 95);

                //Log.Debug(this, $"Read:{sequenceNum} {timestamp}{(marker != 0 ? ", marker": "")}");

                if (padding == 1)
                    throw new NotSupportedException();

                bool isValidSeq = _lastSeqNumber == 0 || sequenceNum == _lastSeqNumber + 1;

                if (_lastSeqNumber != 0 && _lastSeqNumber == sequenceNum)
                {
                    Log.Warn(this, "Same seq received");
                    continue;
                }

                _lastSeqNumber = sequenceNum;

                if (!isValidSeq)
                {
                    Log.Warn(this, "-----------Skipped");
                    //return null;
                }

                var nalUnitHeader = packet[12];

                int fragment_type = nalUnitHeader & 0x1F;

                if (fragment_type == 28)
                {
                    var fuHeader = packet[13];

                    int start_bit = fuHeader & 0x80;
                    int end_bit = fuHeader & 0x40;

                    if (start_bit == 0 && end_bit == 0 && !isStarted)
                    {
                        Log.Warn(this, "Fragment without start");
                        continue;
                    }


                    if (start_bit != 0)
                    {
                        if (isStarted)
                        {
                            Log.Warn(this, "Fragment already started");
                            _readStream.SetLength(0);
                            break;
                        }

                        isStarted = true;

                        var nalType = (nalUnitHeader & 0xE0) | (fuHeader & 0x1F);

                        _readStream.Write(NAL_START);
                        _readStream.WriteByte((byte)nalType);
                    }

                    _readStream.Write(packet, 14, packet.Length - 14);

                    if (end_bit != 0)
                    {
                        if (!isStarted)
                        {
                            Log.Warn(this, "Fragment not started");
                            return null;
                        }

                        break;
                    }
                }
                else if (fragment_type >= 1 && fragment_type <= 23)
                {
                    if (fragment_type == 7)
                    {
                        var frameData = new byte[packet.Length - 12];
                        Buffer.BlockCopy(packet, 12, frameData, 0, packet.Length - 12);

                        var format = new VideoFormat();
                        SpsDecoder.Decode(frameData, ref format);
                        Format = format;

                        _spsRec = true;
                    }

                    if (fragment_type == 8)
                    {
                        _ppsRec = true;
                    }
                    _readStream.Write(NAL_START);
                    _readStream.Write(packet, 12, packet.Length - 12);
                    break;
                }
                else
                {
                    throw new NotSupportedException();
                }

                if (marker == 1)
                    break;
            }
            /*
            if ((DateTime.UtcNow - _lastReportTime).TotalSeconds > 5)
            {
                byte[] ctrlPacket = new byte[8];
                // version | padding | RC
                //  0  1        2      3  4  5  6 7 
                ctrlPacket[0] = 0x80;

                //receiver report
                ctrlPacket[1] = 201;

                //len. (8 / 4)) - 1
                ctrlPacket[2] = 0x00; // Length MSB
                ctrlPacket[3] = 0x01; // Length LSB

                ctrlPacket[4] = packet[8];
                ctrlPacket[5] = packet[9];
                ctrlPacket[6] = packet[10];
                ctrlPacket[7] = packet[11];

                _client.Send(ctrlPacket, new IPEndPoint(_endPoint.Address, _serverPort));

                _lastReportTime = DateTime.UtcNow; 
            }
            */

            var result = _readStream.ToArray();

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
