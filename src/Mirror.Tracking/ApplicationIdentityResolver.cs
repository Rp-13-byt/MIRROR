using System.Collections.Concurrent;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Tracking;

public class ApplicationIdentityResolver : IApplicationIdentityResolver
{
    private static readonly Dictionary<string, (string AppKey, string DisplayName, string Category)> KnownApps =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Browsers
            ["msedge"] = ("browser.edge", "Microsoft Edge", "Browser"),
            ["chrome"] = ("browser.chrome", "Google Chrome", "Browser"),
            ["firefox"] = ("browser.firefox", "Mozilla Firefox", "Browser"),
            ["brave"] = ("browser.brave", "Brave Browser", "Browser"),
            ["opera"] = ("browser.opera", "Opera", "Browser"),

            // Development
            ["code"] = ("dev.vscode", "Visual Studio Code", "Development"),
            ["devenv"] = ("dev.vs", "Visual Studio", "Development"),
            ["windowsterminal"] = ("dev.terminal", "Windows Terminal", "Development"),
            ["wt"] = ("dev.terminal", "Windows Terminal", "Development"),
            ["cmd"] = ("dev.cmd", "Command Prompt", "Development"),
            ["powershell"] = ("dev.powershell", "Windows PowerShell", "Development"),
            ["pwsh"] = ("dev.pwsh", "PowerShell 7", "Development"),
            ["idea64"] = ("dev.intellij", "IntelliJ IDEA", "Development"),
            ["pycharm64"] = ("dev.pycharm", "PyCharm", "Development"),
            ["rider64"] = ("dev.rider", "JetBrains Rider", "Development"),
            ["git-bash"] = ("dev.gitbash", "Git Bash", "Development"),

            // Communication
            ["discord"] = ("comm.discord", "Discord", "Communication"),
            ["slack"] = ("comm.slack", "Slack", "Communication"),
            ["teams"] = ("comm.teams", "Microsoft Teams", "Communication"),
            ["ms-teams"] = ("comm.teams", "Microsoft Teams", "Communication"),
            ["telegram"] = ("comm.telegram", "Telegram", "Communication"),
            ["whatsapp"] = ("comm.whatsapp", "WhatsApp", "Communication"),
            ["outlook"] = ("comm.outlook", "Microsoft Outlook", "Communication"),

            // Media
            ["spotify"] = ("media.spotify", "Spotify", "Media"),
            ["vlc"] = ("media.vlc", "VLC Media Player", "Media"),
            ["wmplayer"] = ("media.wmp", "Windows Media Player", "Media"),
            ["obs64"] = ("media.obs", "OBS Studio", "Media"),

            // Productivity
            ["winword"] = ("prod.word", "Microsoft Word", "Productivity"),
            ["excel"] = ("prod.excel", "Microsoft Excel", "Productivity"),
            ["powerpnt"] = ("prod.powerpoint", "Microsoft PowerPoint", "Productivity"),
            ["onenote"] = ("prod.onenote", "Microsoft OneNote", "Productivity"),
            ["notion"] = ("prod.notion", "Notion", "Productivity"),
            ["obsidian"] = ("prod.obsidian", "Obsidian", "Productivity"),
            ["acrobat"] = ("prod.acrobat", "Adobe Acrobat", "Productivity"),

            // System
            ["explorer"] = ("sys.explorer", "File Explorer", "System"),
            ["taskmgr"] = ("sys.taskmgr", "Task Manager", "System"),
            ["systemsettings"] = ("sys.settings", "Windows Settings", "System"),
            ["applicationframehost"] = ("sys.uwp", "Windows App Host", "System")
        };

    private readonly ConcurrentDictionary<string, string> _categoryOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, byte> _excludedApps = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, AppIdentity> _cache = new(StringComparer.OrdinalIgnoreCase);

    public ApplicationIdentityResolver(
        IReadOnlyDictionary<string, string>? initialOverrides = null,
        IEnumerable<string>? initialExclusions = null)
    {
        if (initialOverrides != null)
        {
            foreach (var kvp in initialOverrides)
            {
                _categoryOverrides[kvp.Key] = kvp.Value;
            }
        }

        if (initialExclusions != null)
        {
            foreach (var item in initialExclusions)
            {
                _excludedApps[item] = 0;
            }
        }
    }

    public AppIdentity ResolveIdentity(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return new AppIdentity("unknown", "Unknown Application", "Other", false);
        }

        string cleanName = processName.Trim().ToLowerInvariant();
        string baseName = cleanName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? cleanName[..^4]
            : cleanName;

        if (_cache.TryGetValue(cleanName, out var cached))
        {
            // Check if exclusion or category override changed
            bool isExcluded = _excludedApps.ContainsKey(cached.AppKey) || _excludedApps.ContainsKey(cleanName) || _excludedApps.ContainsKey(baseName);
            string category = _categoryOverrides.TryGetValue(cached.AppKey, out var cat) ? cat : cached.Category;
            if (isExcluded != cached.IsExcluded || category != cached.Category)
            {
                var updated = cached with { Category = category, IsExcluded = isExcluded };
                _cache[cleanName] = updated;
                return updated;
            }
            return cached;
        }

        string appKey;
        string displayName;
        string defaultCategory;

        if (KnownApps.TryGetValue(baseName, out var known) || KnownApps.TryGetValue(cleanName, out known))
        {
            appKey = known.AppKey;
            displayName = known.DisplayName;
            defaultCategory = known.Category;
        }
        else
        {
            appKey = $"app.{cleanName}";
            string nameNoExt = baseName.Length > 0 ? baseName : cleanName;
            displayName = char.ToUpperInvariant(nameNoExt[0]) + (nameNoExt.Length > 1 ? nameNoExt[1..] : "");
            defaultCategory = "Other";
        }

        string finalCategory = _categoryOverrides.TryGetValue(appKey, out var userCat) ? userCat : defaultCategory;
        bool excluded = _excludedApps.ContainsKey(appKey) || _excludedApps.ContainsKey(cleanName) || _excludedApps.ContainsKey(baseName);

        var identity = new AppIdentity(appKey, displayName, finalCategory, excluded);
        _cache[cleanName] = identity;
        return identity;
    }

    public void SetCategoryOverride(string appKey, string category)
    {
        _categoryOverrides[appKey] = category;
        _cache.Clear();
    }

    public void SetExcluded(string appKey, bool excluded)
    {
        if (excluded)
        {
            _excludedApps[appKey] = 0;
        }
        else
        {
            _excludedApps.TryRemove(appKey, out _);
        }
        _cache.Clear();
    }

    public IReadOnlyDictionary<string, string> GetCategoryOverrides()
    {
        return _categoryOverrides.ToDictionary(k => k.Key, v => v.Value);
    }

    public IReadOnlySet<string> GetExcludedApps()
    {
        return _excludedApps.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
