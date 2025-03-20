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

        private static async Task ListenForClipboardUpdateAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // wait for Broadcast
                    UdpReceiveResult result = await _receiver.ReceiveAsync();

                    // convert to text 
                    string recievedText = Encoding.UTF8.GetString(result.Buffer);
                    System.Console.WriteLine($"Received From network: {recievedText}");

                    // update clipboard if it's different from what we already have 
                    if (recievedText != _lastClipboardContent)
                    {
                        _lastClipboardContent = recievedText;
                        await ClipboardService.SetTextAsync(recievedText);

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
