using System;
using System.IO;
using System.Security.Cryptography;

namespace Sensus.Audio;

internal static class RuntimeBaseline
{
    internal const string GameHash = "5F7DB5538B78DC408845A3002907619785AC9F9C6B6059D13DC9A602D9B65731";
    internal static bool Matches()
    {
        using var stream = File.OpenRead(typeof(EnemyAI).Assembly.Location);
        using var sha = SHA256.Create();
        return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "") == GameHash;
    }
}
