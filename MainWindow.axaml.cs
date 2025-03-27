using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Clippy.Console.Models;
using Clippy.Console.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Clippy.Console
{
    public partial class MainWindow : Window
    {
        private readonly ClipboardService _clipboardService;
        private readonly NetworkService _networkService;

        // Observable collection for UI binding 
        public ObservableCollection<Device> Devices { get; } = new ObservableCollection<Device>();
        public ObservableCollection<ClipboardItem> ClipboardItems { get; } = new ObservableCollection<ClipboardItem>();
        public MainWindow()
        {
            InitializeComponent();

            // set teh data context to this instance so bindings work
            DataContext = this;

            // create and start services 
            _clipboardService = new ClipboardService();
            _networkService = new NetworkService();

            // wire up event handlers 
            _clipboardService.ClipboardChanged += OnClipboardChanged;
            _networkService.ClipboardDataReceived += OnClipboardDataRecieved;

            // statr services 
            _clipboardService.Start();
            _networkService.Start();

        }

        private async void OnClipboardChanged(object sender, string content)
        {
            // when local clipboard changes, broadcast to network 
            await _networkService.BroadcastClipboardData(content);

            // add to local history (on UI Thread)
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                AddClipboardItem(content, Environment.MachineName, "Local", GetDeviceType());
            });
        }


        private async void OnClipboardDataRecieved(object sender, ClipboardItem item)
        {
            // when receiving clipboard data from network 
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                // update local clipboard 
                await _clipboardService.SetClipboardContentAsync(item.Content);

                // add to history 
                AddClipboardItem(item.Content, item.ComputerName, item.IpAddress, item.DeviceType);
            });
        }


        private void OnDeviceDiscovered(object sender, Device device)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                // check if device already exists in the list 
                if (!Devices.Any(d => d.IpAddress == device.IpAddress && d.Name == device.Name))
                {
                    Devices.Add(device);
                }
            });
        }

        private void AddClipboardItem(string content, string computerName, string ipAddress, string deviceType)
        {
            var item = new ClipboardItem(content, computerName, ipAddress, deviceType);
            ClipboardItems.Insert(0, item); // add to the beginning of the list 

            // keep history limited to last 20 items 
            while (ClipboardItems.Count > 20)
            {
                ClipboardItems.RemoveAt(ClipboardItems.Count - 1);
            }
        }

        private string GetDeviceType()
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

        protected override void OnClosed(EventArgs e)
        {
            // cleanup resourecs 
            _clipboardService.Stop();
            _networkService.Stop();
            base.OnClosed(e);
        }


        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }
    }
}
