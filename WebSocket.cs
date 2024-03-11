using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace AlgolabAPI
{
    public class WebSocketApi
    {
        private const int _pingPeriod = 30000;  // in ms

        private WebSocket _client;
        private Action OnWebSocketConnect;
        private string _key;
        private string _hash;
        private string _checker;

        private StreamWriter sw;
        private StreamWriter swData;

        private Timer _pingTimer;
        private TimerCallback _pingCallback;

        public bool IsOpen
        {
            get { return _client != null && _client.State == WebSocketState.Open; }
        }

        public string DT
        {
            get { return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"); }
        }

        public WebSocketApi(string key, string hash, string checker)
        {
            _key = key;
            _hash = hash;
            _checker = checker;

            sw = new StreamWriter("./socket.log", append: true);
            swData = new StreamWriter("./socketData.log", append: true);

            sw.AutoFlush = true;
            swData.AutoFlush = true;
        }

        public async Task Connect(string url)
        {
            _client = await CreateWebSocket(url);
            OnWebSocketConnect?.Invoke();
        }

        protected async Task<WebSocket> CreateWebSocket(string url)
        {
            var headers = new Dictionary<string, string>
            {
                {"APIKEY", _key},
                {"Authorization", _hash},
                {"Checker", _checker}
            };

            var webSocket = new WebSocket(url, customHeaderItems:headers.ToList());

            webSocket.Security.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls | SslProtocols.Tls12;
            webSocket.Opened += OnWebsocketOpen;
            webSocket.Error += OnWebSocketError;
            webSocket.Closed += OnWebsocketClosed;
            webSocket.MessageReceived += OnWebsocketMessageReceive;
            webSocket.DataReceived += OnWebsocketDataRecieved;

            await OpenConnection(webSocket);

            // heartbeat timer
            _pingCallback = new TimerCallback(OnTimerPing);
            _pingTimer = new Timer(_pingCallback, null, _pingPeriod, _pingPeriod);

            return webSocket;
        }

        protected async Task OpenConnection(WebSocket webSocket)
        {
            webSocket.Open();

            while (webSocket.State != WebSocketState.Open)
            {
                await Task.Delay(25);
            }
        }

        public async Task Stop()
        {
            if (_client != null)
            {
                _client.Close();

                while (_client.State != WebSocketState.Closed)
                {
                    await Task.Delay(25);
                }

                _client.Opened -= OnWebsocketOpen;
                _client.Error -= OnWebSocketError;
                _client.Closed -= OnWebsocketClosed;
                _client.MessageReceived -= OnWebsocketMessageReceive;
                _client.DataReceived -= OnWebsocketDataRecieved;
                _client?.Dispose();
            }
        }

        private void OnWebsocketOpen(object sender, EventArgs e)
        {
            sw.WriteLine($"{DT} OnWebsocketOpen");
        }

        private void OnWebSocketError(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            sw.WriteLine($"{DT} OnWebSocketError: {e.Exception}");
        }

        private void OnWebsocketClosed(object o, EventArgs e)
        {
            if (_pingTimer != null)
                _pingTimer.Dispose();

            sw.WriteLine($"{DT} OnWebsocketClosed");
        }

        private void OnWebsocketMessageReceive(object o, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            try
            {
                var message = JsonConvert.DeserializeObject<WebsocketData>(messageReceivedEventArgs.Message);

                switch (message.Type)
                {
                    case "T":
                        swData.WriteLine($"{DT} Tick:  {messageReceivedEventArgs.Message}");
                        break;
                    case "D":
                        swData.WriteLine($"{DT} Depth: {messageReceivedEventArgs.Message}");
                        break;
                    case "O":
                    default:
                        sw.WriteLine($"{DT} {messageReceivedEventArgs.Message}");
                        break;
                }
            }
            catch (Exception e)
            {
                sw.WriteLine($"{DT} OnMsgReceive error: {e.Message}");
            }
        }

        private void OnWebsocketDataRecieved(object sender, DataReceivedEventArgs e)
        {
            swData.WriteLine($"{DT} {e.Data}");
        }


        // send H-message
        private void OnTimerPing(object obj)
        {
            SendCommand("{\"Type\":\"H\",\"Token\":\"" + _hash + "\"}");
        }

        // send D-message
        public void SubscribeDepth(string symbol)
        {
            SendCommand("{\"Type\":\"D\",\"Token\":\"" + _hash + "\",\"Symbols\":[\"" + symbol + "\"]}");
        }

        // send T-message
        public void SubscribeTrade(string symbol)
        {
            SendCommand("{\"Type\":\"T\",\"Token\":\"" + _hash + "\",\"Symbols\":[\"" + symbol + "\"]}");
        }

        private void SendCommand(string command)
        {
            try
            {
                sw.WriteLine($"{DT} Send: {command}");
                _client?.Send(command);
            }
            catch (Exception e)
            {
                sw.WriteLine($"{DT} OnTimerPing error: {e.Message}");
            }
        }
    }
}
