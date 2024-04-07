// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Xunit;

namespace Silverback.Tests.Core;

public class SilverbackContextFixture
{
    private static readonly Guid ObjectTypeId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void SetObject_ShouldStoreNewObject()
    {
        SilverbackContext context = new();

        context.SetObject(ObjectTypeId, "new");

        context.TryGetObject(ObjectTypeId, out object? value).Should().BeTrue();
        value.Should().BeEquivalentTo("new");
    }

    [Fact]
    public void SetObject_ShouldOverwriteObject()
    {
        SilverbackContext context = new();

        context.SetObject(ObjectTypeId, "new");
        context.SetObject(ObjectTypeId, "overwritten");

        context.TryGetObject(ObjectTypeId, out object? value).Should().BeTrue();
        value.Should().BeEquivalentTo("overwritten");
    }

    [Fact]
    public void TryGetObject_ShouldReturnStoredObject()
    {
        SilverbackContext context = new();
        context.SetObject(ObjectTypeId, "myobject");

        context.TryGetObject(ObjectTypeId, out object? value).Should().BeTrue();
        value.Should().Be("myobject");
    }

    [Fact]
    public void TryGetObject_ShouldReturnFalse_WhenObjectDoesntExist()
    {
        SilverbackContext context = new();

        context.TryGetObject(ObjectTypeId, out object? _).Should().BeFalse();
    }
}
