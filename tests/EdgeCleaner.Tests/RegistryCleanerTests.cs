using Microsoft.Win32;
using Shouldly;

namespace EdgeCleaner.Tests;

public class RegistryCleanerTests
{
    [Fact]
    public void DeleteAll_ExistingKey_DeletedSuccessfully()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);

        var result = cleaner.DeleteAll([entry]);

        result.Deleted.ShouldBe(1);
        result.Skipped.ShouldBe(0);
        result.Errors.ShouldBe(0);
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge").ShouldBeFalse();
    }

    [Fact]
    public void DeleteAll_NonExistingKey_Skipped()
    {
        var fake = new FakeRegistryOperations();
        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);

        var result = cleaner.DeleteAll([entry]);

        result.Deleted.ShouldBe(0);
        result.Skipped.ShouldBe(1);
        result.Errors.ShouldBe(0);
    }

    [Fact]
    public void DeleteAll_KeyWithSubkeys_RecursivelyDeleted()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey1");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey1\Deep");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey2");

        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);

        var result = cleaner.DeleteAll([entry]);

        result.Deleted.ShouldBe(1);
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge").ShouldBeFalse();
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey1").ShouldBeFalse();
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey1\Deep").ShouldBeFalse();
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge\SubKey2").ShouldBeFalse();
    }

    [Fact]
    public void DeleteAll_ValueOnly_DeletesValueKeepsKey()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            "Microsoft Edge", @"Software\Clients\StartMenuInternet\Microsoft Edge\Capabilities");
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            "Firefox", @"Software\Clients\StartMenuInternet\Firefox\Capabilities");

        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            TargetCategory.StartMenuRegisteredApps, IsValueOnly: true, ValueName: "Microsoft Edge");

        var result = cleaner.DeleteAll([entry]);

        result.Deleted.ShouldBe(1);
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications").ShouldBeTrue();
        fake.GetValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications", "Microsoft Edge").ShouldBeNull();
        fake.GetValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications", "Firefox").ShouldNotBeNull();
    }

    [Fact]
    public void DeleteAll_ValueOnly_NonExistingValue_Skipped()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications");

        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            TargetCategory.StartMenuRegisteredApps, IsValueOnly: true, ValueName: "Microsoft Edge");

        var result = cleaner.DeleteAll([entry]);

        result.Skipped.ShouldBe(1);
    }

    [Fact]
    public void DeleteAll_AccessDenied_CountedAsError()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.SetAccessDenied(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);

        var result = cleaner.DeleteAll([entry]);

        result.Errors.ShouldBe(1);
        result.ErrorMessages.Count.ShouldBe(1);
        result.ErrorMessages[0].ShouldContain("Access denied");
    }

    [Fact]
    public void DeleteAll_MixedResults_CorrectCounters()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeUpdate");
        fake.SetAccessDenied(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeUpdate");

        var cleaner = new RegistryCleaner(fake);
        var entries = new List<RegistryEntry>
        {
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore),
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeUpdate", TargetCategory.EdgeUpdate),
            new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", TargetCategory.Policies),
        };

        var result = cleaner.DeleteAll(entries);

        result.Deleted.ShouldBe(1);
        result.Errors.ShouldBe(1);
        result.Skipped.ShouldBe(1);
    }

    [Fact]
    public void DeleteAll_ContinuesAfterError()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.SetAccessDenied(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeWebView");

        var cleaner = new RegistryCleaner(fake);
        var entries = new List<RegistryEntry>
        {
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore),
            new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeWebView", TargetCategory.EdgeCore),
        };

        var result = cleaner.DeleteAll(entries);

        result.Errors.ShouldBe(1);
        result.Deleted.ShouldBe(1);
        fake.KeyExists(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeWebView").ShouldBeFalse();
    }

    [Fact]
    public void DeleteAll_ProgressCallback_Called()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var cleaner = new RegistryCleaner(fake);
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);

        var callbacks = new List<(RegistryEntry Entry, string Status)>();
        cleaner.DeleteAll([entry], (e, s) => callbacks.Add((e, s)));

        callbacks.Count.ShouldBe(1);
        callbacks[0].Status.ShouldBe("deleted");
    }

    [Fact]
    public void DeleteAll_EmptyList_ZeroCounters()
    {
        var fake = new FakeRegistryOperations();
        var cleaner = new RegistryCleaner(fake);

        var result = cleaner.DeleteAll([]);

        result.Deleted.ShouldBe(0);
        result.Skipped.ShouldBe(0);
        result.Errors.ShouldBe(0);
        result.ErrorMessages.ShouldBeEmpty();
    }
}
