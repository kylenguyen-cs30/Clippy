namespace Clippy.Console.Models
{
    public class Device
    {
        public string Name { get; set; }
        public string IpAddress { get; set; }
        public string DeviceType { get; set; }

        public Device(string name, string ipAddress, string deviceType)
        {
            Name = name;
            IpAddress = ipAddress;
            DeviceType = deviceType;
        }
    }
}
