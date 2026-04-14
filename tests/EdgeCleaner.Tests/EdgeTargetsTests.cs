using Microsoft.Win32;
using Shouldly;

namespace EdgeCleaner.Tests;

public class EdgeTargetsTests
{
    [Fact]
    public void StaticKeys_ShouldNotBeEmpty()
    {
        EdgeTargets.StaticKeys.ShouldNotBeEmpty();
    }

    [Fact]
    public void DynamicTargets_ShouldNotBeEmpty()
    {
        EdgeTargets.DynamicTargets.ShouldNotBeEmpty();
    }

    [Fact]
    public void AllCategories_ShouldBeRepresented()
    {
        var staticCategories = EdgeTargets.StaticKeys.Select(k => k.Category).Distinct();
        var dynamicCategories = EdgeTargets.DynamicTargets.Select(k => k.Category).Distinct();
        var allCategories = staticCategories.Union(dynamicCategories).ToHashSet();

        allCategories.ShouldContain(TargetCategory.EdgeCore);
        allCategories.ShouldContain(TargetCategory.EdgeUpdate);
        allCategories.ShouldContain(TargetCategory.Policies);
        allCategories.ShouldContain(TargetCategory.Uninstall);
        allCategories.ShouldContain(TargetCategory.FileAssociations);
        allCategories.ShouldContain(TargetCategory.ComObjects);
        allCategories.ShouldContain(TargetCategory.Services);
        allCategories.ShouldContain(TargetCategory.AppPaths);
    }

    [Fact]
    public void StaticKeys_ShouldHaveNoDuplicates()
    {
        var keys = EdgeTargets.StaticKeys
            .Where(k => !k.IsValueOnly)
            .Select(k => (k.Hive, k.Path))
            .ToList();

        keys.Count.ShouldBe(keys.Distinct().Count());
    }

    [Fact]
    public void StaticKeys_ShouldOnlyUseValidHives()
    {
        var validHives = new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser, RegistryHive.ClassesRoot };

        foreach (var key in EdgeTargets.StaticKeys)
        {
            validHives.ShouldContain(key.Hive, $"Key {key.Path} uses unsupported hive {key.Hive}");
        }
    }

    [Fact]
    public void StaticKeys_PathsShouldNotStartWithBackslash()
    {
        foreach (var key in EdgeTargets.StaticKeys)
        {
            key.Path.StartsWith(@"\").ShouldBeFalse($"Path {key.Path} should not start with backslash");
        }
    }

    [Fact]
    public void ValueOnlyEntries_ShouldHaveValueName()
    {
        var valueOnly = EdgeTargets.StaticKeys.Where(k => k.IsValueOnly);

        foreach (var entry in valueOnly)
        {
            entry.ValueName.ShouldNotBeNullOrEmpty($"ValueOnly entry at {entry.Path} must have a ValueName");
        }
    }

    [Fact]
    public void RegisteredApplications_ShouldBeValueOnly()
    {
        var regApps = EdgeTargets.StaticKeys
            .Where(k => k.Path.Contains("RegisteredApplications"))
            .ToList();

        regApps.ShouldNotBeEmpty();
        regApps.ShouldAllBe(k => k.IsValueOnly);
        regApps.ShouldAllBe(k => k.ValueName == "Microsoft Edge");
    }

    [Fact]
    public void DynamicTargets_ShouldHaveWow6432NodeClsidTargets()
    {
        var wowClsid = EdgeTargets.DynamicTargets
            .Where(t => t.ParentPath.Contains(@"WOW6432Node\Classes\CLSID"))
            .ToList();

        wowClsid.ShouldNotBeEmpty("WOW6432Node CLSID targets should exist for 32-bit COM cleanup");
    }

    [Fact]
    public void DynamicTargets_AppxPackages_ShouldHaveUnderscorePatterns()
    {
        var appxTargets = EdgeTargets.DynamicTargets
            .Where(t => t.Category == TargetCategory.AppxPackagesHklm || t.Category == TargetCategory.AppxPackagesHkcu)
            .ToList();

        var underscoreTargets = appxTargets.Where(t => t.Pattern == "Microsoft.MicrosoftEdge_").ToList();
        underscoreTargets.ShouldNotBeEmpty("Should have underscore patterns for legacy Edge packages");
    }

    [Fact]
    public void DynamicTargets_ScheduledTasks_ShouldNotMatchBareEdge()
    {
        var taskTargets = EdgeTargets.DynamicTargets
            .Where(t => t.Category == TargetCategory.ScheduledTasks)
            .ToList();

        foreach (var target in taskTargets)
        {
            target.Pattern.ShouldNotBe("Edge", "Scheduled task pattern should not be bare 'Edge' — too broad for a destructive tool");
        }
    }

    [Fact]
    public void DynamicTargets_PathsShouldNotStartWithBackslash()
    {
        foreach (var target in EdgeTargets.DynamicTargets)
        {
            target.ParentPath.StartsWith(@"\").ShouldBeFalse($"ParentPath {target.ParentPath} should not start with backslash");
        }
    }

    [Fact]
    public void DynamicTargets_PatternsShouldNotBeEmpty()
    {
        foreach (var target in EdgeTargets.DynamicTargets)
        {
            target.Pattern.ShouldNotBeNullOrEmpty($"Pattern for {target.ParentPath} should not be empty");
        }
    }

    [Fact]
    public void StaticKeys_ShouldNotContainSystemPaths()
    {
        var dangerousPaths = new[]
        {
            "Component Based Servicing",
            "SideBySide\\Winners",
            "StateRepository\\Cache",
            "Windows.UI.Input.EdgeGesture",
            "PolicyManager\\default"
        };

        foreach (var key in EdgeTargets.StaticKeys)
        {
            foreach (var danger in dangerousPaths)
            {
                key.Path.Contains(danger).ShouldBeFalse($"Static key {key.Path} matches dangerous system path pattern '{danger}'");
            }
        }
    }
}
