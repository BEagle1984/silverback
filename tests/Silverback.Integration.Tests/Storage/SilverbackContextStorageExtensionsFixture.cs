// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Shouldly;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Integration.Storage;

public class SilverbackContextStorageExtensionsFixture
{
    [Fact]
    public void EnlistTransaction_ShouldSetTransaction()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();

        context.EnlistTransaction(transaction);

        context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction);
        storedTransaction.ShouldBeSameAs(transaction);
    }

    [Fact]
    public void TryGetStorageTransaction_ShouldReturnTrue_WhenTransactionIsStored()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();
        context.EnlistTransaction(transaction);

        bool result = context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction);

        result.ShouldBeTrue();
        storedTransaction.ShouldBeSameAs(transaction);
    }

    [Fact]
    public void TryGetStorageTransaction_ShouldReturnFalse_WhenNoTransactionIsStored()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());

        bool result = context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction);

        result.ShouldBeFalse();
        storedTransaction.ShouldBeNull();
    }

    [Fact]
    public void RemoveTransaction_ShouldClearTransaction()
    {
        SilverbackContext context = new(Substitute.For<IServiceProvider>());
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();
        context.EnlistTransaction(transaction);

        context.ClearStorageTransaction();

        context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction).ShouldBeFalse();
        storedTransaction.ShouldBeNull();
    }
}
