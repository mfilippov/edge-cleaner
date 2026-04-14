using Microsoft.Win32;
using Shouldly;

namespace EdgeCleaner.Tests;

public class RegistryScannerTests
{
    [Fact]
    public void ScanAll_EmptyRegistry_ReturnsEmptyList()
    {
        var fake = new FakeRegistryOperations();
        var scanner = new RegistryScanner(fake);

        var results = scanner.ScanAll();

        results.ShouldBeEmpty();
    }

    [Fact]
    public void ScanAll_ExistingStaticKey_ReturnsIt()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e =>
            e.Hive == RegistryHive.LocalMachine &&
            e.Path == @"SOFTWARE\Microsoft\Edge" &&
            e.Category == TargetCategory.EdgeCore);
    }

    [Fact]
    public void ScanAll_NonExistingKey_NotIncluded()
    {
        var fake = new FakeRegistryOperations();
        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldNotContain(e => e.Path == @"SOFTWARE\Microsoft\Edge");
    }

    [Fact]
    public void ScanAll_MultipleCategories_AllFound()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeUpdate");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.Count.ShouldBe(3);
        results.Select(e => e.Category).ShouldContain(TargetCategory.EdgeCore);
        results.Select(e => e.Category).ShouldContain(TargetCategory.EdgeUpdate);
        results.Select(e => e.Category).ShouldContain(TargetCategory.Policies);
    }

    [Fact]
    public void ScanAll_ValueOnlyEntry_DetectedWhenValueExists()
    {
        var fake = new FakeRegistryOperations();
        fake.AddValue(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications",
            "Microsoft Edge", @"Software\Clients\StartMenuInternet\Microsoft Edge\Capabilities");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e =>
            e.IsValueOnly &&
            e.ValueName == "Microsoft Edge" &&
            e.Category == TargetCategory.StartMenuRegisteredApps);
    }

    [Fact]
    public void ScanAll_ValueOnlyEntry_NotDetectedWhenValueMissing()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldNotContain(e => e.ValueName == "Microsoft Edge");
    }

    [Fact]
    public void ScanAll_DynamicTarget_FindsMatchingSubkeys()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Classes\MicrosoftEdgeUpdate.CoreClass");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Classes\MicrosoftEdgeUpdate.CoreClass.1");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Classes\SomethingElse");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path == @"SOFTWARE\Classes\MicrosoftEdgeUpdate.CoreClass");
        results.ShouldContain(e => e.Path == @"SOFTWARE\Classes\MicrosoftEdgeUpdate.CoreClass.1");
        results.ShouldNotContain(e => e.Path == @"SOFTWARE\Classes\SomethingElse");
    }

    [Fact]
    public void ScanAll_ClsidWithEdgeDll_Found()
    {
        var fake = new FakeRegistryOperations();
        var clsidGuid = "{AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE}";
        fake.AddKey(RegistryHive.LocalMachine, @$"SOFTWARE\Classes\CLSID\{clsidGuid}");
        fake.AddValue(RegistryHive.LocalMachine,
            @$"SOFTWARE\Classes\CLSID\{clsidGuid}\InProcServer32",
            "", @"C:\Program Files (x86)\Microsoft\EdgeUpdate\1.3.209.9\psmachine_64.dll");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e =>
            e.Path == @$"SOFTWARE\Classes\CLSID\{clsidGuid}" &&
            e.Category == TargetCategory.ComObjects);
    }

    [Fact]
    public void ScanAll_ClsidWithNonEdgeDll_NotIncluded()
    {
        var fake = new FakeRegistryOperations();
        var clsidGuid = "{11111111-2222-3333-4444-555555555555}";
        fake.AddKey(RegistryHive.LocalMachine, @$"SOFTWARE\Classes\CLSID\{clsidGuid}");
        fake.AddValue(RegistryHive.LocalMachine,
            @$"SOFTWARE\Classes\CLSID\{clsidGuid}\InProcServer32",
            "", @"C:\Windows\System32\someother.dll");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldNotContain(e => e.Path.Contains(clsidGuid));
    }

    [Fact]
    public void ScanAll_ScheduledTask_FindsEdgeTasks()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\MicrosoftEdgeUpdateTaskMachineCore");
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\SomeOtherTask");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("MicrosoftEdgeUpdateTaskMachineCore"));
        results.ShouldNotContain(e => e.Path.Contains("SomeOtherTask"));
    }

    [Fact]
    public void ScanAll_ScheduledTask_DoesNotMatchBareEdge()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\SomeEdgeTask");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldNotContain(e => e.Path.Contains("SomeEdgeTask"));
    }

    [Fact]
    public void ScanAll_TaskCacheSibling_FollowsTreeToTasks()
    {
        var fake = new FakeRegistryOperations();
        var treePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\MicrosoftEdgeUpdateTaskMachineCore";
        var guid = "{AAAAAAAA-1111-2222-3333-444444444444}";
        var tasksPath = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{guid}";

        fake.AddKey(RegistryHive.LocalMachine, treePath);
        fake.AddValue(RegistryHive.LocalMachine, treePath, "Id", guid);
        fake.AddKey(RegistryHive.LocalMachine, tasksPath);

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path == treePath);
        results.ShouldContain(e => e.Path == tasksPath && e.Category == TargetCategory.ScheduledTasks);
    }

    [Fact]
    public void ScanAll_TaskCacheSibling_NoIdValue_SkipsTasksKey()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\MicrosoftEdgeUpdateTaskMachineUA");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldNotContain(e => e.Path.Contains(@"TaskCache\Tasks"));
    }

    [Fact]
    public void FakeRegistry_GetSubKeyNames_WorksForDeepPaths()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.CurrentUser, @"A\B\C\child1");
        fake.AddKey(RegistryHive.CurrentUser, @"A\B\C\child2");

        var subKeys = fake.GetSubKeyNames(RegistryHive.CurrentUser, @"A\B\C");
        subKeys.Length.ShouldBe(2);
        subKeys.ShouldContain("child1");
        subKeys.ShouldContain("child2");
    }

    [Fact]
    public void ScanAll_CapabilityAccessManager_FindsMsedgeEntries()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged\C:#Program Files (x86)#Microsoft#Edge#Application#msedge.exe");
        fake.AddKey(RegistryHive.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged\C:#Program Files#SomeOtherApp#app.exe");

        // Verify fake works correctly for this path
        var subKeys = fake.GetSubKeyNames(RegistryHive.CurrentUser,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged");
        subKeys.Length.ShouldBe(2);

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("msedge.exe"));
        results.ShouldNotContain(e => e.Path.Contains("SomeOtherApp"));
    }

    [Fact]
    public void ScanAll_SidBasedTarget_FindsEdgePackages()
    {
        var fake = new FakeRegistryOperations();
        var basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore";
        fake.AddKey(RegistryHive.LocalMachine, $@"{basePath}\S-1-5-21-123\Microsoft.Edge.Stable_130.0_x64__8wekyb3d8bbwe");
        fake.AddKey(RegistryHive.LocalMachine, $@"{basePath}\S-1-5-21-123\SomeOtherPackage_1.0_x64__aaaa");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("Microsoft.Edge.Stable"));
        results.ShouldNotContain(e => e.Path.Contains("SomeOtherPackage"));
    }

    [Fact]
    public void ScanAll_Wow6432NodeClsid_Found()
    {
        var fake = new FakeRegistryOperations();
        var clsidGuid = "{BBBBBBBB-1111-2222-3333-444444444444}";
        fake.AddKey(RegistryHive.LocalMachine, @$"SOFTWARE\WOW6432Node\Classes\CLSID\{clsidGuid}");
        fake.AddValue(RegistryHive.LocalMachine,
            @$"SOFTWARE\WOW6432Node\Classes\CLSID\{clsidGuid}\InProcServer32",
            "", @"C:\Program Files (x86)\Microsoft\EdgeUpdate\1.3.209.9\psmachine.dll");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e =>
            e.Path == @$"SOFTWARE\WOW6432Node\Classes\CLSID\{clsidGuid}" &&
            e.Category == TargetCategory.ComObjects);
    }

    [Fact]
    public void ScanAll_LegacyEdgeAppxWithUnderscore_Found()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages\Microsoft.MicrosoftEdge_44.19041.1266.0_neutral__8wekyb3d8bbwe");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("Microsoft.MicrosoftEdge_44"));
    }

    [Fact]
    public void ScanAll_NoDuplicatesInResults()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        var grouped = results.GroupBy(e => (e.Hive, e.Path, e.ValueName));
        foreach (var g in grouped)
        {
            g.Count().ShouldBe(1, $"Duplicate entry for {g.Key}");
        }
    }

    [Fact]
    public void ScanAll_CorrectCategoryAssignment()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdate");
        fake.AddKey(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("edgeupdate") && e.Category == TargetCategory.Services);
        results.ShouldContain(e => e.Path.Contains("msedge.exe") && e.Category == TargetCategory.AppPaths);
    }

    [Fact]
    public void ScanAll_CloudStore_FindsDeepNestedEdgeEntries()
    {
        var fake = new FakeRegistryOperations();
        var basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Cloud";
        fake.AddKey(RegistryHive.CurrentUser,
            $@"{basePath}\{{GUID}}$windows.data.apps.appmetadata\windows.data.apps.appmetadata$microsoft edge");
        fake.AddKey(RegistryHive.CurrentUser,
            $@"{basePath}\{{GUID}}$windows.data.apps.appmetadata\windows.data.apps.appmetadata$notepad");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("microsoft edge") && e.Category == TargetCategory.UserAppTraces);
        results.ShouldNotContain(e => e.Path.Contains("notepad"));
    }

    [Fact]
    public void ScanAll_DeletedUpgrade_FindsEdgePackages()
    {
        var fake = new FakeRegistryOperations();
        var basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\Upgrade";
        fake.AddKey(RegistryHive.LocalMachine,
            $@"{basePath}\S-1-5-21-123\Microsoft.Edge.Stable_1.0_x64__8wekyb3d8bbwe");
        fake.AddKey(RegistryHive.LocalMachine,
            $@"{basePath}\S-1-5-21-123\SomeOtherApp_1.0_x64__aaaa");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("Microsoft.Edge.Stable"));
        results.ShouldNotContain(e => e.Path.Contains("SomeOtherApp"));
    }

    [Fact]
    public void ScanAll_GameAssist_ExcludedFromResults()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\Microsoft.Edge.Stable_130.0_x64__8wekyb3d8bbwe");
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\Microsoft.Edge.GameAssist_1.74_x64__8wekyb3d8bbwe");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("Microsoft.Edge.Stable"));
        results.ShouldNotContain(e => e.Path.Contains("Microsoft.Edge.GameAssist"));
    }

    [Fact]
    public void ScanAll_DevToolsClient_Found()
    {
        var fake = new FakeRegistryOperations();
        fake.AddKey(RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications\Microsoft.MicrosoftEdgeDevToolsClient_1.0_x64__8wekyb3d8bbwe");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("Microsoft.MicrosoftEdgeDevToolsClient"));
    }

    [Fact]
    public void ScanAll_SidBasedTarget_FindsLegacyUnderscorePackages()
    {
        var fake = new FakeRegistryOperations();
        var basePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore";
        fake.AddKey(RegistryHive.LocalMachine,
            $@"{basePath}\S-1-5-21-999\Microsoft.MicrosoftEdge_44.19041_neutral__8wekyb3d8bbwe");

        var scanner = new RegistryScanner(fake);
        var results = scanner.ScanAll();

        results.ShouldContain(e => e.Path.Contains("Microsoft.MicrosoftEdge_44"));
    }

    [Fact]
    public void RegistryEntry_FullPath_FormatsCorrectly()
    {
        var entry = new RegistryEntry(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        entry.FullPath.ShouldBe(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Edge");
    }

    [Fact]
    public void RegistryEntry_FullPath_CurrentUser_FormatsCorrectly()
    {
        var entry = new RegistryEntry(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore);
        entry.FullPath.ShouldBe(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Edge");
    }
}
