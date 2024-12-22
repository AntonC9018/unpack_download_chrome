using Microsoft.Win32;

const string myappname = "unpack_zip";
const string nativeMessagingKey = @$"Software\Google\Chrome\NativeMessagingHosts\{myappname}";

if (Registry.CurrentUser.OpenSubKey(nativeMessagingKey, writable: true) is not { } key)
{
    key = Registry.CurrentUser.CreateSubKey(nativeMessagingKey, writable: true);

}
const string defaultValueName = "";
key.SetValue(defaultValueName, @"C:\Users\Anton\Desktop\ext\extension\native_manifest.json", RegistryValueKind.String);