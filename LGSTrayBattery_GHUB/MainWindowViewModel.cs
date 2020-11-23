using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LGSTrayBattery_GHUB.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Websocket.Client;
using Websocket.Client.Exceptions;

namespace LGSTrayBattery_GHUB
{
    class MainWindowViewModel : INotifyPropertyChanged, INotifyCollectionChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private WebsocketClient _ws = null;

        public Device SelectedDevice { get; set; } = null;

        private Dictionary<string, Device> _devIdNameMap;
        public List<Device> DeviceList { get; private set; }

        private Thread _httpServer;

        public MainWindowViewModel()
        {
            ConnectToGHUB_async();

            _httpServer = new Thread(() => HttpServer.ServerLoop(this));
            _httpServer.Start();
        }

        public async void ConnectToGHUB_async()
        {
            var url = new Uri("ws://localhost:9010");

            var factory = new Func<ClientWebSocket>(() =>
            {
                var client = new ClientWebSocket();
                client.Options.UseDefaultCredentials = false;
                client.Options.SetRequestHeader("Origin", "file://");
                client.Options.SetRequestHeader("Pragma", "no-cache");
                client.Options.SetRequestHeader("Cache-Control", "no-cache");
                client.Options.SetRequestHeader("Sec-WebSocket-Extensions", "permessage-deflate; client_max_window_bits");
                client.Options.SetRequestHeader("Sec-WebSocket-Protocol", "json");
                return client;
            });

            _ws?.Dispose();
            _ws = new WebsocketClient(url, factory);
            _ws.MessageReceived.Subscribe(ParseSocketMsg);
            _ws.ErrorReconnectTimeout = TimeSpan.FromMilliseconds(500);
            _ws.ReconnectTimeout = null;

            Console.WriteLine($"Trying to connect to LGHUB_agent, at {url}");

            while (!_ws.IsRunning)
            {
                await _ws.Start();
            }

            Console.WriteLine($"Connected to LGHUB_agent");

            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/devices/arrival"
            }));

            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/devices/removal"
            }));

            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/devices/state_changed"
            }));

            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "SUBSCRIBE",
                path = "/battery_state_changed"
            }));

            ScanDevices();
        }

        public void UpdateSelectedDevice(Device selectedDevice)
        {
            foreach (var device in DeviceList)
            {
                device.IsChecked = false;
            }

            SelectedDevice = selectedDevice;
            SelectedDevice.IsChecked = true;

            Settings.Default.LastDeviceId = SelectedDevice.DeviceId;
            Settings.Default.Save();
        }

        public void ScanDevices()
        {
            _ws.Send(JsonConvert.SerializeObject(new
            {
                msgId = "",
                verb = "GET",
                path = "/devices"
            }));
        }

        private void ParseSocketMsg(ResponseMessage msg)
        {
            GHUBMsg ghubmsg = JsonConvert.DeserializeObject<GHUBMsg>(msg.Text);

            if (ghubmsg.path == "/devices/arrival" || ghubmsg.path == "/devices/removal" || ghubmsg.path == "/devices/state_changed")
            {
                _ws.Send(JsonConvert.SerializeObject(new { msgId = "", verb = "GET", path = "/devices" }));
            }
            else if (ghubmsg.path == "/devices")
            {
                LoadDevices(ghubmsg.payload);
            }
            else if (ghubmsg.path.StartsWith("/battery_state/"))
            {
                if (ghubmsg.result["code"]?.ToString() == "SUCCESS")
                {
                    UpdateBattery(ghubmsg.payload);
                }
            }
            else if (ghubmsg.path == "/battery_state_changed")
            {
                UpdateBattery(ghubmsg.payload);
            }
            else
            {
            }
            Debug.WriteLine(msg);
        }

        private void LoadDevices(JObject payload)
        {
            _devIdNameMap = new Dictionary<string, Device>();
            List<Device> temp = new List<Device>();

            foreach (var deviceToken in payload["devices"])
            {
                Device device = new Device()
                {
                    DeviceId = deviceToken["id"].ToString(),
                    DisplayName = deviceToken["extendedDisplayName"].ToString()
                };

                _devIdNameMap.Add(deviceToken["id"].ToString(), device);
                temp.Add(device);
            }

            DeviceList = temp;

            RefreshBattery();

            SelectedDevice = DeviceList.SingleOrDefault(x => x.DeviceId == Settings.Default.LastDeviceId) ?? DeviceList.First();

            SelectedDevice.IsChecked = true;
        }

        private void RefreshBattery()
        {
            foreach (var device in DeviceList)
            {
                _ws.Send(JsonConvert.SerializeObject(new
                {
                    msgId = "",
                    verb = "GET",
                    path = $"/battery_state/{device.DeviceId}"
                }));
            }
        }

        private void UpdateBattery(JObject payload)
        {
            var device = _devIdNameMap[payload["deviceId"].ToString()];

            device.Percentage = payload["percentage"].ToObject<double>();
            device.Mileage = payload["mileage"].ToObject<double>();
            device.Charging = payload["charging"].ToObject<bool>();
        }
    }
}
