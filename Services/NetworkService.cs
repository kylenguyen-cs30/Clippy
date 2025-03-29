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

            // Limit the content size to prevent UDP packet size issues 
            const int maxDirectSize = 60000;

            // check if the content is small enough for direct transmission 
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            if (contentBytes.Length <= maxDirectSize)
            {
                // small contetn - use direcet transmission
                await SendDirectMessage(content);
            }
            else
            {
                // large content - use chunking 
                await SendChunkedMessage(content);
            }


        }


        private async Task SendDirectMessage(string content)
        {
            var messageData = new Dictionary<string, string>
            {
                ["app"] = APP_IDENTIFIER,
                ["content"] = content,
                ["deviceName"] = Environment.MachineName,
                ["deviceType"] = GetDeviceType(),
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                ["isChunked"] = "false"
            };

            string jsonData = JsonSerializer.Serialize(messageData);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);
            byte[] messageBytes = MAGIC_BYTES.Concat(data).ToArray();
            await _broadcaster.SendAsync(messageBytes, messageBytes.Length, new IPEndPoint(IPAddress.Broadcast, 5555));

        }

        private async Task SendChunkedMessage(string content)
        {
            // generate a unique ID for this multi-part message
            string messageId = Guid.NewGuid().ToString();
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);

            // Determine chunk size - leave room for headers 
            const int chunkSize = 50000;

            // calculate number of chunks 
            int totalChunks = (int)Math.Ceiling(contentBytes.Length / (double)chunkSize);

            // send each chunk 
            for (int i = 0; i < totalChunks; i++)
            {
                // calculate the size of this chunk 
                int currentChunkSize = Math.Min(chunkSize, contentBytes.Length - i * chunkSize);

                // extract the chunk data 
                byte[] chunkData = new byte[currentChunkSize];
                Array.Copy(contentBytes, i * chunkSize, chunkData, 0, currentChunkSize);

                // convert chunk to base64 for safe JSON transmission 
                string chunkBase64 = Convert.ToBase64String(chunkData);

                var messageData = new Dictionary<string, string>
                {
                    ["app"] = APP_IDENTIFIER,
                    ["content"] = content,
                    ["deviceName"] = Environment.MachineName,
                    ["deviceType"] = GetDeviceType(),
                    ["isChunked"] = "true",
                    ["messageId"] = messageId,
                    ["chunkData"] = chunkBase64,
                    ["deviceName"] = Environment.MachineName,
                    ["deviceType"] = GetDeviceType(),
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                };

                string jsonData = JsonSerializer.Serialize(messageData);
                byte[] data = Encoding.UTF8.GetBytes(jsonData);
                byte[] messageBytes = MAGIC_BYTES.Concat(data).ToArray();

                await _broadcaster.SendAsync(messageBytes, messageBytes.Length, new IPEndPoint(IPAddress.Broadcast, 5555));


                // add a small delay between chunks to avoid network congestion
                await Task.Delay(50);



            }

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

        // add a dictionary to store chunks of message being assembled 
        private readonly Dictionary<string, Dictionary<int, string>> _messageChunks = new();

        private void ProcessChunkedMesage(Dictionary<string, string> messageData, string senderIp, string deviceName, string deviceType)
        {
            string messageId = messageData["messageId"];
            int chunkIndex = int.Parse(messageData["chunkIndex"]);
            int totalChunks = int.Parse(messageData["totalChunks"]);
            string chunkData = messageData("chunkData");

            // ensure we have a dictionary for this message 
            if (!_messageChunks.ContainsKey(messageId))
            {
                _messageChunks[messageId] = new Dictionary<int, string>();
            }


            // store this chunk 
            _messageChunks[messageId][chunkIndex] = chunkData;


        }


        private async Task ListenForClipboardUpdateAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await _receiver.ReceiveAsync();

                    //  checker if the data is successfully receieving or not 
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


                    // checker whether if it is chunked message or not
                    bool isChunked = messageData.ContainsKey("isChunked") && messageData["messageData"] == "true";

                    if (isChunked)
                    {
                        // handle chunked message
                        ProcessChunkedMesage(messageData, senderIp, deviceName, deviceType);

                    }
                    else
                    {
                        // handle message
                        string content = messageData["content"];

                        // notify about new device
                        var device = new Device(deviceName, senderIp, deviceType);
                        DeviceDiscovered?.Invoke(this, device);

                        // notify about new clipboard content
                        var clipboardItem = new ClipboardItem(content, deviceName, senderIp, deviceType);
                        ClipboardDataReceived?.Invoke(this, clipboardItem);

                    }









                    // string content = messageData["content"];
                    //
                    // // notify about new device 
                    // var device = new Device(deviceName, senderIp, deviceType);
                    // DeviceDiscovered?.Invoke(this, device);
                    //
                    // // Notify about new clipboard content 
                    // var clipboardItem = new ClipboardItem(content, deviceName, senderIp, deviceType);
                    // ClipboardDataReceived?.Invoke(this, clipboardItem);


                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error receieving clipboard adata: {ex.Message}");
                }
            }
        }



    }
}
