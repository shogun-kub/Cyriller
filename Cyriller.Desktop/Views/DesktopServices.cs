using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace Cyriller.Desktop.Views
{
    /// <summary>
    /// Доступ к объектам окна, которые в Avalonia 11 больше не висят на <see cref="Application"/>.
    /// </summary>
    public static class DesktopServices
    {
        public static Window MainWindow
        {
            get
            {
                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    return desktop.MainWindow;
                }

                return null;
            }
        }

        public static IClipboard Clipboard => MainWindow?.Clipboard;

        public static void Shutdown()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
}
