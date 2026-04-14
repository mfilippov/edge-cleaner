# EdgeCleaner

Windows CLI tool to scan and remove Microsoft Edge registry traces. Supports backup to `.reg` files before deletion.

## Features

- Scans 100+ registry locations across HKLM, HKCU, and HKCR
- Groups findings by category: Edge Core, Update Service, Policies, File Associations, COM Objects, Services, Scheduled Tasks, AppX Packages, and more
- Creates importable `.reg` backup before deletion
- Self-contained single-file executable — no runtime required

## Usage

Run as Administrator:

```
EdgeCleaner.exe [options]
```

| Option | Description |
|---|---|
| `--scan-only` | Scan and report only, do not delete |
| `--no-backup` | Skip creating .reg backup file |
| `--force` | Delete without confirmation prompt |
| `--help` | Show help message |

## Building

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet build
dotnet test
```

### Publish self-contained executable

```bash
dotnet publish src/EdgeCleaner/EdgeCleaner.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

## License

[Apache License 2.0](LICENSE)
