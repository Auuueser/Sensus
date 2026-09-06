using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Sensus.Audio;

// Read only opcode boundaries; do not mistake operand bytes for call instructions.
internal static class AudioCallTokens
{
    private static readonly Dictionary<short, OpCode> Codes = CreateCodes();
    private static Dictionary<short, OpCode> CreateCodes()
    {
        var result = new Dictionary<short, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (field.FieldType == typeof(OpCode)) { var code = (OpCode)field.GetValue(null)!; result[code.Value] = code; }
        return result;
    }
    internal static IEnumerable<int> Read(byte[] il)
    {
        int at = 0;
        while (at < il.Length)
        {
            short value = il[at++];
            if (value == 0xfe)
            {
                if (at == il.Length) throw new InvalidOperationException("Truncated IL opcode.");
                value = (short)(0xfe00 | il[at++]);
            }
            if (!Codes.TryGetValue(value, out var code)) throw new InvalidOperationException("Unknown IL opcode.");
            int size;
            switch (code.OperandType)
            {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch:
                    if (at + 4 > il.Length) throw new InvalidOperationException("Truncated switch.");
                    int count = BitConverter.ToInt32(il, at);
                    if (count < 0 || count > (il.Length - at - 4) / 4) throw new InvalidOperationException("Invalid switch length.");
                    size = 4 + count * 4; break;
                default: size = 4; break;
            }
            if (size > il.Length - at) throw new InvalidOperationException("Truncated IL operand.");
            if (code == OpCodes.Call || code == OpCodes.Callvirt) yield return BitConverter.ToInt32(il, at);
            at += size;
        }
    }
}
