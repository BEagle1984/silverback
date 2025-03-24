// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Util;

internal static class Base64Extensions
{
    public static string? ToBase64String(this byte[]? data) => data == null ? null : Convert.ToBase64String(data);

    public static byte[]? FromBase64String(this string? base64String) => string.IsNullOrEmpty(base64String)
        ? null
        : Convert.FromBase64String(base64String);
}
