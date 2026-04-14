using System.Security.Principal;

namespace EdgeCleaner;

public class Program
{
    public static int Main(string[] args)
    {
        var options = ParseArguments(args);
        if (options == null) return 1;

        if (!IsAdministrator())
        {
            WriteColor("This program must be run as Administrator.", ConsoleColor.Red);
            WriteColor("Right-click and select 'Run as administrator'.", ConsoleColor.Yellow);
            return 1;
        }

        var registry = new RegistryOperations();
        return Run(registry, options);
    }

    internal static int Run(IRegistryOperations registry, Options options)
    {
        Console.WriteLine("=== Edge Registry Cleaner ===");
        Console.WriteLine();

        // Scan
        WriteColor("Scanning registry for Microsoft Edge traces...", ConsoleColor.Cyan);
        Console.WriteLine();

        var scanner = new RegistryScanner(registry);
        var entries = scanner.ScanAll();

        if (entries.Count == 0)
        {
            WriteColor("No Microsoft Edge registry entries found.", ConsoleColor.Green);
            return 0;
        }

        // Report
        PrintReport(entries);

        if (options.ScanOnly)
        {
            WriteColor($"Scan complete. {entries.Count} entries found. Use without --scan-only to delete.", ConsoleColor.Cyan);
            return 0;
        }

        // Confirmation
        if (!options.Force)
        {
            Console.WriteLine();
            if (!options.NoBackup)
                WriteColor($"Create backup and delete {entries.Count} registry entries? [Y/N]: ", ConsoleColor.Yellow, newLine: false);
            else
                WriteColor($"Delete {entries.Count} registry entries WITHOUT backup? [Y/N]: ", ConsoleColor.Yellow, newLine: false);

            var response = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (response != "Y")
            {
                WriteColor("Operation cancelled.", ConsoleColor.Yellow);
                return 0;
            }
        }

        // Backup
        if (!options.NoBackup)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var backupFile = $"edge-backup-{timestamp}.reg";
            WriteColor($"Creating backup: {backupFile}", ConsoleColor.Cyan);

            var backup = new RegistryBackup(registry);
            var backupResult = backup.ExportToFile(entries, backupFile);
            WriteColor($"Backup saved to {Path.GetFullPath(backupFile)}", ConsoleColor.Green);

            if (backupResult.Warnings.Count > 0)
            {
                Console.WriteLine();
                WriteColor($"WARNING: Backup is incomplete ({backupResult.Warnings.Count} errors):", ConsoleColor.Red);
                foreach (var warning in backupResult.Warnings)
                    WriteColor($"  - {warning}", ConsoleColor.Red);
                Console.WriteLine();

                if (!options.Force)
                {
                    WriteColor("Proceed with deletion despite incomplete backup? [Y/N]: ", ConsoleColor.Yellow, newLine: false);
                    var resp = Console.ReadLine()?.Trim().ToUpperInvariant();
                    if (resp != "Y")
                    {
                        WriteColor("Operation cancelled. Backup file has been kept.", ConsoleColor.Yellow);
                        return 0;
                    }
                }
            }

            Console.WriteLine();
        }

        // Delete
        WriteColor("Deleting registry entries...", ConsoleColor.Cyan);
        Console.WriteLine();

        var cleaner = new RegistryCleaner(registry);
        var result = cleaner.DeleteAll(entries, (entry, status) =>
        {
            var color = status switch
            {
                "deleted" => ConsoleColor.Green,
                "skipped" => ConsoleColor.Yellow,
                "error" => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };
            var prefix = status switch
            {
                "deleted" => "[DEL]",
                "skipped" => "[SKIP]",
                "error" => "[ERR]",
                _ => "[???]"
            };
            var label = entry.IsValueOnly ? $"{entry.FullPath} (value: {entry.ValueName})" : entry.FullPath;
            WriteColor($"  {prefix} {label}", color);
        });

        // Summary
        Console.WriteLine();
        Console.WriteLine("=== Summary ===");
        WriteColor($"  Deleted: {result.Deleted}", ConsoleColor.Green);
        if (result.Skipped > 0)
            WriteColor($"  Skipped: {result.Skipped}", ConsoleColor.Yellow);
        if (result.Errors > 0)
        {
            WriteColor($"  Errors:  {result.Errors}", ConsoleColor.Red);
            foreach (var err in result.ErrorMessages)
                WriteColor($"    - {err}", ConsoleColor.Red);
        }

        return result.Errors > 0 ? 2 : 0;
    }

    private static void PrintReport(List<RegistryEntry> entries)
    {
        var grouped = entries.GroupBy(e => e.Category).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            WriteColor($"[{group.Key}] ({group.Count()} entries)", ConsoleColor.White);
            foreach (var entry in group)
            {
                var label = entry.IsValueOnly
                    ? $"  {entry.FullPath} -> value: \"{entry.ValueName}\""
                    : $"  {entry.FullPath}";
                Console.WriteLine(label);
            }
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {entries.Count} registry entries found.");
    }

    internal static Options? ParseArguments(string[] args)
    {
        var options = new Options();

        foreach (var arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--scan-only":
                    options.ScanOnly = true;
                    break;
                case "--no-backup":
                    options.NoBackup = true;
                    break;
                case "--force":
                    options.Force = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    return null;
                default:
                    WriteColor($"Unknown argument: {arg}", ConsoleColor.Red);
                    PrintHelp();
                    return null;
            }
        }

        return options;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage: EdgeCleaner [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --scan-only    Scan and report only, do not delete");
        Console.WriteLine("  --no-backup    Skip creating .reg backup file");
        Console.WriteLine("  --force        Delete without confirmation prompt");
        Console.WriteLine("  --help, -h     Show this help message");
    }

    internal static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows()) return false;
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void WriteColor(string text, ConsoleColor color, bool newLine = true)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (newLine)
            Console.WriteLine(text);
        else
            Console.Write(text);
        Console.ForegroundColor = prev;
    }
}

public class Options
{
    public bool ScanOnly { get; set; }
    public bool NoBackup { get; set; }
    public bool Force { get; set; }
}
