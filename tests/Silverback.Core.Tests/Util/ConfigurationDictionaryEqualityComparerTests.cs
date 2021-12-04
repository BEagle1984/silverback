// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class ConfigurationDictionaryEqualityComparerTests
{
    [Fact]
    public void Equals_SameDictionary_TrueReturned()
    {
        Dictionary<string, string> dictionary = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };

        bool result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionary, dictionary);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameItems_TrueReturned()
    {
        Dictionary<string, string> dictionaryX = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };
        Dictionary<string, string> dictionaryY = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };

        bool result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_SameItemsDifferentOrder_TrueReturned()
    {
        Dictionary<string, string> dictionaryX = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };
        Dictionary<string, string> dictionaryY = new()
        {
            { "two", "due" },
            { "three", "tre" },
            { "one", "uno" }
        };

        bool result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

        result.Should().BeTrue();
    }

    [Fact]
    public void Equals_DifferentValues_FalseReturned()
    {
        Dictionary<string, string> dictionaryX = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };
        Dictionary<string, string> dictionaryY = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "3" }
        };

        bool result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_DifferentKeys_FalseReturned()
    {
        Dictionary<string, string> dictionaryX = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };
        Dictionary<string, string> dictionaryY = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "four", "quattro" }
        };

        bool result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_NullVsNonNull_FalseReturned()
    {
        Dictionary<string, string?> dictionaryX = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", "tre" }
        };
        Dictionary<string, string?> dictionaryY = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", null }
        };

        bool result =
            new ConfigurationDictionaryEqualityComparer<string, string?>().Equals(dictionaryX, dictionaryY);

        result.Should().BeFalse();
    }

    [Fact]
    public void Equals_NullVsMissingKey_FalseReturned()
    {
        Dictionary<string, string?> dictionaryX = new()
        {
            { "one", "uno" },
            { "two", "due" },
            { "three", null }
        };
        Dictionary<string, string?> dictionaryY = new()
        {
            { "one", "uno" },
            { "two", "due" }
        };

        bool result =
            new ConfigurationDictionaryEqualityComparer<string, string?>().Equals(dictionaryX, dictionaryY);

        result.Should().BeFalse();
    }

    [Fact]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local", Justification = "Test")]
    [SuppressMessage("ReSharper", "ExpressionIsAlwaysNull", Justification = "Test")]
    public void Equals_NullDictionaryVsEmptyDictionary_TrueReturned()
    {
        Dictionary<string, string> dictionaryX = new();
        Dictionary<string, string>? dictionaryY = null;

        bool result1 =
            new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);
        bool result2 =
            new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryY, dictionaryX);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}
