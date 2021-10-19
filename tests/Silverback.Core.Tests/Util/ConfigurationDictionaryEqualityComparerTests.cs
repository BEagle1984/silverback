// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class ConfigurationDictionaryEqualityComparerTests
    {
        [Fact]
        public void Equals_SameDictionary_TrueReturned()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };

            var result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionary, dictionary);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameItems_TrueReturned()
        {
            var dictionaryX = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };
            var dictionaryY = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };

            var result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameItemsDifferentOrder_TrueReturned()
        {
            var dictionaryX = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };
            var dictionaryY = new Dictionary<string, string>
            {
                { "two", "due" },
                { "three", "tre" },
                { "one", "uno" }
            };

            var result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentValues_FalseReturned()
        {
            var dictionaryX = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };
            var dictionaryY = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "3" }
            };

            var result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentKeys_FalseReturned()
        {
            var dictionaryX = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };
            var dictionaryY = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "four", "quattro" }
            };

            var result = new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_NullVsNonNull_FalseReturned()
        {
            var dictionaryX = new Dictionary<string, string?>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };
            var dictionaryY = new Dictionary<string, string?>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", null }
            };

            var result =
                new ConfigurationDictionaryEqualityComparer<string, string?>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_NullVsMissingKey_FalseReturned()
        {
            var dictionaryX = new Dictionary<string, string?>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", null }
            };
            var dictionaryY = new Dictionary<string, string?>
            {
                { "one", "uno" },
                { "two", "due" }
            };

            var result =
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

            var result1 =
                new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryX, dictionaryY);
            var result2 =
                new ConfigurationDictionaryEqualityComparer<string, string>().Equals(dictionaryY, dictionaryX);

            result1.Should().BeTrue();
            result2.Should().BeTrue();
        }
    }
}
