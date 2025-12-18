using System.Diagnostics;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace VirtualCamera.IPCamera
{
    public class RtspResponse
    {
        public int Code { get; set; }

        public string? Message { get; set; }

        public Dictionary<string, string>? Header { get; set; }

        public string? Content { get; set; }
    }

    public enum RtspStreamType
    {
        Audio,
        Video
    }

    public class RtspStream
    {
        public string? Control { get; set; }

        public string? Protocol { get; set; }

        public RtspStreamType Type { get; set; }

        public int TrackId { get; set; }

        public int BitRate { get; set; }

        public string? Format { get; set; }

        public IList<byte[]>? Data { get; set; }
    }

    public class RtspSession
    {
        public string? Protocol { get; set; }

        public int ClientPort { get; set; }

        public int ServerPort { get; set; }

        public string? SessionId { get; set; }

        public TimeSpan SessionTimeout { get; set; }
    }


    public class RtspClient
    {
        TcpClient? _client;
        StreamReader? _reader;
        StreamWriter? _writer;
        int _seqNum;
        private string? _authHash1;
        private Dictionary<string, string>? _authParams;

        public RtspClient()
        {
            Timeout = TimeSpan.FromSeconds(0);
        }

        public void Connect(string address, int port = 554)
        {
            _client = new TcpClient();
            _client.Connect(address, port);
            _client.ReceiveTimeout = (int)Timeout.TotalMilliseconds;
            _client.SendTimeout = (int)Timeout.TotalMilliseconds;
            _reader = new StreamReader(_client.GetStream());
            _writer = new StreamWriter(_client.GetStream());
            _seqNum = 0;
        }

        public void Close()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }

        public RtspSession? Setup(RtspStream stream, int port)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["Transport"] = $"{stream.Protocol};unicast;client_port={port}-{port + 1}";
            Send("SETUP", stream.Control!, header);
            RtspResponse? response = ReadResponse();
            if (response?.Header != null && response.Code == 200)
            {
                RtspSession result = new RtspSession();
                string transport = response.Header["Transport"];
                string[] parts = transport.Split(';');
                result.Protocol = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string[] attrParts = parts[i].Split('=');
                    switch (attrParts[0])
                    {
                        case "client_port":
                            result.ClientPort = int.Parse(attrParts[1].Split('-')[0]);
                            break;
                        case "server_port":
                            result.ServerPort = int.Parse(attrParts[1].Split('-')[0]);
                            break;
                    }
                }
                string session = response.Header["Session"];
                parts = session.Split(';');
                result.SessionId = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string[] attrParts = parts[i].Split('=');
                    switch (attrParts[0])
                    {
                        case "timeout":
                            result.SessionTimeout = TimeSpan.FromSeconds(int.Parse(attrParts[1]));
                            break;
                    }
                }
                return result;
            }
            return null;
        }

        public Dictionary<string, string>? GetParameter(string streamName, RtspSession session, params string[] parameters)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["Session"] = session.SessionId!;
            header["Content-Type"] = "text/parameters";

            Send("GET_PARAMETER", streamName, header, string.Join("\r\n", parameters));

            RtspResponse? response = ReadResponse();

            if (response?.Content != null && response.Code == 200 && parameters.Length > 0)
            {
                string[] lines = response.Content.Split(':');
                Dictionary<string, string> result = new Dictionary<string, string>();
                foreach (string line in lines)
                {
                    int index = line.IndexOf(':');
                    result[line.Substring(0, index)] = line.Substring(index + 1);
                }
                return result;
            }

            return null;
        }

        public bool Play(string streamName, RtspSession session)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["Session"] = session.SessionId!;
            header["Range"] = "npt=0.000-";
            Send("PLAY", streamName, header);
            RtspResponse? response = ReadResponse();
            if (response == null)
                return false;
            return response.Code == 200;
        }

        public bool TearDown(string streamName, RtspSession session)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["Session"] = session.SessionId!;
            Send("TEARDOWN", streamName, header);
            RtspResponse? response = ReadResponse();
            return response != null && response.Code == 200;
        }

        public void Authenticate(string streamName, string username, string password)
        {
            Send("DESCRIBE", streamName);
            RtspResponse? response = ReadResponse();
            if (response?.Header != null && response.Code == 401)
            {
                string www = response.Header["WWW-Authenticate"];
                Dictionary<string, string> dic = ParseParams(www);

                _authParams = new Dictionary<string, string>();
                _authParams["realm"] = dic["realm"];
                _authParams["nonce"] = dic["nonce"];
                _authParams["username"] = username;
                _authParams["uri"] = streamName;

                _authHash1 = HashMD5($"{username}:{dic["realm"]}:{password}");


            }
        }

        static string HashMD5(string input)
        {
            return Convert.ToHexStringLower(MD5.HashData(Encoding.ASCII.GetBytes(input)));
        }





        public IList<RtspStream> Describe(string streamName)
        {
            Dictionary<string, string> header = new Dictionary<string, string>();
            header["Accept"] = "application/sdp";
            header["User-Agent"] = "XrEngine";
            Send("DESCRIBE", streamName, header);

            RtspResponse? response = ReadResponse();

            List<RtspStream> result = new List<RtspStream>();

            if (response?.Content != null && response.Code == 200)
            {
                string[] contentLines = response.Content.Split("\r\n");

                RtspStream? curStream = null;
                foreach (string line in contentLines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    char attr = line[0];
                    string data = line.Substring(2);
                    if (attr == 'a')
                    {
                        int attrIndex = data.IndexOf(':');
                        string attrName = data.Substring(0, attrIndex);
                        string attrValue = data.Substring(attrIndex + 1);

                        if (attrName == "control")
                        {
                            if (curStream != null)
                            {
                                if (attrValue.StartsWith("rtsp://"))
                                    curStream.Control = attrValue;
                                else
                                    curStream.Control = $"{streamName}/{attrValue}";
                            }
                        }
                        else if (attrName == "rtpmap")
                        {
                            Debug.Assert(curStream != null);

                            int idIndex = attrValue.IndexOf(' ');
                            int id = int.Parse(attrValue.Substring(0, idIndex));
                            if (curStream.TrackId == id)
                            {
                                string[] formatParts = attrValue.Substring(idIndex + 1).Split('/');
                                curStream.Format = formatParts[0];
                                if (formatParts.Length > 1)
                                    curStream.BitRate = int.Parse(formatParts[1]);
                            }
                        }
                        else if (attrName == "fmtp")
                        {
                            Debug.Assert(curStream != null);

                            int idIndex = attrValue.IndexOf(' ');
                            int id = int.Parse(attrValue.Substring(0, idIndex));
                            if (curStream.TrackId == id)
                            {
                                string[] fAttrList = attrValue.Substring(idIndex + 1).Split(';');
                                foreach (string fAttr in fAttrList)
                                {
                                    int fIndex = fAttr.IndexOf('=');
                                    string fName = fAttr.Substring(0, fIndex);
                                    string fValue = fAttr.Substring(fIndex + 1);
                                    switch (fName)
                                    {
                                        case "packetization-mode":
                                            break;
                                        case "profile-level-id":
                                            break;
                                        case "sprop-parameter-sets":
                                            string[] pSet = fValue.Split(',');
                                            foreach (string setData in pSet)
                                            {
                                                byte[] buffer = Convert.FromBase64String(setData);
                                                if (curStream.Data == null)
                                                    curStream.Data = new List<byte[]>();
                                                curStream.Data.Add(buffer);
                                            }
                                            break;
                                    }

                                }
                            }
                        }
                    }
                    else if (attr == 'm')
                    {
                        curStream = new RtspStream();
                        result.Add(curStream);

                        string[] mediaParts = data.Split(' ');
                        if (mediaParts[0] == "video")
                            curStream.Type = RtspStreamType.Video;
                        else if (mediaParts[0] == "audio")
                            curStream.Type = RtspStreamType.Audio;

                        curStream.TrackId = int.Parse(mediaParts[3]);
                        curStream.Protocol = mediaParts[2];
                    }
                }
            }
            return result;
        }

        protected RtspResponse? ReadResponse()
        {
            Debug.Assert(_reader != null);

            string? code = _reader.ReadLine();
            if (code == null)
            {
                Close();
                return null;
            }

            int index = code.IndexOf(' ');
            string part1 = code.Substring(0, index);
            if (part1 != "RTSP/1.0")
                return null;
            index = code.IndexOf(' ', index + 1);

            RtspResponse result = new RtspResponse
            {
                Code = int.Parse(code.Substring(9, index - 9)),
                Message = code.Substring(index + 1),
                Header = []
            };

            while (true)
            {
                string? line = _reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    break;
                string[] parts = line.Split(':');
                result.Header[parts[0]] = parts[1].Trim();
            }

            if (result.Header.TryGetValue("Content-Length", out string? contentLen))
            {
                int intLen = int.Parse(contentLen);
                char[] chars = new char[intLen];
                _reader.ReadBlock(chars, 0, chars.Length);
                result.Content = new string(chars);
            }

            return result;
        }

        protected string CreateAuthDigest(string verb)
        {
            string hash2 = HashMD5($"{verb}:{_authParams!["uri"]}");
            string response = HashMD5($"{_authHash1}:{_authParams["nonce"]}:{hash2}");

            return $"username=\"{_authParams["username"]}\", realm=\"{_authParams["realm"]}\", nonce=\"{_authParams["nonce"]}\", uri=\"{_authParams["uri"]}\", response=\"{response}\"";
        }

        protected void Send(string verb, string path, Dictionary<string, string>? header = null, string? body = null)
        {
            Debug.Assert(_writer != null);

            _writer.Write($"{verb} {path} RTSP/1.0\r\n");
            _seqNum++;
            _writer.Write($"CSeq: {_seqNum}\r\n");
            if (header != null)
            {
                foreach (KeyValuePair<string, string> item in header)
                    _writer.Write($"{item.Key}: {item.Value}\r\n");
            }

            if (_authParams != null)
                _writer.Write($"Authorization: Digest {CreateAuthDigest(verb)}\r\n");

            if (!string.IsNullOrEmpty(body))
                _writer.Write($"Content-Length: {body.Length}\r\n");

            _writer.Write("\r\n");

            if (!string.IsNullOrEmpty(body))
                _writer.Write(body);

            _writer.Flush();
        }

        static string FormatParams(Dictionary<string, string> input)
        {
            StringBuilder result = new StringBuilder();
            foreach (KeyValuePair<string, string> item in input)
            {
                if (result.Length > 0)
                    result.Append(", ");
                result.Append(item.Key)
                      .Append("=\"")
                      .Append(item.Value)
                      .Append('"');
            }
            return result.ToString();
        }

        static Dictionary<string, string> ParseParams(string input)
        {
            // Regular expression to capture key-value pairs
            Regex regex = new Regex(@"(\w+)=""([^""]*)""");
            MatchCollection matches = regex.Matches(input);

            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            foreach (Match match in matches)
            {
                dictionary[match.Groups[1].Value] = match.Groups[2].Value;
            }

            return dictionary;
        }


        public TimeSpan Timeout { get; set; }

    }
}
