// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Silverback.Util;

internal static class HashExtensions
{
    public static string GetSha256Hash(this string input)
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        StringBuilder builder = new();

        foreach (byte hashByte in hashBytes)
        {
            builder.Append(hashByte.ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
