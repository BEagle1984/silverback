// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;

namespace Silverback.Util;

internal static class Utf8EncodingExtensions
{
    public static string? ToUtf8String(this byte[]? data) => data == null ? null : Encoding.UTF8.GetString(data);

    public static byte[]? ToUtf8Bytes(this string? data) => string.IsNullOrEmpty(data) ? null : Encoding.UTF8.GetBytes(data);
}
