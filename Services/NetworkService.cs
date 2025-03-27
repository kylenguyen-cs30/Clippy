using Clippy.Console.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace Clippy.Console.Services
{
    public class NetworkService
    {
        private readonly UdpClient _broadcaster = new UdpClient();
        private readonly UdpClient _receiver = new UdpClient(new IPEndPoint(IPAddress.Any, 5555));
        private readonly string APP_IDENTIFIER = "ClippySynch_v1.0";
        private readonly byte[] MAGIC_BYTES = Encoding.UTF8.GetBytes("CLIPPY");
        private CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<ClipboardItem> ClipboardDataReceived;
        public event EventHandler<Device> DeviceDiscovered;


        public void Start()
        {
            _broadcaster.EnableBroadcast = true;
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => ListenForClipboardUpdateAsync(_cancellationTokenSource.Token));
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async Task BroadcastClipboardData(string content)
        {
            var messageData = new Dictionary<string, string>
            {
                ["app"] = APP_IDENTIFIER,
                ["content"] = content,
                ["deviceName"] = Environment.MachineName,
                ["deviceType"] = GetDeviceType(),
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            };

            string jsonData = JsonSerializer.Serialize(messageData);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            byte[] messageBytes = MAGIC_BYTES.Concat(data).ToArray();
            await _broadcaster.SendAsync(messageBytes, messageBytes.Length, new IPEndPoint(IPAddress.Broadcast, 5555));
        }

        public string GetDeviceType()
        {
            if (OperatingSystem.IsWindows())
                return "Windows";
            else if (OperatingSystem.IsMacOS())
                return "MacOS";
            else if (OperatingSystem.IsLinux())
                return "Linux";
            else
                return "Unknown";
        }


        private async Task ListenForClipboardUpdateAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await _receiver.ReceiveAsync();
                    if (result.Buffer.Length <= MAGIC_BYTES.Length || !result.Buffer.Take(MAGIC_BYTES.Length).SequenceEqual(MAGIC_BYTES))
                    {
                        continue;
                    }

                    byte[] jsonBytes = result.Buffer.Skip(MAGIC_BYTES.Length).ToArray();
                    string jsonData = Encoding.UTF8.GetString(jsonBytes);
                    var messageData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);

                    if (!messageData.ContainsKey("app") || messageData["app"] != APP_IDENTIFIER)
                    {
                        continue;
                    }

                    string senderIp = result.RemoteEndPoint.Address.ToString();
                    string deviceName = messageData["deviceName"];
                    string deviceType = messageData["deviceType"];
                    string content = messageData["content"];

                    // notify about new device 
                    var device = new Device(deviceName, senderIp, deviceType);
                    DeviceDiscovered?.Invoke(this, device);

                    // Notify about new clipboard content 
                    var clipboardItem = new ClipboardItem(content, deviceName, senderIp, deviceType);
                    ClipboardDataReceived?.Invoke(this, clipboardItem);


                }
                catch (Exception ex)
                {

                    System.Console.WriteLine($"Error receieving clipboard adata: {ex.Message}");
                }
            }
        }



    }
}
