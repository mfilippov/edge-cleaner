using System.Text;
using Microsoft.Win32;

namespace EdgeCleaner;

public record BackupResult(string Content, List<string> Warnings);

public class RegistryBackup
{
    private readonly IRegistryOperations _registry;

    public RegistryBackup(IRegistryOperations registry)
    {
        _registry = registry;
    }

    public BackupResult Export(List<RegistryEntry> entries)
    {
        var sb = new StringBuilder();
        var warnings = new List<string>();
        sb.AppendLine("Windows Registry Editor Version 5.00");
        sb.AppendLine();

        foreach (var entry in entries)
        {
            if (entry.IsValueOnly)
            {
                ExportValue(sb, entry);
            }
            else
            {
                ExportKey(sb, entry.Hive, entry.Path, warnings);
            }
        }

        return new BackupResult(sb.ToString(), warnings);
    }

    public BackupResult ExportToFile(List<RegistryEntry> entries, string filePath)
    {
        var result = Export(entries);
        File.WriteAllText(filePath, result.Content, Encoding.Unicode);
        return result;
    }

    private void ExportValue(StringBuilder sb, RegistryEntry entry)
    {
        var fullKeyPath = $"{RegistryEntry.HiveToString(entry.Hive)}\\{entry.Path}";
        sb.AppendLine($"[{fullKeyPath}]");

        var value = _registry.GetValue(entry.Hive, entry.Path, entry.ValueName!);
        if (value != null)
        {
            var kind = _registry.GetValueKind(entry.Hive, entry.Path, entry.ValueName!);
            sb.AppendLine(FormatValue(entry.ValueName!, value, kind));
        }

        sb.AppendLine();
    }

    private void ExportKey(StringBuilder sb, RegistryHive hive, string path, List<string> warnings)
    {
        var fullKeyPath = $"{RegistryEntry.HiveToString(hive)}\\{path}";
        sb.AppendLine($"[{fullKeyPath}]");

        try
        {
            var valueNames = _registry.GetValueNames(hive, path);
            foreach (var name in valueNames)
            {
                var value = _registry.GetValue(hive, path, name);
                if (value == null) continue;

                var kind = _registry.GetValueKind(hive, path, name);
                sb.AppendLine(FormatValue(name, value, kind));
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"{fullKeyPath}: could not read values ({ex.Message})");
        }

        sb.AppendLine();

        // Recurse into subkeys
        try
        {
            var subKeys = _registry.GetSubKeyNames(hive, path);
            foreach (var subKey in subKeys)
            {
                ExportKey(sb, hive, $@"{path}\{subKey}", warnings);
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"{fullKeyPath}: could not enumerate subkeys ({ex.Message})");
        }
    }

    internal static string FormatValue(string name, object value, RegistryValueKind kind)
    {
        var quotedName = name == "" ? "@" : $"\"{EscapeString(name)}\"";

        return kind switch
        {
            RegistryValueKind.String =>
                $"{quotedName}=\"{EscapeString((string)value)}\"",
            RegistryValueKind.DWord =>
                $"{quotedName}=dword:{(int)value:x8}",
            RegistryValueKind.QWord =>
                $"{quotedName}=hex(b):{FormatBytes(BitConverter.GetBytes((long)value))}",
            RegistryValueKind.Binary =>
                $"{quotedName}=hex:{FormatBytes((byte[])value)}",
            RegistryValueKind.MultiString =>
                $"{quotedName}=hex(7):{FormatBytes(EncodeMultiString((string[])value))}",
            RegistryValueKind.ExpandString =>
                $"{quotedName}=hex(2):{FormatBytes(Encoding.Unicode.GetBytes((string)value + "\0"))}",
            RegistryValueKind.None or RegistryValueKind.Unknown when value is byte[] bytes =>
                $"{quotedName}=hex(0):{FormatBytes(bytes)}",
            _ when value is byte[] bytes =>
                $"{quotedName}=hex({(int)kind:x}):{FormatBytes(bytes)}",
            _ => $"{quotedName}=\"{EscapeString(value.ToString() ?? "")}\"",
        };
    }

    internal static string EscapeString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string FormatBytes(byte[] bytes) =>
        string.Join(",", bytes.Select(b => b.ToString("x2")));

    private static byte[] EncodeMultiString(string[] strings)
    {
        var combined = string.Join("\0", strings) + "\0\0";
        return Encoding.Unicode.GetBytes(combined);
    }
}
