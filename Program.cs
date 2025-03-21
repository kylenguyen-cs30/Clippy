// Name : Hoang Nguyen
// Project: Clippy
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;

namespace Clippy.Console
{
    class Program
    {

        private static string _lastClipboardContent = "";
        private static readonly UdpClient _boardcaster = new UdpClient();
        private static readonly UdpClient _receiver = new UdpClient(new IPEndPoint(IPAddress.Any, 5555));

        // for BroadcastClipboardData
        private static readonly string APP_IDENTIFIER = "ClippySync_v1.0";
        private static readonly byte[] MAGIC_BYTES = Encoding.UTF8.GetBytes("CLIPPY");


        // main method
        static async Task Main(string[] args)
        {
            System.Console.WriteLine("Clippy Started - press Ctrl+C to exit");

            // set up _receiver endpoint 
            _boardcaster.EnableBroadcast = true;

            // start the clipboard monitoring thread 
            var clipboardTask = MonitorClipboardAsync(CancellationToken.None);

            // Start the network listening thread
            var networkTask = ListenForClipboardUpdateAsync(CancellationToken.None);

            // wait for both tasks (they'll run until Cancellation)
            await Task.WhenAll(clipboardTask, networkTask);
        }


        private static async Task BroadcastClipboardData(string content)
        {
            // create a simple message format 
            var messageData = new Dictionary<string, string>
            {
                ["app"] = APP_IDENTIFIER,
                ["content"] = content,
                ["deviceName"] = Environment.MachineName,
                ["deviceType"] = GetDeviceType(),
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()

            };


            // serialize to JSON 
            string jsonData = System.Text.Json.JsonSerializer.Serialize(messageData);
            byte[] data = Encoding.UTF8.GetBytes(jsonData);

            // prepend magic bytes  for addtional verification 
            byte[] messageBytes = MAGIC_BYTES.Concat(data).ToArray();

            await _boardcaster.SendAsync(messageBytes, messageBytes.Length, new IPEndPoint(IPAddress.Broadcast, 5555));


        }

        private static string GetDeviceType()
        {
            if (OperatingSystem.IsWindows())
            {
                return "Windows";
            }
            else if (OperatingSystem.IsMacOS())
            {
                return "MacOS";
            }
            else if (OperatingSystem.IsLinux())
            {
                return "Linux";
            }
            else
            {
                return "Unknown";
            }
        }



        private static async Task MonitorClipboardAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // get current clipboard text 
                    string currentText = await ClipboardService.GetTextAsync();


                    // check if it changed 
                    if (currentText != null && currentText != _lastClipboardContent)
                    {
                        _lastClipboardContent = currentText;
                        System.Console.WriteLine($"Clipboard changed : {currentText}");

                        // broadcase to network 
                        byte[] data = Encoding.UTF8.GetBytes(currentText);
                        await _boardcaster.SendAsync(data, data.Length,
                            new IPEndPoint(IPAddress.Broadcast, 5555));
                    }

                    // wait a bit before checking again 
                    await Task.Delay(500, token);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error monitoring clipboard: {ex.Message}");

                }
            }
        }

        // Listen For 
        private static async Task ListenForClipboardUpdateAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // wait for Broadcast
                    UdpReceiveResult result = await _receiver.ReceiveAsync();


                    // verify magic byte 
                    // j
                    if (result.Buffer.Length <= MAGIC_BYTES.Length || !result.Buffer.Take(MAGIC_BYTES.Length).SequenceEqual(MAGIC_BYTES))
                    {
                        // not our application's message, ignore 
                        continue;
                    }

                    // Extract the JSON data (skip magic bytes)
                    byte[] jsonBytes = result.Buffer.Skip(MAGIC_BYTES.Length).ToArray();
                    string jsonData = Encoding.UTF8.GetString(jsonBytes);

                    // Deserialize the jsonData
                    var messageData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);


                    // verify app identifier 
                    // if the first token in the messageData is not same with APP_IDENTIFIER
                    // if (messageData["app"] != APP_IDENTIFIER)
                    // {
                    //     // not our app, ignore 
                    //     continue;
                    // }
                    //

                    if (!messageData.ContainsKey("app") || messageData["app"] != APP_IDENTIFIER)
                    {
                        continue;
                    }

                    // get sender info 
                    string senderIp = result.RemoteEndPoint.Address.ToString();
                    string deviceName = messageData["deviceName"];
                    string deviceType = messageData["deviceType"];
                    string content = messageData["content"];

                    // print formatted info 
                    System.Console.WriteLine($"Received from: {deviceName} ({deviceType} - {senderIp})");
                    System.Console.WriteLine($"Content: {content}");




                    // update clipboard if it's different from what we already have 
                    if (content != _lastClipboardContent)
                    {
                        _lastClipboardContent = content;
                        await ClipboardService.SetTextAsync(content);

                    }

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error receiving clipboard data: {ex.Message}");
                }
            }
        }
    }
}
