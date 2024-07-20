namespace Home_Assistant_Agent_for_SteamVR;

using Windows.Storage;

public static class AppSettings
{
    private static readonly ApplicationDataContainer LocalSettings = ApplicationData.Current.LocalSettings;

    public static bool LaunchMinimized
    {
        get => (bool)(LocalSettings.Values[nameof(LaunchMinimized)] ?? false);
        set => LocalSettings.Values[nameof(LaunchMinimized)] = value;
    }

    public static bool EnableTray
    {
        get => (bool)(LocalSettings.Values[nameof(EnableTray)] ?? true);
        set => LocalSettings.Values[nameof(EnableTray)] = value;
    }

    public static bool ExitWithSteamVR
    {
        get => (bool)(LocalSettings.Values[nameof(ExitWithSteamVR)] ?? true);
        set => LocalSettings.Values[nameof(ExitWithSteamVR)] = value;
    }

    public static bool AlwaysOnTop
    {
        get => (bool)(LocalSettings.Values[nameof(AlwaysOnTop)] ?? false);
        set => LocalSettings.Values[nameof(AlwaysOnTop)] = value;
    }

    public static bool EnableNotifyPlugin
    {
        get => (bool)(LocalSettings.Values[nameof(EnableNotifyPlugin)] ?? true);
        set => LocalSettings.Values[nameof(EnableNotifyPlugin)] = value;
    }

    public static int Port
    {
        get => (int)(LocalSettings.Values[nameof(Port)] ?? 8077);
        set => LocalSettings.Values[nameof(Port)] = value;
    }

    public static string ManifestFilePath
    {
        get => (string)(LocalSettings.Values[nameof(ManifestFilePath)] ?? string.Empty);
        set => LocalSettings.Values[nameof(ManifestFilePath)] = value;
    }
}