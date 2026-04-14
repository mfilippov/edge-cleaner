using Microsoft.Win32;

namespace EdgeCleaner;

public enum TargetCategory
{
    EdgeCore,
    EdgeUpdate,
    Policies,
    Uninstall,
    FileAssociations,
    ComObjects,
    BrowserHelperObjects,
    IeIntegration,
    StartMenuRegisteredApps,
    Services,
    ScheduledTasks,
    AppPaths,
    UserAppTraces,
    AppxPackagesHkcu,
    AppxPackagesHklm,
    Misc
}

public record TargetKey(
    RegistryHive Hive,
    string Path,
    TargetCategory Category,
    bool IsValueOnly = false,
    string? ValueName = null);

public record DynamicTarget(
    RegistryHive Hive,
    string ParentPath,
    string Pattern,
    TargetCategory Category);

public static class EdgeTargets
{
    // Non-browser packages that share the Microsoft.Edge. prefix
    public static readonly string[] ExcludedPackagePatterns = ["Microsoft.Edge.GameAssist"];

    public static readonly List<TargetKey> StaticKeys =
    [
        // Category 1: Edge Core
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Edge", TargetCategory.EdgeCore),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Edge", TargetCategory.EdgeCore),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Edge", TargetCategory.EdgeCore),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeWebView", TargetCategory.EdgeCore),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\EdgeWebView", TargetCategory.EdgeCore),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\MicrosoftEdge", TargetCategory.EdgeCore),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\MicrosoftEdge", TargetCategory.EdgeCore),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\MicrosoftEdge", TargetCategory.EdgeCore),

        // Category 2: Edge Update Service
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\EdgeUpdate", TargetCategory.EdgeUpdate),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\EdgeUpdate", TargetCategory.EdgeUpdate),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate", TargetCategory.EdgeUpdate),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\EdgeUpdateDev", TargetCategory.EdgeUpdate),

        // Category 3: Policies
        new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Edge", TargetCategory.Policies),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Policies\Microsoft\Edge", TargetCategory.Policies),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\EdgeUpdate", TargetCategory.Policies),

        // Category 4: Uninstall Entries
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge", TargetCategory.Uninstall),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge Update", TargetCategory.Uninstall),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView", TargetCategory.Uninstall),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge", TargetCategory.Uninstall),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge Update", TargetCategory.Uninstall),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView", TargetCategory.Uninstall),

        // Category 5: File/Protocol Associations
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\MSEdgeHTM", TargetCategory.FileAssociations),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\MSEdgePDF", TargetCategory.FileAssociations),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\MSEdgeMHT", TargetCategory.FileAssociations),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\microsoft-edge", TargetCategory.FileAssociations),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\microsoft-edge-holographic", TargetCategory.FileAssociations),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ie_to_edge_bho.IEToEdgeBHO", TargetCategory.FileAssociations),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\ie_to_edge_bho.IEToEdgeBHO.1", TargetCategory.FileAssociations),

        // Category 6: COM Objects (AppID)
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\AppID\ie_to_edge_bho.dll", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\AppID\MicrosoftEdgeUpdate.exe", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\WOW6432Node\AppID\ie_to_edge_bho.dll", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\WOW6432Node\AppID\MicrosoftEdgeUpdate.exe", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\AppID\{1FCBE96C-1697-43AF-9140-2897C7C69767}", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\AppID\{31575964-95F7-414B-85E4-0E9A93699E13}", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\AppID\{A6B716CB-028B-404D-B72C-50E153DD68DA}", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\AppID\{CECDDD22-2E72-4832-9606-A9B0E5E344B2}", TargetCategory.ComObjects),

        // Category 7: Browser Helper Objects
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}", TargetCategory.BrowserHelperObjects),

        // Category 8: IE Integration
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\AdvancedOptions\BROWSE\HIDENEWEDGEBUTTON", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\AdvancedOptions\BROWSE\HIDEOPENWITHEDGE_CONTEXTMENU", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\EdgeDebugActivation", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\EdgeIntegration", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\ProtocolExecute\microsoft-edge", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\AdvancedOptions\BROWSE\HIDENEWEDGEBUTTON", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\AdvancedOptions\BROWSE\HIDEOPENWITHEDGE_CONTEXTMENU", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\EdgeDebugActivation", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\EdgeIntegration", TargetCategory.IeIntegration),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Internet Explorer\ProtocolExecute\microsoft-edge", TargetCategory.IeIntegration),

        // Category 9: Start Menu / Registered Apps
        new(RegistryHive.LocalMachine, @"SOFTWARE\Clients\StartMenuInternet\Microsoft Edge", TargetCategory.StartMenuRegisteredApps),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet\Microsoft Edge", TargetCategory.StartMenuRegisteredApps),
        new(RegistryHive.LocalMachine, @"SOFTWARE\RegisteredApplications", TargetCategory.StartMenuRegisteredApps, IsValueOnly: true, ValueName: "Microsoft Edge"),

        // Category 10: Services
        new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdate", TargetCategory.Services),
        new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\edgeupdatem", TargetCategory.Services),
        new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\MicrosoftEdgeElevationService", TargetCategory.Services),
        new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\EventLog\Application\Edge", TargetCategory.Services),
        new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\EventLog\Application\edgeupdate", TargetCategory.Services),
        new(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\EventLog\Application\edgeupdatem", TargetCategory.Services),

        // Category 12: App Paths
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe", TargetCategory.AppPaths),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe", TargetCategory.AppPaths),

        // Category 13: HKCU App Traces (static ones)
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Start\TileProperties\W~MSEdge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\microsoft-edge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\microsoft-edge-holographic", TargetCategory.UserAppTraces),

        // Category 16: Misc
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\MediaPlayer\ShimInclusionList\msedge.exe", TargetCategory.Misc),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\RADAR\HeapLeakDetection\DiagnosedApplications\msedge.exe", TargetCategory.Misc),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameInput\Exe\msedgewebview2.exe", TargetCategory.Misc),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\GameInput\Exe\msedgewebview2.exe", TargetCategory.Misc),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\MicrosoftEdgeUpdate.exe", TargetCategory.Misc),
    ];

    public static readonly List<DynamicTarget> DynamicTargets =
    [
        // Category 6: COM MicrosoftEdgeUpdate classes
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes", "MicrosoftEdgeUpdate.", TargetCategory.ComObjects),

        // Category 6: CLSID with Edge DLL paths (searched by value content)
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\CLSID", @"\Microsoft\Edge", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\CLSID", @"\Microsoft\EdgeUpdate", TargetCategory.ComObjects),

        // Category 6: WOW6432Node COM (32-bit registrations on 64-bit Windows)
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Classes", "MicrosoftEdgeUpdate.", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Classes\CLSID", @"\Microsoft\Edge", TargetCategory.ComObjects),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Classes\CLSID", @"\Microsoft\EdgeUpdate", TargetCategory.ComObjects),

        // Category 11: Scheduled Tasks
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree", "MicrosoftEdge", TargetCategory.ScheduledTasks),

        // Category 13: HKCU App Traces (dynamic)
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost\IndexedDB", "Microsoft.Edge.", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost\IndexedDB", "Microsoft.MicrosoftEdge.", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "Microsoft.Edge.", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "Microsoft.MicrosoftEdge.", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\Backup", "Microsoft.Edge.", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\PushNotifications\Backup", "Microsoft.MicrosoftEdge.", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\HostActivityManager\CommitHistory", "Microsoft.MicrosoftEdge.", TargetCategory.UserAppTraces),

        // Category 13: CapabilityAccessManager consent keys for msedge.exe
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\graphicsCaptureWithoutBorder\NonPackaged", "msedge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location\NonPackaged", "msedge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\microphone\NonPackaged", "msedge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\passkeys\NonPackaged", "msedge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\passkeysEnumeration\NonPackaged", "msedge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam\NonPackaged", "msedge", TargetCategory.UserAppTraces),

        // Category 14: AppX Package Traces (HKCU)
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Extensions\ContractId\Windows.BackgroundTasks\PackageId", "Microsoft.Edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Extensions\ContractId\Windows.GameBarUIExtension\PackageId", "Microsoft.Edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Extensions\ContractId\Windows.Launch\PackageId", "Microsoft.Edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\MrtCache", "Edge", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\MrtCache", "MicrosoftEdge", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage", "microsoft.edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppContainer\Storage", "microsoft.microsoftedge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PolicyCache", "Microsoft.Edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PolicyCache", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PolicyCache", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages", "Microsoft.Edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages", "Microsoft.MicrosoftEdgeDevToolsClient", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", "Microsoft.Edge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHkcu),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHkcu),

        // Category 15: AppX Package Traces (HKLM)
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages", "Microsoft.MicrosoftEdgeDevToolsClient", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\SystemAppData", "Microsoft.MicrosoftEdgeDevToolsClient", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Applications", "Microsoft.MicrosoftEdgeDevToolsClient", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Config", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Config", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\InboxApplications", "Microsoft.MicrosoftEdge", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.MicrosoftEdgeDevToolsClient", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\SecurityManager\CapAuthz\ApplicationsEx", "Microsoft.MicrosoftEdgeDevToolsClient", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Config", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Config", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\StagingInfo", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
    ];

    // Paths in AppxAllUserStore that have SID-based subkeys containing Edge entries
    public static readonly List<DynamicTarget> SidBasedTargets =
    [
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\Upgrade", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\Upgrade", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\Upgrade", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\EndOfLife", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\EndOfLife", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\AppxAllUserStore\Deleted\EndOfLife", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\PackageState", "Microsoft.Edge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\PackageState", "Microsoft.MicrosoftEdge.", TargetCategory.AppxPackagesHklm),
        new(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Appx\PackageState", "Microsoft.MicrosoftEdge_", TargetCategory.AppxPackagesHklm),
    ];

    // Targets where Edge entries are nested 2+ levels deep (e.g. CloudStore buckets)
    public static readonly List<DynamicTarget> DeepDynamicTargets =
    [
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Cloud", "microsoft edge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Cloud", "msedge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current", "microsoft edge", TargetCategory.UserAppTraces),
        new(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current", "msedge", TargetCategory.UserAppTraces),
    ];
}
