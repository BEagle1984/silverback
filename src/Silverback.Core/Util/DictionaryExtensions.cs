// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Util;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value))
            return value;

        return dictionary[key] = factory(key);
    }

    public static TValue GetOrAddDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : new() =>
        dictionary.GetOrAdd(key, static _ => new TValue());

    public static void AddOrUpdate<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TValue> addValueFactory,
        Func<TKey, TValue, TValue> updateValueFactory)
        where TKey : notnull
    {
        if (dictionary.ContainsKey(key))
            dictionary[key] = updateValueFactory.Invoke(key, dictionary[key]);
        else
            dictionary[key] = addValueFactory.Invoke(key);
    }

    public static void AddOrUpdate<TKey, TValue, TData>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        Func<TKey, TData, TValue> addValueFactory,
        Func<TKey, TValue, TData, TValue> updateValueFactory,
        TData data)
        where TKey : notnull
    {
        if (dictionary.ContainsKey(key))
            dictionary[key] = updateValueFactory.Invoke(key, dictionary[key], data);
        else
            dictionary[key] = addValueFactory.Invoke(key, data);
    }
}
