using System;
using TextCopy;
using System.Threading.Tasks;
using System.Threading;

namespace ClippySync.Services;

public class ClipboardMonitor
{
    private string? _lastClipboardContent;
    private Timer? _timer;

    public event EventHandler<string>? ClipboardContentChanged;

    public void Start()
    {
        // check the clipboard every 500ms
        _timer = new Timer(CheckClipboard, null, 0, 500);
    }
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }


    private async void CheckClipboard(object? state)
    {
        try
        {
            string? currentContent = await ClipboardService.GetTextAsync();

            // if contetn changed and isn't null/empty
            if (currentContent != _lastClipboardContent && !string.IsNullOrEmpty(currentContent))
            {
                _lastClipboardContent = currentContent;
                ClipboardContentChanged?.Invoke(this, currentContent);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error Checking Clipboard: {ex.Message}");
        }
    }
    public async Task SetClipboardContentAsync(string content)
    {
        if (content == _lastClipboardContent) return;

        _lastClipboardContent = content;
        await ClipboardService.SetTextAsync(content);
    }
}
