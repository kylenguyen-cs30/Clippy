// This file defines the code-behind for your 
// main application window
using Avalonia.Controls;

namespace ClippySync;

// this methods inherit Avalonia's Window class. partial indicates class is split 
// between this .cs file and the MainWindow.axaml file
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
