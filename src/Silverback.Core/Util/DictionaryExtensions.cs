// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Util;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value))
            return value;

        return dictionary[key] = factory(key);
    }

    public static TValue GetOrAddDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key)
        where TKey : notnull
        where TValue : new() =>
        dictionary.GetOrAdd(key, static _ => new TValue());
}
