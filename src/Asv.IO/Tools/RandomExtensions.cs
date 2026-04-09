using System;

namespace Asv.IO;

public static class RandomExtensions
{
    private static readonly char[] DefaultChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public static string NextString(this Random random, string? chars, int length)
    {
        chars ??= new string(DefaultChars);
        var result = new char[length];

        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }

        return new string(result);
    }
}
