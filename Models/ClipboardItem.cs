using System;

namespace Clippy.Console.Models
{
    public class ClipboardItem
    {
        public string Content { get; set; }
        public string ComputerName { get; set; }
        public string IpAddress { get; set; }
        public string DeviceType { get; set; }
        public DateTime Timestamp { get; set; }

        public ClipboardItem(string content, string computerName, string ipAddress, string deviceType)
        {
            Content = content;
            ComputerName = computerName;
            IpAddress = ipAddress;
            DeviceType = deviceType;
            Timestamp = DateTime.Now;
        }
    }
}
