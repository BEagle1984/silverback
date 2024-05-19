// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using NSubstitute;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Integration.Storage;

public class SilverbackContextStorageExtensionsFixture
{
    [Fact]
    public void EnlistTransaction_ShouldSetTransaction()
    {
        SilverbackContext context = new();
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();

        context.EnlistTransaction(transaction);

        context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction);
        storedTransaction.Should().BeSameAs(transaction);
    }

    [Fact]
    public void TryGetStorageTransaction_ShouldReturnTrue_WhenTransactionIsStored()
    {
        SilverbackContext context = new();
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();
        context.EnlistTransaction(transaction);

        bool result = context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction);

        result.Should().BeTrue();
        storedTransaction.Should().BeSameAs(transaction);
    }

    [Fact]
    public void TryGetStorageTransaction_ShouldReturnFalse_WhenNoTransactionIsStored()
    {
        SilverbackContext context = new();

        bool result = context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction);

        result.Should().BeFalse();
        storedTransaction.Should().BeNull();
    }

    [Fact]
    public void RemoveTransaction_ShouldClearTransaction()
    {
        SilverbackContext context = new();
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();
        context.EnlistTransaction(transaction);

        context.RemoveTransaction();

        context.TryGetStorageTransaction(out IStorageTransaction? storedTransaction).Should().BeFalse();
        storedTransaction.Should().BeNull();
    }
}
