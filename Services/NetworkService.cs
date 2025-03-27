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
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMiliseconds().ToString(),
            };

            string jsonData = JsonSerializer.Serialize(messageData);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            byte[] messageBytes = MAGIC_BYTES.Concat(data).ToArray();
            await _broadcaster.SendAsync(messasgeBytes, messageBytes.Length, new IPEndPoint(IPAddress.Broadcast, 5555));
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
                }
                catch (System.Exception)
                {

                    throw;
                }
            }
        }



    }
}
