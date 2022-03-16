// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Xunit;

namespace Silverback.Tests.Core;

public class SilverbackContextFixture
{
    [Fact]
    public void SetObject_ShouldStoreNewObject()
    {
        SilverbackContext context = new();

        context.SetObject(42, "new");

        context.TryGetObject(42, out object? value).Should().BeTrue();
        value.Should().BeEquivalentTo("new");
    }

    [Fact]
    public void SetObject_ShouldOverwriteObject()
    {
        SilverbackContext context = new();

        context.SetObject(42, "new");
        context.SetObject(42, "overwritten");

        context.TryGetObject(42, out object? value).Should().BeTrue();
        value.Should().BeEquivalentTo("overwritten");
    }

    [Fact]
    public void TryGetObject_ShouldReturnStoredObject()
    {
        SilverbackContext context = new();
        context.SetObject(42, "myobject");

        context.TryGetObject(42, out object? value).Should().BeTrue();
        value.Should().Be("myobject");
    }

    [Fact]
    public void TryGetObject_ShouldReturnFalse_WhenObjectDoesntExist()
    {
        SilverbackContext context = new();

        context.TryGetObject(42, out object? _).Should().BeFalse();
    }
}
