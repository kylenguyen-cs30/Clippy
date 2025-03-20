using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClippySync.Models;


namespace ClippySync.Services;

public class NetworkServices
{
    private const int Port = 12345;
    private UdpClient? _udpClient;
    private string _localIp = "";

    public event EventHandler<ClipboardItem>? ClipboardContentReceived;

    public string LocalIpAddress => _localIp;

    public async Task StartAsync()
    {

        // get local IP address 
        _localIp = GetLocalIpAddress();

        // start UDP listener 
        _udpClient = new UdpClient(Port);


        // start listening for messages
        await Task.Run(ListenForMessages);
    }

    public void Stop()
    {
        _udpClient?.Close();
        _udpClient = null;
    }


    // Broadcast clipboard method
    public async Task BroadcasClipboardContentAsync(string content)
    {
        if (_udpClient == null)
        {
            return;
        }
        try
        {
            // create a simple message format: content|computerName|ipAddress 
            string computerName = Environment.MachineName;
            string message = $"{content}|{computerName}|{_localIp}";
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Broadcast to local network 
            await _udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Broadcast, Port));
        }
        catch (Exception ex)
        {

            Console.WriteLine($"Error Broadcasting content: {ex.Message}");
        }
    }


    private async Task ListenForMessages()
    {

        // when UdpClient object is not destructed 
        while (_udpClient != null)
        {
            try
            {
                // receiving result from the UDP client
                UdpReceiveResult result = await _udpClient.ReceiveAsync();
                // parse the string from receiving async message
                string message = Encoding.UTF8.GetString(result.Buffer);

                // parse the message 
                string[] parts = message.Split('|');
                if (parts.Length == 3)
                {
                    string content = parts[0];
                    string computerName = parts[1];
                    string ipAddress = parts[2];

                    // skip messages from ourselves 
                    if (ipAddress == _localIp) continue;

                    var clipboardItem = new ClipboardItem
                    {
                        Content = content,
                        ComputerName = computerName,
                        IpAddress = ipAddress,
                        TimeStamp = DateTime.Now
                    };

                    ClipboardContentReceived?.Invoke(this, clipboardItem);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
                // small delay to prevent tight loop 
                await Task.Delay(1000);
            }

        }

    }
    private string GetLocalIpAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}
