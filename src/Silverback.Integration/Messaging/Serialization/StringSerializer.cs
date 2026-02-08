// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Text;

namespace Silverback.Messaging.Serialization;

// TODO: Test
public class StringSerializer : ISimpleSerializer
{
    public byte[]? Serialize(object? value)
    {
        if (value is null)
            return null;

        string? stringValue = value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(stringValue))
            return null;

        return Encoding.UTF8.GetBytes(stringValue);
    }
}
