namespace EdgeCleaner;

public record CleanResult(int Deleted, int Skipped, int Errors, List<string> ErrorMessages);

public class RegistryCleaner
{
    private readonly IRegistryOperations _registry;

    public RegistryCleaner(IRegistryOperations registry)
    {
        _registry = registry;
    }

    public CleanResult DeleteAll(List<RegistryEntry> entries, Action<RegistryEntry, string>? onProgress = null)
    {
        int deleted = 0, skipped = 0, errors = 0;
        var errorMessages = new List<string>();

        foreach (var entry in entries)
        {
            try
            {
                if (entry.IsValueOnly)
                {
                    var value = _registry.GetValue(entry.Hive, entry.Path, entry.ValueName!);
                    if (value == null)
                    {
                        skipped++;
                        onProgress?.Invoke(entry, "skipped");
                        continue;
                    }

                    _registry.DeleteValue(entry.Hive, entry.Path, entry.ValueName!);
                    deleted++;
                    onProgress?.Invoke(entry, "deleted");
                }
                else
                {
                    if (!_registry.KeyExists(entry.Hive, entry.Path))
                    {
                        skipped++;
                        onProgress?.Invoke(entry, "skipped");
                        continue;
                    }

                    _registry.DeleteSubKeyTree(entry.Hive, entry.Path);
                    deleted++;
                    onProgress?.Invoke(entry, "deleted");
                }
            }
            catch (Exception ex)
            {
                errors++;
                var msg = $"{entry.FullPath}: {ex.Message}";
                errorMessages.Add(msg);
                onProgress?.Invoke(entry, "error");
            }
        }

        return new CleanResult(deleted, skipped, errors, errorMessages);
    }
}
