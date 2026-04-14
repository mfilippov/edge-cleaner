using System.Security;
using Microsoft.Win32;

namespace EdgeCleaner.Tests;

public class FakeRegistryOperations : IRegistryOperations
{
    // key = "HIVE|path", value = dict of value names -> (object value, RegistryValueKind kind)
    private readonly Dictionary<string, Dictionary<string, (object Value, RegistryValueKind Kind)>> _keys = new();
    private readonly HashSet<string> _accessDenied = new();

    private static string MakeKey(RegistryHive hive, string path) => $"{hive}|{path}";

    public void AddKey(RegistryHive hive, string path)
    {
        var key = MakeKey(hive, path);
        _keys.TryAdd(key, new Dictionary<string, (object, RegistryValueKind)>());
    }

    public void AddValue(RegistryHive hive, string path, string name, object value, RegistryValueKind kind = RegistryValueKind.String)
    {
        AddKey(hive, path);
        _keys[MakeKey(hive, path)][name] = (value, kind);
    }

    public void SetAccessDenied(RegistryHive hive, string path)
    {
        _accessDenied.Add(MakeKey(hive, path));
    }

    public bool KeyExists(RegistryHive hive, string path)
    {
        var key = MakeKey(hive, path);
        return _keys.ContainsKey(key);
    }

    public string[] GetSubKeyNames(RegistryHive hive, string path)
    {
        var prefix = MakeKey(hive, path) + @"\";
        var subKeys = new HashSet<string>();

        foreach (var key in _keys.Keys)
        {
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var remainder = key[prefix.Length..];
                var firstSep = remainder.IndexOf('\\');
                var subKeyName = firstSep >= 0 ? remainder[..firstSep] : remainder;
                subKeys.Add(subKeyName);
            }
        }

        return [.. subKeys.Order()];
    }

    public string[] GetValueNames(RegistryHive hive, string path)
    {
        var key = MakeKey(hive, path);
        if (_accessDenied.Contains(key))
            throw new SecurityException("Access denied");
        return _keys.TryGetValue(key, out var values) ? [.. values.Keys] : [];
    }

    public object? GetValue(RegistryHive hive, string path, string name)
    {
        var key = MakeKey(hive, path);
        if (_keys.TryGetValue(key, out var values) && values.TryGetValue(name, out var entry))
            return entry.Value;
        return null;
    }

    public RegistryValueKind GetValueKind(RegistryHive hive, string path, string name)
    {
        var key = MakeKey(hive, path);
        if (_keys.TryGetValue(key, out var values) && values.TryGetValue(name, out var entry))
            return entry.Kind;
        throw new InvalidOperationException($"Value not found: {hive}\\{path}\\{name}");
    }

    public void DeleteSubKeyTree(RegistryHive hive, string path)
    {
        var key = MakeKey(hive, path);
        if (_accessDenied.Contains(key))
            throw new SecurityException("Access denied");

        var prefix = key + @"\";
        var toRemove = _keys.Keys.Where(k => k == key || k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var k in toRemove)
            _keys.Remove(k);
    }

    public void DeleteValue(RegistryHive hive, string path, string name)
    {
        var key = MakeKey(hive, path);
        if (_accessDenied.Contains(key))
            throw new SecurityException("Access denied");
        if (_keys.TryGetValue(key, out var values))
            values.Remove(name);
    }
}
