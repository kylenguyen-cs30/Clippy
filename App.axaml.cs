// file conatins the App class, which is the core application class 
// that controls app's lifecycle

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ClippySync;

// class inheritance from Application class 
public partial class App : Application
{
    // This method is called when the application start initializing
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // this method is called when the app finish initializing
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
