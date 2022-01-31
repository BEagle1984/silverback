// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Outbound.TransactionalOutbox;

public class InMemoryOutboxWriterFixture
{
    private static readonly OutboxMessageEndpoint Endpoint = new("test", null, null);

    [Fact]
    public async Task AddAsync_ShouldAddItemToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);

        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);
        storage.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 },
                new byte[] { 0x02 },
                new byte[] { 0x03 }
            });
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToCorrectStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings1 = new("outbox1");
        InMemoryOutboxSettings outboxSettings2 = new("outbox2");
        IOutboxWriter outboxWriter1 = writerFactory.GetWriter(outboxSettings1);
        IOutboxWriter outboxWriter2 = writerFactory.GetWriter(outboxSettings2);

        await outboxWriter1.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter2.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));

        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage1 = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings1);
        InMemoryStorage<OutboxMessage> storage2 = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings2);
        storage1.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 }
            });
        storage2.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x02 }
            });
    }

    [Fact]
    public async Task Add_ShouldJoinAmbientTransaction()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
        InMemoryStorageFactory storageFactory = serviceProvider.GetRequiredService<InMemoryStorageFactory>();
        InMemoryStorage<OutboxMessage> storage = storageFactory.GetStorage<OutboxSettings, OutboxMessage>(outboxSettings);

        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x01 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x02 }, null, Endpoint));
        await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x03 }, null, Endpoint));

        using (TransactionScope dummy = new())
        {
            await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x99 }, null, Endpoint));

            // Don't commit the transaction
        }

        storage.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 },
                new byte[] { 0x02 },
                new byte[] { 0x03 }
            });

        using (TransactionScope transaction = new())
        {
            await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x04 }, null, Endpoint));
            await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x05 }, null, Endpoint));
            await outboxWriter.AddAsync(new OutboxMessage(typeof(TestMessage), new byte[] { 0x06 }, null, Endpoint));

            storage.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
                new[]
                {
                    new byte[] { 0x01 },
                    new byte[] { 0x02 },
                    new byte[] { 0x03 }
                });

            transaction.Complete();
        }

        storage.Get(10).Select(message => message.Content).Should().BeEquivalentTo(
            new[]
            {
                new byte[] { 0x01 },
                new byte[] { 0x02 },
                new byte[] { 0x03 },
                new byte[] { 0x04 },
                new byte[] { 0x05 },
                new byte[] { 0x06 }
            });
    }

    [SuppressMessage("", "CA1812", Justification = "Class used for testing")]
    private class TestMessage
    {
    }
}
