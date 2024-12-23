using Microsoft.Win32;

public static class InstallationHelper
{
    public enum Arch
    {
        _64bit,
        _32bit,
    }

    public static bool CheckIsInstalledChromeArch(Arch arch)
    {
        var view = arch switch
        {
            Arch._32bit => RegistryView.Registry32,
            Arch._64bit => RegistryView.Registry64,
            _ => throw new InvalidOperationException(),
        };
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
        using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", writable: false);
        if (key == null)
        {
            return false;
        }
        foreach (var subkeyName in key.GetSubKeyNames())
        {
            using var subkey = key.OpenSubKey(subkeyName, writable: false);
            if (subkey is null)
            {
                continue;
            }
            if (subkey.GetValue("DisplayName") is not string displayName)
            {
                continue;
            }

            // This method doesn't exist in the standard lib in net framework.
            bool Contains()
            {
                const string search = "chrome";
                int minLength = displayName.Length - search.Length;

                for (int i = 0; i < minLength; i++)
                {
                    if (ContainsSub())
                    {
                        return true;
                    }
                    bool ContainsSub()
                    {
                        for (int j = 0; j < search.Length; j++)
                        {
                            char a = char.ToLower(displayName[i + j]);
                            char b = search[j];
                            if (a != b)
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                return false;
            }
            if (Contains())
            {
                continue;
            }
            return true;
        }
        return false;
    }

    public static RegistryKey AdminMaybe(RegistryKey? key)
    {
        if (key == null)
        {
            throw new InvalidOperationException("Should start in admin probably? idk");
        }
        return key;
    }

    public static void InitializeExtensionRegistryKey(string installDirectory)
    {
        bool anyWritten = false;
        foreach (var arch in new[]
                 {
                     Arch._64bit,
                     Arch._32bit,
                 })
        {
            if (!CheckIsInstalledChromeArch(arch))
            {
                continue;
            }
            anyWritten = true;


            string extensionsKeyName = arch switch
            {
                Arch._64bit => @"Software\Wow6432Node\Google\Chrome\Extensions",
                Arch._32bit => @"Software\Google\Chrome\Extensions",
                _ => throw new InvalidCastException(),
            };
            string id = AppConstants.Id;

            var key = Registry.LocalMachine.OpenSubKey(extensionsKeyName);
            key = AdminMaybe(key);
            using var t1 = key;

            var idKey = key.OpenSubKey(id, writable: true);
            if (idKey is null)
            {
                idKey = key.CreateSubKey(id, writable: true);
            }
            idKey = AdminMaybe(idKey);
            using var t2 = idKey;

            // idKey.SetValue();
        }

        if (!anyWritten)
        {
            throw new InvalidOperationException("No chrome installed?");
        }
    }
}
