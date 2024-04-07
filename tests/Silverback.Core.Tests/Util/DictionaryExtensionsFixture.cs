// Copyright (c) 2024 Sergio Aquilini
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
        dictionary.Should().HaveCount(3);
    }

    [Fact]
    public void GetOrAdd_ShouldAddNewItem()
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
        dictionary.Should().HaveCount(4);
        dictionary.Keys.Should().Contain("d");
        dictionary["d"].Should().Be(42);
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
        dictionary.Should().HaveCount(3);
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
        dictionary.Should().HaveCount(4);
        dictionary.Keys.Should().Contain("d");
        dictionary["d"].Should().Be(0);
    }

    [Fact]
    public void AddOrUpdate_ShouldAddNewItem()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        dictionary.AddOrUpdate(
            "d",
            _ => 42,
            (_, _) => throw new InvalidOperationException("Shouldn't call this"));

        dictionary.Should().HaveCount(4);
        dictionary.Keys.Should().Contain("d");
        dictionary["d"].Should().Be(42);
    }

    [Fact]
    public void AddOrUpdate_ShouldUpdateExistingItem()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        dictionary.AddOrUpdate(
            "c",
            _ => throw new InvalidOperationException("Shouldn't call this"),
            (_, _) => 42);

        dictionary.Should().HaveCount(3);
        dictionary["c"].Should().Be(42);
    }

    [Fact]
    public void AddOrUpdate_ShouldAddNewItem_WhenPassingExtraData()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        dictionary.AddOrUpdate(
            "d",
            static (_, data) => data,
            static (_, _, _) => throw new InvalidOperationException("Shouldn't call this"),
            42);

        dictionary.Should().HaveCount(4);
        dictionary.Keys.Should().Contain("d");
        dictionary["d"].Should().Be(42);
    }

    [Fact]
    public void AddOrUpdate_ShouldUpdateExistingItem_WhenPassingExtraData()
    {
        Dictionary<string, int> dictionary = new()
        {
            { "a", 1 },
            { "b", 2 },
            { "c", 3 }
        };

        dictionary.AddOrUpdate(
            "c",
            static (_, _) => throw new InvalidOperationException("Shouldn't call this"),
            static (_, _, data) => data,
            42);

        dictionary.Should().HaveCount(3);
        dictionary["c"].Should().Be(42);
    }
}
