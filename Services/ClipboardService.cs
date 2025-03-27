using System;
using System.Threading;
using System.Threading.Tasks;
using TextCopy;

namespace Clippy.Console.Services
{
    public class ClipboardService
    {
        private string _lastClipboardContent = "";
        private CancellationTokenSource _cancellationTokenSource; // cancellation token in order to stop the threading

        public event EventHandler<string> ClipboardChanged;


        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorClipboardAsync(() => MonitorClipboardAsync(_cancellationTokenSource.Token)));
        }


        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task MonitorClipboardAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    string currentText = await TextCopy.ClipboardService.GetTextAsync();
                    if (currentText != null && currentText != _lastClipboardContent)
                    {
                        _lastClipboardContent = currentText;
                        ClipboardChanged?.Invoke(this, currentText);
                    }
                    await Task.Delay(500, token);

                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error Monitoring Clipboard: {ex.Message}");
                }
            }
        }
        public async Task SetClipboardContentAsync(string content)
        {
            if (content != _lastClipboardContent)
            {
                _lastClipboardContent = content;
                await TextCopy.ClipboardService.SetTextAsync(content);

            }
        }
    }
}
