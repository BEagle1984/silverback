// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;

namespace Silverback.Messaging.Messages;

internal static class MessageHeaderValueConversionExtensions
{
    public static string? ToHeaderValueString(this object? value) =>
        value switch
        {
            null => null,
            DateTime datetime => datetime.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
}
