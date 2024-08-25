// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Data;
using System.Data.Common;
using NSubstitute;
using Silverback.Messaging.Publishing;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.Relational.Storage;

public class PublisherStorageExtensionsFixture
{
    private readonly ISilverbackContext _context = Substitute.For<ISilverbackContext>();

    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    public PublisherStorageExtensionsFixture()
    {
        _publisher.Context.Returns(_context);
    }

    [Fact]
    public void EnlistTransaction_ShouldEnlistTransaction()
    {
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();

        _publisher.EnlistTransaction(transaction);

        _context.Received(1).EnlistTransaction(transaction);
    }

    [Fact]
    public void EnlistDbTransaction_ShouldEnlistDbTransaction()
    {
        DbTransaction transaction = new TestTransaction();

        _publisher.EnlistDbTransaction(transaction);

        _context.Received(1).EnlistDbTransaction(transaction);
    }

    private class TestTransaction : DbTransaction
    {
        public override IsolationLevel IsolationLevel => throw new NotSupportedException();

        protected override DbConnection DbConnection => throw new NotSupportedException();

        public override void Commit() => throw new NotSupportedException();

        public override void Rollback() => throw new NotSupportedException();
    }
}
