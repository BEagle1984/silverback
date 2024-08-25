// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.Relational.Storage;

public class DbTransactionWrapperFixture
{
    private readonly ISilverbackContext _context = Substitute.For<ISilverbackContext>();

    [Fact]
    public void Constructor_ShouldEnlistTransaction()
    {
        DbTransaction transaction = new TestTransaction();

        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.Should().NotBeNull();
        _context.Received(1).EnlistTransaction(Arg.Any<DbTransactionWrapper>());
    }

    [Fact]
    public void UnderlyingTransaction_ShouldReturnWrappedTransaction()
    {
        DbTransaction transaction = new TestTransaction();

        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.UnderlyingTransaction.Should().Be(transaction);
    }

    [Fact]
    public void Equals_WithSameInstance_ShouldReturnTrue()
    {
        DbTransaction transaction = new TestTransaction();

        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.Equals(transactionWrapper).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithSameWrappedTransaction_ShouldReturnTrue()
    {
        DbTransaction transaction = new TestTransaction();

        object transactionWrapper1 = new DbTransactionWrapper(transaction, _context);
        object transactionWrapper2 = new DbTransactionWrapper(transaction, _context);

        transactionWrapper1.Equals(transactionWrapper2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentWrappedTransaction_ShouldReturnFalse()
    {
        DbTransaction transaction1 = new TestTransaction();
        DbTransaction transaction2 = new TestTransaction();

        object transactionWrapper1 = new DbTransactionWrapper(transaction1, _context);
        object transactionWrapper2 = new DbTransactionWrapper(transaction2, _context);

        transactionWrapper1.Equals(transactionWrapper2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ShouldReturnHashCodeOfWrappedTransaction()
    {
        DbTransaction transaction = new TestTransaction();

        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.GetHashCode().Should().Be(transaction.GetHashCode());
    }

    [Fact]
    public void Commit_ShouldCommitWrappedTransaction()
    {
        TestTransaction transaction = new();
        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.Commit();

        transaction.IsCommitted.Should().BeTrue();
        transaction.IsRolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task CommitAsync_ShouldCommitWrappedTransaction()
    {
        TestTransaction transaction = new();
        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        await transactionWrapper.CommitAsync();

        transaction.IsCommitted.Should().BeTrue();
        transaction.IsRolledBack.Should().BeFalse();
    }

    [Fact]
    public void Rollback_ShouldRollbackWrappedTransaction()
    {
        TestTransaction transaction = new();
        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.Rollback();

        transaction.IsCommitted.Should().BeFalse();
        transaction.IsRolledBack.Should().BeTrue();
    }

    [Fact]
    public async Task RollbackAsync_ShouldRollbackWrappedTransaction()
    {
        TestTransaction transaction = new();
        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        await transactionWrapper.RollbackAsync();

        transaction.IsCommitted.Should().BeFalse();
        transaction.IsRolledBack.Should().BeTrue();
    }

    [Fact]
    public void Dispose_ShouldClearContextTransactionAndDisposeWrappedTransaction()
    {
        TestTransaction transaction = new();
        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        transactionWrapper.Dispose();

        _context.Received(1).RemoveTransaction();
        transaction.IsDisposed.Should().BeTrue();
    }

    [Fact]
    public async Task DisposeAsync_ShouldClearContextTransactionAndDisposeWrappedTransaction()
    {
        TestTransaction transaction = new();
        DbTransactionWrapper transactionWrapper = new(transaction, _context);

        await transactionWrapper.DisposeAsync();

        _context.Received(1).RemoveTransaction();
        transaction.IsDisposed.Should().BeTrue();
    }

    private class TestTransaction : DbTransaction
    {
        public override IsolationLevel IsolationLevel => throw new NotSupportedException();

        public bool IsCommitted { get; private set; }

        public bool IsRolledBack { get; private set; }

        public bool IsDisposed { get; private set; }

        protected override DbConnection DbConnection => throw new NotSupportedException();

        public override void Commit() => IsCommitted = true;

        public override void Rollback() => IsRolledBack = true;

        [SuppressMessage("Usage", "CA2215:Dispose methods should call base class dispose", Justification = "Test code")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                IsDisposed = true;
        }
    }
}
