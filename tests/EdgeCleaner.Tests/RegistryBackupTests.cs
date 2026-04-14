using Microsoft.Win32;
using Shouldly;

namespace EdgeCleaner.Tests;

public class RegistryBackupTests
{
    [Fact]
    public void Export_EmptyList_ReturnsHeaderOnly()
    {
        var fake = new FakeRegistryOperations();
        var backup = new RegistryBackup(fake);

        var result = backup.Export([]);

        result.Content.ShouldStartWith("Windows Registry Editor Version 5.00");
        result.Content.Trim().ShouldBe("Windows Registry Editor Version 5.00");
        result.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Export_KeyWithStringValue_CorrectFormat()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Version", "120.0.0.1", RegistryValueKind.String);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Edge]");
        result.Content.ShouldContain("\"Version\"=\"120.0.0.1\"");
    }

    [Fact]
    public void Export_KeyWithDwordValue_CorrectFormat()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Enabled", 1, RegistryValueKind.DWord);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("\"Enabled\"=dword:00000001");
    }

    [Fact]
    public void Export_KeyWithBinaryValue_CorrectFormat()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Data", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, RegistryValueKind.Binary);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("\"Data\"=hex:de,ad,be,ef");
    }

    [Fact]
    public void Export_KeyWithMultiStringValue_CorrectFormat()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Urls", new[] { "https://example.com", "https://test.com" }, RegistryValueKind.MultiString);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("\"Urls\"=hex(7):");
    }

    [Fact]
    public void Export_KeyWithExpandStringValue_CorrectFormat()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Path", @"%ProgramFiles%\Edge", RegistryValueKind.ExpandString);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("\"Path\"=hex(2):");
    }

    [Fact]
    public void Export_KeyWithQwordValue_CorrectFormat()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "BigNum", (long)0x1234567890ABCDEF, RegistryValueKind.QWord);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("\"BigNum\"=hex(b):");
    }

    [Fact]
    public void Export_KeyWithSubkeys_RecursiveExport()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Version", "120", RegistryValueKind.String);
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey",
            "SubValue", "test", RegistryValueKind.String);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Edge]");
        result.Content.ShouldContain(@"[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Edge\SubKey]");
        result.Content.ShouldContain("\"Version\"=\"120\"");
        result.Content.ShouldContain("\"SubValue\"=\"test\"");
    }

    [Fact]
    public void Export_DefaultValue_UsesAtSign()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "", "default value", RegistryValueKind.String);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("@=\"default value\"");
    }

    [Fact]
    public void Export_BackslashInValue_Escaped()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Path", @"C:\Program Files\Edge", RegistryValueKind.String);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain(@"""Path""=""C:\\Program Files\\Edge""");
    }

    [Fact]
    public void Export_ValueOnlyEntry_ExportsOnlyThatValue()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            "Microsoft Edge", @"Software\Clients\StartMenuInternet\Microsoft Edge\Capabilities");
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            "Firefox", @"Software\Clients\StartMenuInternet\Firefox\Capabilities");

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            TargetCategory.StartMenuRegisteredApps, IsValueOnly: true, ValueName: "Microsoft Edge");
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Content.ShouldContain("\"Microsoft Edge\"=");
        result.Content.ShouldNotContain("\"Firefox\"=");
    }

    [Fact]
    public void FormatValue_String_CorrectlyFormatted()
    {
        var result = RegistryBackup.FormatValue("Name", "Value", RegistryValueKind.String);
        result.ShouldBe("\"Name\"=\"Value\"");
    }

    [Fact]
    public void FormatValue_DWord_CorrectlyFormatted()
    {
        var result = RegistryBackup.FormatValue("Count", 255, RegistryValueKind.DWord);
        result.ShouldBe("\"Count\"=dword:000000ff");
    }

    [Fact]
    public void EscapeString_BackslashesAndQuotes_Escaped()
    {
        // Input: C:\path\to\"file"
        // Expected: C:\\path\\to\\\"file\"
        var result = RegistryBackup.EscapeString("C:\\path\\to\\\"file\"");
        result.ShouldBe("C:\\\\path\\\\to\\\\\\\"file\\\"");
    }

    [Fact]
    public void ExportToFile_WritesFile()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge",
            "Test", "Value", RegistryValueKind.String);

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var tempFile = Path.GetTempFileName();

        try
        {
            var result = backup.ExportToFile([entry], tempFile);
            File.Exists(tempFile).ShouldBeTrue();
            var content = File.ReadAllText(tempFile);
            content.ShouldContain("Windows Registry Editor Version 5.00");
            result.Warnings.ShouldBeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Export_AccessDeniedOnValues_ReportsWarning()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.SetAccessDenied(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        var backup = new RegistryBackup(fake);
        var result = backup.Export([entry]);

        result.Warnings.ShouldNotBeEmpty();
        result.Warnings[0].ShouldContain("could not read values");
    }

    [Fact]
    public void FormatValue_RegNone_ExportedAsHex()
    {
        var data = new byte[] { 0x01, 0x02, 0x03 };
        var result = RegistryBackup.FormatValue("Flag", data, RegistryValueKind.None);
        result.ShouldBe("\"Flag\"=hex(0):01,02,03");
    }

    [Fact]
    public void FormatValue_UnknownKind_ByteArray_ExportedAsHex()
    {
        var data = new byte[] { 0xAA, 0xBB };
        var result = RegistryBackup.FormatValue("Data", data, (RegistryValueKind)99);
        result.ShouldBe("\"Data\"=hex(63):aa,bb");
    }
}
