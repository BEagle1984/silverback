// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.Text;

namespace Silverback.Messaging.Serialization;

public class StringDeserializer<T> : ISimpleDeserializer
{
    public object? Deserialize(byte[]? data)
    {
        if (data is null || data.Length == 0)
            return null;

        string stringValue = Encoding.UTF8.GetString(data);
        return Convert.ChangeType(stringValue, typeof(T), CultureInfo.InvariantCulture);
    }
}
