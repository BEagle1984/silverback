// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util
{
    public class ConfigurationDictionaryComparerTests
    {
        [Fact]
        public void Equals_SameDictionary_TrueIsReturned()
        {
            var dictionary = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", "tre" }
            };

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionary, dictionary);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameContent_TrueIsReturned()
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

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentValues_FalseIsReturned()
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

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentKeys_FalseIsReturned()
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

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_NullVsNonNull_FalseIsReturned()
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
                { "three", null }
            };

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_NullVsMissingKey_FalseIsReturned()
        {
            var dictionaryX = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" },
                { "three", null }
            };
            var dictionaryY = new Dictionary<string, string>
            {
                { "one", "uno" },
                { "two", "due" }
            };

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeFalse();
        }

        [Fact]
        [SuppressMessage("", "CollectionNeverUpdated.Local")]
        [SuppressMessage("", "ExpressionIsAlwaysNull")]
        public void Equals_NullDictionaryVsEmptyDictionary_TrueIsReturned()
        {
            var dictionaryX = new Dictionary<string, string>();
            var dictionaryY = (Dictionary<string, string>) null;

            var result = new ConfigurationDictionaryComparer<string, string>().Equals(dictionaryX, dictionaryY);

            result.Should().BeTrue();
        }
    }
}