// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Util;

internal static class ConfigurationDictionaryEqualityComparer
{
    internal static readonly ConfigurationDictionaryEqualityComparer<string, string> StringString = new();
}
