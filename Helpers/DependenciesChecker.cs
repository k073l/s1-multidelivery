using ScheduleOne.UI;
using Semver;

namespace MultiDelivery.Helpers;

public static class DependenciesChecker
{
    private static readonly Logger Logger = new(nameof(DependenciesChecker));
    private static readonly List<Dependency> Dependencies =
    [
        new()
        {
            Name = "S1API (Forked)",
            AssemblyName = "S1API",
            MinVersion = new SemVersion(3, 0, 1),
            IsRequired = true
        },
        new()
        {
            Name = "SteamNetworkLib",
            AssemblyName = "SteamNetworkLib",
            MinVersion = new SemVersion(1, 2, 1),
            IsRequired = false
        }
    ];

    private static bool IsPresent(Dependency dependency) {
        var foundInDomain = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == dependency.AssemblyName);
        if (foundInDomain == null) return false;
        var foundInMelons = MultiDelivery.RegisteredMelons.FirstOrDefault(m => m.MelonAssembly.Assembly == foundInDomain);
        if (foundInMelons == null)
        {
            Logger.Debug($"{foundInDomain.GetName().Name} wasn't found in RegisteredMelons, cannot verify version. Assuming present.");
            return true;
        }

        var version = foundInMelons.Info.SemanticVersion;
        if (version == null)
        {
            Logger.Debug($"{foundInDomain.GetName().Name}'s version {version} is not SemVer. Assuming present.");
            return true;
        }
        
        return version >= dependency.MinVersion;
    }

    private static List<Dependency> GetMissing() => Dependencies.Where(dependency => !IsPresent(dependency)).ToList();

    public static void PrintMissing()
    {
        var missing = GetMissing();
        if (!missing.Any()) return;
        foreach (var dependency in missing)
        {
            if (dependency.IsRequired)
                Logger.Error($"Critical:\nRequired dependency {dependency.Name} is missing or outdated (min version {dependency.MinVersion}). Mod will not function correctly without it.");
            else
                Logger.Warn($"Optional dependency {dependency.Name} is missing or outdated (min version {dependency.MinVersion}).");
        }
    }
}

public record Dependency
{
    public string Name { get; set; }
    public string AssemblyName { get; set; }
    public SemVersion MinVersion { get; set; }
    public bool IsRequired { get; set; }
}