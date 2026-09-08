using System;
using System.IO;
using System.Security.Cryptography;

namespace Sensus.Audio;

internal static class RuntimeBaseline
{
    internal const string GameHash = "5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731";
    internal static bool Matches(string managedPath)
    {
        // Preloaders load patched assemblies from memory or a dump directory.
        // Audit the original game; PlaybackHooks resolves the live method IL.
        if (string.IsNullOrWhiteSpace(managedPath) || !Path.IsPathRooted(managedPath)) return false;
        var path = Path.Combine(managedPath, "Assembly-CSharp.dll");
        if (!File.Exists(path)) return false;
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") == GameHash;
    }
}
