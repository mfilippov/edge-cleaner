using Microsoft.Win32;

namespace EdgeCleaner;

public record RegistryEntry(
    RegistryHive Hive,
    string Path,
    TargetCategory Category,
    bool IsValueOnly = false,
    string? ValueName = null)
{
    public string FullPath => $"{HiveToString(Hive)}\\{Path}";

    public static string HiveToString(RegistryHive hive) => hive switch
    {
        RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
        _ => hive.ToString()
    };
}

public class RegistryScanner
{
    private readonly IRegistryOperations _registry;

    public RegistryScanner(IRegistryOperations registry)
    {
        _registry = registry;
    }

    public List<RegistryEntry> ScanAll()
    {
        var results = new List<RegistryEntry>();

        ScanStaticKeys(results);
        ScanDynamicTargets(results);
        ScanDeepDynamicTargets(results);
        ScanTaskCacheSiblings(results);
        ScanSidBasedTargets(results);
        ScanClsidByValue(results);

        return results.DistinctBy(e => (e.Hive, e.Path, e.ValueName)).ToList();
    }

    private void ScanStaticKeys(List<RegistryEntry> results)
    {
        foreach (var target in EdgeTargets.StaticKeys)
        {
            if (target.IsValueOnly)
            {
                var value = _registry.GetValue(target.Hive, target.Path, target.ValueName!);
                if (value != null)
                    results.Add(new RegistryEntry(target.Hive, target.Path, target.Category, true, target.ValueName));
            }
            else
            {
                if (_registry.KeyExists(target.Hive, target.Path))
                    results.Add(new RegistryEntry(target.Hive, target.Path, target.Category));
            }
        }
    }

    private static bool IsExcludedPackage(string name) =>
        EdgeTargets.ExcludedPackagePatterns.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase));

    private void ScanDynamicTargets(List<RegistryEntry> results)
    {
        foreach (var target in EdgeTargets.DynamicTargets)
        {
            // Skip CLSID value-based search — handled separately
            if (target.ParentPath.Contains("CLSID"))
                continue;

            try
            {
                var subKeys = _registry.GetSubKeyNames(target.Hive, target.ParentPath);
                foreach (var subKey in subKeys)
                {
                    if (subKey.Contains(target.Pattern, StringComparison.OrdinalIgnoreCase) &&
                        !IsExcludedPackage(subKey))
                    {
                        var fullPath = $@"{target.ParentPath}\{subKey}";
                        results.Add(new RegistryEntry(target.Hive, fullPath, target.Category));
                    }
                }
            }
            catch
            {
                // Parent key doesn't exist or access denied
            }
        }
    }

    private void ScanDeepDynamicTargets(List<RegistryEntry> results)
    {
        foreach (var target in EdgeTargets.DeepDynamicTargets)
        {
            try
            {
                var buckets = _registry.GetSubKeyNames(target.Hive, target.ParentPath);
                foreach (var bucket in buckets)
                {
                    var bucketPath = $@"{target.ParentPath}\{bucket}";
                    try
                    {
                        var leaves = _registry.GetSubKeyNames(target.Hive, bucketPath);
                        foreach (var leaf in leaves)
                        {
                            if (leaf.Contains(target.Pattern, StringComparison.OrdinalIgnoreCase) &&
                                !IsExcludedPackage(leaf))
                            {
                                results.Add(new RegistryEntry(target.Hive, $@"{bucketPath}\{leaf}", target.Category));
                            }
                        }
                    }
                    catch
                    {
                        // Bucket subkey not accessible
                    }
                }
            }
            catch
            {
                // Parent key doesn't exist
            }
        }
    }

    private void ScanSidBasedTargets(List<RegistryEntry> results)
    {
        foreach (var target in EdgeTargets.SidBasedTargets)
        {
            try
            {
                var topSubKeys = _registry.GetSubKeyNames(target.Hive, target.ParentPath);
                foreach (var sidKey in topSubKeys)
                {
                    var sidPath = $@"{target.ParentPath}\{sidKey}";
                    try
                    {
                        var innerSubKeys = _registry.GetSubKeyNames(target.Hive, sidPath);
                        foreach (var inner in innerSubKeys)
                        {
                            if (inner.Contains(target.Pattern, StringComparison.OrdinalIgnoreCase) &&
                                !IsExcludedPackage(inner))
                            {
                                var fullPath = $@"{sidPath}\{inner}";
                                results.Add(new RegistryEntry(target.Hive, fullPath, target.Category));
                            }
                        }
                    }
                    catch
                    {
                        // Access denied to SID subkey
                    }
                }
            }
            catch
            {
                // Parent key doesn't exist
            }
        }
    }

    private void ScanTaskCacheSiblings(List<RegistryEntry> results)
    {
        const string taskCacheTree = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree";
        const string taskCacheTasks = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks";
        var hive = RegistryHive.LocalMachine;

        var treeEntries = results
            .Where(e => e.Hive == hive && e.Path.StartsWith(taskCacheTree + @"\", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var entry in treeEntries)
        {
            try
            {
                var id = _registry.GetValue(hive, entry.Path, "Id") as string;
                if (id != null)
                {
                    var tasksPath = $@"{taskCacheTasks}\{id}";
                    if (_registry.KeyExists(hive, tasksPath))
                        results.Add(new RegistryEntry(hive, tasksPath, TargetCategory.ScheduledTasks));
                }
            }
            catch
            {
                // Tree key may not have Id value or Tasks key may not exist
            }
        }
    }

    private void ScanClsidByValue(List<RegistryEntry> results)
    {
        var clsidTargets = EdgeTargets.DynamicTargets
            .Where(t => t.ParentPath.Contains("CLSID"))
            .ToList();

        if (clsidTargets.Count == 0) return;

        var patterns = clsidTargets.Select(t => t.Pattern).Distinct().ToList();

        foreach (var group in clsidTargets.GroupBy(t => (t.Hive, t.ParentPath)))
        {
            var hive = group.Key.Hive;
            var clsidPath = group.Key.ParentPath;

            try
            {
                var clsidSubKeys = _registry.GetSubKeyNames(hive, clsidPath);
                foreach (var clsidKey in clsidSubKeys)
                {
                    if (IsEdgeClsid(hive, clsidPath, clsidKey, patterns))
                    {
                        results.Add(new RegistryEntry(hive, $@"{clsidPath}\{clsidKey}", TargetCategory.ComObjects));
                    }
                }
            }
            catch
            {
                // CLSID key not accessible
            }
        }
    }

    private bool IsEdgeClsid(RegistryHive hive, string clsidPath, string clsidKey, List<string> patterns)
    {
        string[] valueSubKeys = ["InProcServer32", "InprocHandler32", "LocalServer32"];
        foreach (var valueSubKey in valueSubKeys)
        {
            var path = $@"{clsidPath}\{clsidKey}\{valueSubKey}";
            try
            {
                var value = _registry.GetValue(hive, path, "") as string;
                if (value != null)
                {
                    foreach (var pattern in patterns)
                    {
                        if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch
            {
                // Subkey doesn't exist
            }
        }

        // Also check default value of the CLSID key itself
        try
        {
            var defaultValue = _registry.GetValue(hive, $@"{clsidPath}\{clsidKey}", "") as string;
            if (defaultValue != null)
            {
                if (defaultValue.Contains("Edge", StringComparison.OrdinalIgnoreCase) &&
                    !defaultValue.Contains("EdgeGesture", StringComparison.OrdinalIgnoreCase) &&
                    !defaultValue.Contains("EdgeUI", StringComparison.OrdinalIgnoreCase))
                {
                    // Only match if it's clearly Edge-browser related
                    if (defaultValue.Contains("IEToEdge", StringComparison.OrdinalIgnoreCase) ||
                        defaultValue.Contains("MicrosoftEdge", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        catch
        {
            // No default value
        }

        return false;
    }
}
