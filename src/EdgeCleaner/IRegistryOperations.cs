using Microsoft.Win32;

namespace EdgeCleaner;

public interface IRegistryOperations
{
    bool KeyExists(RegistryHive hive, string path);
    string[] GetSubKeyNames(RegistryHive hive, string path);
    string[] GetValueNames(RegistryHive hive, string path);
    object? GetValue(RegistryHive hive, string path, string name);
    RegistryValueKind GetValueKind(RegistryHive hive, string path, string name);
    void DeleteSubKeyTree(RegistryHive hive, string path);
    void DeleteValue(RegistryHive hive, string path, string name);
}
