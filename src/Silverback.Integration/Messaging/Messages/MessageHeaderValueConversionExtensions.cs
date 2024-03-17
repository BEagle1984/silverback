// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;

namespace Silverback.Messaging.Messages;

internal static class MessageHeaderValueConversionExtensions
{
    public static string? ToHeaderValueString(this object? value) =>
        value is DateTime datetime
            ? datetime.ToString("O", CultureInfo.InvariantCulture)
            : value?.ToString();
}
