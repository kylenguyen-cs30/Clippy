// Name : Hoang Nguyen
// Project: Clippy
//
using Avalonia;
using System;

namespace Clippy.Console
{
    class Program
    {
        // this method is needed for IDE Previewer infrastructure 
        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();

        // main entry point 
        //
        public static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
    }
}
