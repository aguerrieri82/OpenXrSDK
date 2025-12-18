using SkiaSharp;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);



namespace XrSamples.Dnd
{

    public class VttScene
    {
        public string? ItemType { get; set; }
        public int[][]? Reveals { get; set; }
        public string? Scale { get; set; }
        public int UVTTFile { get; set; }
        public int Vpps { get; set; }
        public string? Title { get; set; }
        public string? FogOfWar { get; set; }
        public string? PlayerMap { get; set; }
        public string? Upsq { get; set; }
        public string? FolderPath { get; set; }
        public int GridType { get; set; }
        public IList<IList<object>>? Drawings { get; set; }
        public int Fpsq { get; set; }
        public VttToken[]? Tokens { get; set; }
        public Guid Id { get; set; }
        public string? DarknessFilter { get; set; }
        public long Order { get; set; }
        public int Height { get; set; }
        public string? ParentId { get; set; }
        public float ScaleFactor { get; set; }
        public int OffsetX { get; set; }
        public string? DmMapUsable { get; set; }
        public int OffsetY { get; set; }
        public string? DmMapIsVideo { get; set; }
        public int Grid { get; set; }
        public int ScaleCheck { get; set; }
        public string? DmMap { get; set; }
        public string? PlayerMapIsVideo { get; set; }
        public int Width { get; set; }
        public int Hpps { get; set; }
        public int Snap { get; set; }
    }

    public class VttHitPointInfo
    {
        public object? Maximum { get; set; }
        public int Current { get; set; }
        public int Temp { get; set; }
    }

    public class VttRange
    {
        public string? Color { get; set; }
        public string? Feet { get; set; }
    }

    public class VttOffset
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class VttSceneReesponse
    {
        public string? ObjectId { get; set; }
        public string? CampaignId { get; set; }
        public Guid SceneId { get; set; }
        public VttScene? Data { get; set; }
        public long Timestamp { get; set; }
    }

    public class VttToken
    {
        public VttRange? Aura2 { get; set; }
        public VttRange? Aura1 { get; set; }
        public string? Color { get; set; }
        public VttOffset? Offset { get; set; }
        public bool Hidden { get; set; }
        public VttHitPointInfo? HitPointInfo { get; set; }
        public double GridSquares { get; set; }
        public object[]? CustomConditions { get; set; }
        public string? TokenStyleSelect { get; set; }
        public int ScaleCreated { get; set; }
        public VttRange? Vision { get; set; }
        public string? Top { get; set; }
        public int Size { get; set; }
        public object? ArmorClass { get; set; }
        public string? Left { get; set; }
        public string? Name { get; set; }
        public VttRange? Light1 { get; set; }
        public Guid Id { get; set; }
        public bool Auraislight { get; set; }
        public object[]? Conditions { get; set; }
        public string? Imgsrc { get; set; }
        public VttRange? Light2 { get; set; }
        public bool Disablestat { get; set; }
        public int Hp { get; set; }
        public bool Locked { get; set; }
        public bool Revealname { get; set; }
        public string? TokenBaseStyleSelect { get; set; }
        public int ItemId { get; set; }
        public bool Square { get; set; }
        public string? Healthauratype { get; set; }
        public string? PlaceType { get; set; }
        public string? Defaultmaxhptype { get; set; }
        public bool Disableborder { get; set; }
        public bool Enablepercenthpbar { get; set; }
        public int SizeId { get; set; }
        public string? ItemType { get; set; }
        public bool Hidestat { get; set; }
        public bool VideoToken { get; set; }
        public bool Legacyaspectratio { get; set; }
        public string? MaxHp { get; set; }
        public bool Alwaysshowname { get; set; }
        public bool RestrictPlayerMove { get; set; }
        public string? ListItemPath { get; set; }
        public bool Hidehpbar { get; set; }
        public int Stat { get; set; }
        public string? LockRestrictDrop { get; set; }
        public bool UnderDarkness { get; set; }
        public bool PlayerOwned { get; set; }
        public int Monster { get; set; }
        public bool MaxAge { get; set; }
        public int Zindexdiff { get; set; }
        public bool Disableaura { get; set; }
        public bool RevealInFog { get; set; }
    }

    public class VttAction
    {
        public string? Action { get; set; }
        public string? CampaignId { get; set; }
        public string? EventType { get; set; }
        public Guid Sender { get; set; }
        public object? Data { get; set; }
        public int Cloud { get; set; }
        public int Sequence { get; set; }
        public Guid SceneId { get; set; }
        public Guid PlayersSceneId { get; set; }
        public string? RequestTimeEpoch { get; set; }
    }


    public class VttCurrentSceneResponse
    {
        public Guid DmScene { get; set; }

        public Guid PlayerScene { get; set; }
    }

    public interface IAboveVttListener
    {
        void OnTokenUpdate(VttToken token);
    }

    public class AboveVttClient
    {
        static readonly JsonSerializerOptions JSON_OPT = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        readonly ClientWebSocket _socketClient;
        readonly HttpClient _httpClient;
        readonly IAboveVttListener _listener;

        Guid _clientId;
        Thread? _receiveThread;
        string? _campaignId;
        int _sequence;

        public AboveVttClient(IAboveVttListener listener)
        {
            _socketClient = new ClientWebSocket();
            _httpClient = new HttpClient();
            _listener = listener;
        }

        public async Task ConnectAsync(string campaignId)
        {
            _clientId = Guid.NewGuid();
            _campaignId = campaignId;
            _sequence = 1;

            await _socketClient.ConnectAsync(new Uri($"wss://blackjackandhookers.abovevtt.net/v1?campaign={campaignId}&DM=1"), CancellationToken.None);

            _receiveThread = new Thread(ReceiveLoop);
            _receiveThread.Name = "AboveVTT Receive WS";
            _receiveThread.Start();

        }


        public async Task<SKBitmap> DownloadImageAsync(string uri)
        {
            var response = await _httpClient.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return SKBitmap.Decode(stream);
            }
            throw new Exception($"Failed to download image from {uri}");
        }

        public async Task DisconnectAsync()
        {
            await _socketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
        }

        public async Task<VttCurrentSceneResponse> GetCurrentSceneAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<VttCurrentSceneResponse>($"https://services.abovevtt.net/services?action=getCurrentScene&campaign={_campaignId}");
            return result!;
        }

        public async Task<VttSceneReesponse> GetSceneAsync(Guid sceneId)
        {
            var result = await _httpClient.GetFromJsonAsync<VttSceneReesponse>($"https://services.abovevtt.net/services?action=getScene&campaign={_campaignId}&scene={sceneId}");
            return result!;
        }


        public async Task UpdateTokenAsync(Guid sceneId, VttToken token)
        {
            var action = new VttAction();
            action.Sender = _clientId;
            action.SceneId = sceneId;
            action.PlayersSceneId = sceneId;
            action.EventType = "custom/myVTT/token";
            action.Data = token;
            action.Cloud = 1;
            action.Sequence = _sequence++;
            action.CampaignId = _campaignId;
            action.Action = "sendmessage";

            var json = JsonSerializer.Serialize(action, JSON_OPT);
            var buffer = Encoding.UTF8.GetBytes(json);

            await _socketClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }


        protected async void ReceiveLoop()
        {
            var buffer = new Memory<byte>(new byte[1024 * 1024]);

            while (_socketClient.State == WebSocketState.Open)
            {
                var res = await _socketClient.ReceiveAsync(buffer, CancellationToken.None);
                if (res.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer.ToArray(), 0, res.Count);
                    var action = JsonSerializer.Deserialize<VttAction>(json, JSON_OPT)!;
                    if (action.EventType == "custom/myVTT/token")
                    {
                        var token = ((JsonElement)action.Data!).Deserialize<VttToken>(JSON_OPT)!;
                        _listener.OnTokenUpdate(token);
                    }
                }
            }

        }
    }
}
