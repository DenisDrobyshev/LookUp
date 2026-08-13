namespace LookUp;

/// <summary>Loads the embedded application icon at the size the tray wants.</summary>
internal static class AppResources
{
    private static Icon? _trayIcon;

    public static Icon TrayIcon => _trayIcon ??= LoadTrayIcon();

    private static Icon LoadTrayIcon()
    {
        try
        {
            var assembly = typeof(AppResources).Assembly;
            using var stream = assembly.GetManifestResourceStream("LookUp.lookup.ico");
            if (stream is not null)
                return new Icon(stream, SystemInformation.SmallIconSize);
        }
        catch
        {
            // Fall through to a system icon so the tray still shows something.
        }

        return SystemIcons.Application;
    }
}
