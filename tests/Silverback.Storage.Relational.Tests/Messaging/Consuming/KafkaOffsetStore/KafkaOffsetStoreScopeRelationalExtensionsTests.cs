// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Storage;
using Xunit;

namespace Silverback.Tests.Storage.Relational.Messaging.Consuming.KafkaOffsetStore;

public class KafkaOffsetStoreScopeRelationalExtensionsTests
{
    private readonly SilverbackContext _silverbackContext;

    private readonly KafkaOffsetStoreScope _scope;

    public KafkaOffsetStoreScopeRelationalExtensionsTests()
    {
        IServiceProvider serviceProvider = Substitute.For<IServiceProvider>();
        _silverbackContext = new SilverbackContext(serviceProvider);
        serviceProvider.GetService(typeof(SilverbackContext)).Returns(_silverbackContext);

        ConsumerPipelineContext pipelineContext = new(
            Substitute.For<IRawInboundEnvelope>(),
            Substitute.For<IConsumer>(),
            Substitute.For<ISequenceStore>(),
            [],
            serviceProvider);

        _scope = new KafkaOffsetStoreScope(Substitute.For<IKafkaOffsetStore>(), pipelineContext);
    }

    [Fact]
    public void EnlistTransaction_ShouldEnlistStorageTransactionInSilverbackContext()
    {
        IStorageTransaction transaction = Substitute.For<IStorageTransaction>();

        _scope.EnlistTransaction(transaction);

        _silverbackContext.TryGetStorageTransaction(out IStorageTransaction? enlistedTransaction).ShouldBeTrue();
        enlistedTransaction.ShouldBeSameAs(transaction);
    }

    [Fact]
    public void EnlistTransaction_ShouldEnlistDbTransactionInSilverbackContext()
    {
        using SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        using SqliteTransaction dbTransaction = connection.BeginTransaction();

        IStorageTransaction storageTransaction = _scope.EnlistTransaction(dbTransaction);

        storageTransaction.UnderlyingTransaction.ShouldBeSameAs(dbTransaction);
        _silverbackContext.GetActiveDbTransaction<SqliteTransaction>().ShouldBeSameAs(dbTransaction);
    }
}
