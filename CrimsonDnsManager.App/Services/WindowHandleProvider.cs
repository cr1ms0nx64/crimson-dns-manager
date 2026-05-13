using Microsoft.UI.Xaml;

namespace CrimsonDnsManager.App.Services;

public sealed class WindowHandleProvider
{
    public Window? CurrentWindow { get; private set; }

    public void SetWindow(Window window) => CurrentWindow = window;

    public nint GetHandle()
    {
        if (CurrentWindow is null)
        {
            throw new InvalidOperationException("Main window is not registered yet.");
        }

        return WinRT.Interop.WindowNative.GetWindowHandle(CurrentWindow);
    }
}
