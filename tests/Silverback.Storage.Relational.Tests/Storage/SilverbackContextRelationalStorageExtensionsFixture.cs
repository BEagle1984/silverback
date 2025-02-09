// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Shouldly;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.Relational.Storage;

public class SilverbackContextRelationalStorageExtensionsFixture
{
    [Fact]
    public void EnlistDbTransaction()
    {
        ISilverbackContext context = Substitute.For<ISilverbackContext>();
        DbTransaction transaction = new TestTransaction();

        IStorageTransaction wrappedTransaction = context.EnlistDbTransaction(transaction);

        wrappedTransaction.ShouldBeOfType<DbTransactionWrapper>().UnderlyingTransaction.ShouldBe(transaction);
        context.Received(1).EnlistTransaction(wrappedTransaction);
    }

    [Fact]
    public void GetActiveDbTransaction_ShouldReturnActiveTransaction()
    {
        ISilverbackContext context = new SilverbackContext(Substitute.For<IServiceProvider>());
        DbTransaction transaction = new TestTransaction();
        context.EnlistDbTransaction(transaction);

        DbTransaction? activeTransaction = context.GetActiveDbTransaction<DbTransaction>();

        activeTransaction.ShouldBe(transaction);
    }

    [Fact]
    public void GetActiveDbTransaction_ShouldReturnNull_WhenContextIsNull()
    {
        ISilverbackContext? context = null;

        DbTransaction? activeTransaction = context.GetActiveDbTransaction<DbTransaction>();

        activeTransaction.ShouldBeNull();
    }

    [Fact]
    public void GetActiveDbTransaction_ShouldReturnNull_WhenNoTransaction()
    {
        ISilverbackContext context = new SilverbackContext(Substitute.For<IServiceProvider>());

        DbTransaction? activeTransaction = context.GetActiveDbTransaction<DbTransaction>();

        activeTransaction.ShouldBeNull();
    }

    [Fact]
    public void TryGetActiveDbTransaction_ShouldReturnActiveTransaction()
    {
        ISilverbackContext context = new SilverbackContext(Substitute.For<IServiceProvider>());
        DbTransaction transaction = new TestTransaction();
        context.EnlistDbTransaction(transaction);

        bool result = context.TryGetActiveDbTransaction(out DbTransaction? activeTransaction);

        result.ShouldBeTrue();
        activeTransaction.ShouldBe(transaction);
    }

    [Fact]
    public void TryGetActiveDbTransaction_ShouldReturnFalse_WhenNoTransaction()
    {
        ISilverbackContext context = new SilverbackContext(Substitute.For<IServiceProvider>());

        bool result = context.TryGetActiveDbTransaction(out TestTransaction? activeTransaction);

        result.ShouldBeFalse();
        activeTransaction.ShouldBeNull();
    }

    [Fact]
    public void TryGetActiveDbTransaction_ShouldThrow_WhenTransactionTypeMismatch()
    {
        ISilverbackContext context = new SilverbackContext(Substitute.For<IServiceProvider>());
        DbTransaction transaction = new TestTransaction();
        context.EnlistDbTransaction(transaction);

        Action act = () => context.GetActiveDbTransaction<SqliteTransaction>();

        Exception exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe(
            "The current transaction (TestTransaction) is not a SqliteTransaction. " +
            "Silverback must be configured to use the same storage as the one used by the application.");
    }

    private class TestTransaction : DbTransaction
    {
        public override IsolationLevel IsolationLevel => throw new NotSupportedException();

        protected override DbConnection DbConnection => throw new NotSupportedException();

        public override void Commit() => throw new NotSupportedException();

        public override void Rollback() => throw new NotSupportedException();
    }
}
