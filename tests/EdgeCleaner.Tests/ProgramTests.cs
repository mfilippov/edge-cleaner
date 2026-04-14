using Shouldly;

namespace EdgeCleaner.Tests;

public class ProgramTests
{
    [Fact]
    public void ParseArguments_NoArgs_ReturnsDefaults()
    {
        var options = Program.ParseArguments([]);

        options.ShouldNotBeNull();
        options.ScanOnly.ShouldBeFalse();
        options.NoBackup.ShouldBeFalse();
        options.Force.ShouldBeFalse();
    }

    [Fact]
    public void ParseArguments_ScanOnly_Parsed()
    {
        var options = Program.ParseArguments(["--scan-only"]);

        options.ShouldNotBeNull();
        options!.ScanOnly.ShouldBeTrue();
    }

    [Fact]
    public void ParseArguments_NoBackup_Parsed()
    {
        var options = Program.ParseArguments(["--no-backup"]);

        options.ShouldNotBeNull();
        options!.NoBackup.ShouldBeTrue();
    }

    [Fact]
    public void ParseArguments_Force_Parsed()
    {
        var options = Program.ParseArguments(["--force"]);

        options.ShouldNotBeNull();
        options!.Force.ShouldBeTrue();
    }

    [Fact]
    public void ParseArguments_AllFlags_Combined()
    {
        var options = Program.ParseArguments(["--scan-only", "--no-backup", "--force"]);

        options.ShouldNotBeNull();
        options!.ScanOnly.ShouldBeTrue();
        options.NoBackup.ShouldBeTrue();
        options.Force.ShouldBeTrue();
    }

    [Fact]
    public void ParseArguments_UnknownArg_ReturnsNull()
    {
        var options = Program.ParseArguments(["--unknown"]);
        options.ShouldBeNull();
    }

    [Fact]
    public void ParseArguments_Help_ReturnsNull()
    {
        var options = Program.ParseArguments(["--help"]);
        options.ShouldBeNull();
    }

    [Fact]
    public void ParseArguments_ShortHelp_ReturnsNull()
    {
        var options = Program.ParseArguments(["-h"]);
        options.ShouldBeNull();
    }

    [Fact]
    public void ParseArguments_CaseInsensitive()
    {
        var options = Program.ParseArguments(["--SCAN-ONLY"]);

        options.ShouldNotBeNull();
        options!.ScanOnly.ShouldBeTrue();
    }

    [Fact]
    public void Run_ScanOnly_EmptyRegistry_ReturnsZero()
    {
        var fake = new FakeRegistryOperations();
        var options = new Options { ScanOnly = true };

        var exitCode = Program.Run(fake, options);

        exitCode.ShouldBe(0);
    }

    [Fact]
    public void Run_ScanOnly_WithEntries_ReturnsZero()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(Microsoft.Win32.RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var options = new Options { ScanOnly = true };

        var exitCode = Program.Run(fake, options);

        exitCode.ShouldBe(0);
    }

    [Fact]
    public void Run_ForceDelete_NoEntries_ReturnsZero()
    {
        var fake = new FakeRegistryOperations();
        var options = new Options { Force = true, NoBackup = true };

        var exitCode = Program.Run(fake, options);

        exitCode.ShouldBe(0);
    }

    [Fact]
    public void Run_ForceDelete_WithEntries_DeletesThem()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(Microsoft.Win32.RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var options = new Options { Force = true, NoBackup = true };

        var exitCode = Program.Run(fake, options);

        exitCode.ShouldBe(0);
        fake.KeyExists(Microsoft.Win32.RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge").ShouldBeFalse();
    }

    [Fact]
    public void Run_ForceDelete_WithErrors_ReturnsTwo()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(Microsoft.Win32.RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.SetAccessDenied(Microsoft.Win32.RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var options = new Options { Force = true, NoBackup = true };

        var exitCode = Program.Run(fake, options);

        exitCode.ShouldBe(2);
    }

    [Fact]
    public void IsAdministrator_DoesNotThrow()
    {
        Should.NotThrow(() => Program.IsAdministrator());
    }
}
