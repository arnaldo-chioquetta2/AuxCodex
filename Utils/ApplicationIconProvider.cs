using System.Drawing;

namespace AuxCodex.Utils;

/// <summary>
/// Provides the single application icon used by the tray and AuxCodex windows.
/// </summary>
public static class ApplicationIconProvider
{
    private static readonly Lazy<Icon> CachedIcon = new(LoadIcon);

    public static Icon Icon => CachedIcon.Value;

    private static Icon LoadIcon()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            return SystemIcons.Application;
        }
    }
}
