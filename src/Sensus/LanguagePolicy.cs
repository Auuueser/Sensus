using System;
using System.Collections.Generic;

namespace Sensus;

internal static class LanguagePolicy
{
    internal const string ChinesePluginGuid = "Aueser.LCChineseProject";
    internal const string LegacyChinesePluginGuid = "cn.codex.v81testchn";

    // Version is deliberately not an input: translation releases retain their GUID.
    internal static bool UseChinese(string preference, IEnumerable<string> loadedPluginGuids)
    {
        if (string.Equals(preference, "Chinese", StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(preference, "English", StringComparison.OrdinalIgnoreCase)) return false;
        foreach (string guid in loadedPluginGuids)
            if (string.Equals(guid, ChinesePluginGuid, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(guid, LegacyChinesePluginGuid, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
