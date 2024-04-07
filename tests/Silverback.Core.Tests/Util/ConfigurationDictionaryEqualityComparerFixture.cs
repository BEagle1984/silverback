// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class ConfigurationDictionaryEqualityComparerFixture
{
    [Fact]
    public void StringString_ShouldReturnStaticInstance()
    {
        object comparer1 = ConfigurationDictionaryEqualityComparer.StringString;
        object comparer2 = ConfigurationDictionaryEqualityComparer.StringString;

        comparer1.Should().BeOfType<ConfigurationDictionaryEqualityComparer<string, string>>();
        comparer1.Should().BeSameAs(comparer2);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingToSameDictionary()
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
    public void Equals_ShouldReturnTrue_WhenDictionariesContainTheSameValues()
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
    public void Equals_ShouldReturnTrue_WhenDictionariesContainTheSameValuesInDifferentOrder()
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
    public void Equals_ShouldReturnFalse_WhenDictionariesContainTheSameKeysWithDifferentValues()
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
    public void Equals_ShouldReturnFalse_WhenDictionariesContainDifferentKeys()
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
    public void Equals_ShouldReturnFalse_WhenDictionariesContainDifferentValueAndOneIsNull()
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
    public void Equals_ShouldReturnFalse_WhenKeyIsMissingAndOtherDictionaryContainsNullForTheSameKey()
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
    public void Equals_ShouldReturnTrue_WhenComparingNullWithEmptyDictionary()
    {
        Dictionary<string, string> dictionaryX = [];
        Dictionary<string, string>? dictionaryY = null;

        bool result1 = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);
        bool result2 = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryY, dictionaryX);

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }
}
