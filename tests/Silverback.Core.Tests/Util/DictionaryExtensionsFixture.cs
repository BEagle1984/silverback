// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using FluentAssertions;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class DictionaryExtensionsFixture
{
    [Fact]
    public void GetOrAdd_ShouldReturnExistingItem()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        int value = dictionary.GetOrAdd(
            "b",
            _ => throw new InvalidOperationException("Shouldn't call this"));

        value.Should().Be(2);
    }

    [Fact]
    public void GetOrAdd_ShouldUseInvokeFactoryAndAddNewItem()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        int value = dictionary.GetOrAdd(
            "d",
            key =>
            {
                key.Should().Be("d");

                return 42;
            });

        value.Should().Be(42);
        dictionary.Keys.Should().Contain("d");
        dictionary.Values.Should().Contain(42);
    }

    [Fact]
    public void GetOrAddDefault_ShouldReturnExistingItem()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        int value = dictionary.GetOrAddDefault("b");

        value.Should().Be(2);
    }

    [Fact]
    public void GetOrAddDefault_ShouldCAddNewItemWithDefaultValue()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        int value = dictionary.GetOrAddDefault("d");

        value.Should().Be(0);
        dictionary.Keys.Should().Contain("d");
        dictionary.Values.Should().Contain(0);
    }
}
