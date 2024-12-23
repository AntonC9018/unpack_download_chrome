using Microsoft.Win32;

public static class NativeMessagingRegistryHelper
{
    private const string _nativeMessagingKey = @$"Software\Google\Chrome\NativeMessagingHosts\{AppConstants.AppName}";

    public static void Init(string installDirectory)
    {
        if (Registry.CurrentUser.OpenSubKey(_nativeMessagingKey, writable: true) is not { } key)
        {
            key = Registry.CurrentUser.CreateSubKey(_nativeMessagingKey, writable: true);
        }
        using (key)
        {
            const string defaultValueName = "";
            var fullManifestPath = Path.Combine(installDirectory, @"native_manifest.json");
            key.SetValue(defaultValueName, fullManifestPath, RegistryValueKind.String);
        }
    }

    public static void Deinit()
    {
        Registry.CurrentUser.DeleteSubKey(_nativeMessagingKey);
    }
}

