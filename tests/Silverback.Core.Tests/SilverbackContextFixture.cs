// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Silverback.Tests.Core;

public class SilverbackContextFixture
{
    private static readonly Guid ObjectTypeId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public void AddObject_ShouldStoreNewObject()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        context.AddObject(ObjectTypeId, "new");

        context.TryGetObject(ObjectTypeId, out object? value).ShouldBeTrue();
        value.ShouldBe("new");
    }

    [Fact]
    public void AddObject_ShouldThrow_WhenSameObjectTypeAlreadyAdded()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.AddObject(ObjectTypeId, new object());

        Action act = () => context.AddObject(ObjectTypeId, new object());

        InvalidOperationException exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("An object of type aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee has already been added.");
    }

    [Fact]
    public void AddObject_ShouldNotThrow_WhenSameObjectAlreadyAdded()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        object obj = new();
        context.AddObject(ObjectTypeId, obj);

        Action act = () => context.AddObject(ObjectTypeId, obj);

        act.ShouldNotThrow();
    }

    [Fact]
    public void SetObject_ShouldStoreNewObject()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        context.SetObject(ObjectTypeId, "new");

        context.TryGetObject(ObjectTypeId, out object? value).ShouldBeTrue();
        value.ShouldBe("new");
    }

    [Fact]
    public void SetObject_ShouldOverwriteObject()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        context.SetObject(ObjectTypeId, "new");
        context.SetObject(ObjectTypeId, "overwritten");

        context.TryGetObject(ObjectTypeId, out object? value).ShouldBeTrue();
        value.ShouldBe("overwritten");
    }

    [Fact]
    public void RemoveObject_ShouldRemoveObject()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, "myobject");

        context.RemoveObject(ObjectTypeId);

        context.TryGetObject(ObjectTypeId, out object? _).ShouldBeFalse();
    }

    [Fact]
    public void GetObject_ShouldReturnStoredObject()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, "myobject");

        context.GetObject(ObjectTypeId).ShouldBe("myobject");
    }

    [Fact]
    public void GetObject_ShouldReturnStoredObject_WhenSpecifyingType()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, "myobject");

        context.GetObject<string>(ObjectTypeId).ShouldBe("myobject");
    }

    [Fact]
    public void GetObject_ShouldThrow_WhenObjectDoesntExist()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        Action act = () => context.GetObject(ObjectTypeId);

        InvalidOperationException exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The object with type id aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found.");
    }

    [Fact]
    public void GetObject_ShouldThrow_WhenSpecifyingTypeAndObjectDoesntExist()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        Action act = () => context.GetObject<string>(ObjectTypeId);

        InvalidOperationException exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The object with type id aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee was not found.");
    }

    [Fact]
    public void TryGetObject_ShouldReturnStoredObject()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, "myobject");

        context.TryGetObject(ObjectTypeId, out object? value).ShouldBeTrue();
        value.ShouldBe("myobject");
    }

    [Fact]
    public void TryGetObject_ShouldReturnStoredObject_WhenSpecifyingType()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, "myobject");

        context.TryGetObject(ObjectTypeId, out string? value).ShouldBeTrue();
        value.ShouldBe("myobject");
    }

    [Fact]
    public void TryGetObject_ShouldReturnFalse_WhenObjectDoesntExist()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        context.TryGetObject(ObjectTypeId, out object? _).ShouldBeFalse();
    }

    [Fact]
    public void GetOrAddObject_ShouldReturnStoredObject()
    {
        object obj = new();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, obj);

        object result = context.GetOrAddObject(ObjectTypeId, () => new object());

        result.ShouldBe(obj);
    }

    [Fact]
    [SuppressMessage("ReSharper", "HeapView.CanAvoidClosure", Justification = "Test code")]
    public void GetOrAddObject_ShouldAddNewObject_WhenNotExists()
    {
        object obj = new();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        object result = context.GetOrAddObject(ObjectTypeId, () => obj);

        result.ShouldBe(obj);
        context.TryGetObject(ObjectTypeId, out object? storedObj).ShouldBeTrue();
        storedObj.ShouldBe(obj);
    }

    [Fact]
    public void GetOrAddObject_ShouldReturnStoredObject_WhenArgumentIsPassed()
    {
        object obj = new();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        context.SetObject(ObjectTypeId, obj);

        object result = context.GetOrAddObject(ObjectTypeId, _ => new object(), "123");

        result.ShouldBe(obj);
    }

    [Fact]
    public void GetOrAddObject_ShouldAddNewObject_WhenNotExistsAndArgumentIsPassed()
    {
        object obj = new();
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        object result = context.GetOrAddObject(ObjectTypeId, arg => arg, obj);

        result.ShouldBe(obj);
        context.TryGetObject(ObjectTypeId, out object? storedObj).ShouldBeTrue();
        storedObj.ShouldBe(obj);
    }
}
