using CrimsonDnsManager.App.Services;
using CrimsonDnsManager.App.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using Windows.Graphics;

namespace CrimsonDnsManager.App;

public sealed partial class MainWindow : Window
{
    private static readonly string LogPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrimsonDnsManager", "startup.log");

    public MainWindow()
    {
        File.AppendAllText(LogPath, $"MainWindow constructor reached: {DateTime.Now:O}{Environment.NewLine}");

        Title = "Crimson DNS Manager";

        File.AppendAllText(LogPath, $"Creating ShellView: {DateTime.Now:O}{Environment.NewLine}");
        var shell = new ShellView();
        Content = shell;
        File.AppendAllText(LogPath, $"ShellView assigned: {DateTime.Now:O}{Environment.NewLine}");

        try
        {
            App.GetService<WindowHandleProvider>().SetWindow(this);

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);

            appWindow.SetIcon("Assets/Icons/app.ico");
            appWindow.Resize(new SizeInt32(1360, 840));

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(shell.TitleBarDragArea);

            File.AppendAllText(LogPath, $"Window chrome configured: {DateTime.Now:O}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogPath, $"Window chrome exception: {ex}{Environment.NewLine}");
        }
    }
}
