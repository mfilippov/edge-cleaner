using Microsoft.Win32;

namespace EdgeCleaner;

public class RegistryOperations : IRegistryOperations
{
    private static RegistryKey GetBaseKey(RegistryHive hive) => hive switch
    {
        RegistryHive.LocalMachine => Registry.LocalMachine,
        RegistryHive.CurrentUser => Registry.CurrentUser,
        RegistryHive.ClassesRoot => Registry.ClassesRoot,
        _ => throw new ArgumentException($"Unsupported hive: {hive}")
    };

    public bool KeyExists(RegistryHive hive, string path)
    {
        using var key = GetBaseKey(hive).OpenSubKey(path);
        return key != null;
    }

    public string[] GetSubKeyNames(RegistryHive hive, string path)
    {
        using var key = GetBaseKey(hive).OpenSubKey(path);
        return key?.GetSubKeyNames() ?? [];
    }

    public string[] GetValueNames(RegistryHive hive, string path)
    {
        using var key = GetBaseKey(hive).OpenSubKey(path);
        return key?.GetValueNames() ?? [];
    }

    public object? GetValue(RegistryHive hive, string path, string name)
    {
        using var key = GetBaseKey(hive).OpenSubKey(path);
        return key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
    }

    public RegistryValueKind GetValueKind(RegistryHive hive, string path, string name)
    {
        using var key = GetBaseKey(hive).OpenSubKey(path);
        if (key == null)
            throw new InvalidOperationException($"Key not found: {hive}\\{path}");
        return key.GetValueKind(name);
    }

    public void DeleteSubKeyTree(RegistryHive hive, string path)
    {
        var baseKey = GetBaseKey(hive);
        baseKey.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
    }

    public void DeleteValue(RegistryHive hive, string path, string name)
    {
        using var key = GetBaseKey(hive).OpenSubKey(path, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}
