// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public partial class BrokerOptionsBuilderMemoryExtensionsFixture
{
    [Fact]
    public void AddInMemoryOutbox_ShouldConfigureOutboxFactories()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));

        IOutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<IOutboxReaderFactory>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();

        IOutboxReader reader = readerFactory.GetReader(new InMemoryOutboxSettings(), serviceProvider);
        IOutboxWriter writer = writerFactory.GetWriter(new InMemoryOutboxSettings(), serviceProvider);

        reader.Should().BeOfType<InMemoryOutboxReader>();
        writer.Should().BeOfType<InMemoryOutboxWriter>();
    }

    [Fact]
    public void UseInMemoryOutbox_ShouldOverrideAllOutboxSettingsTypes()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.UseInMemoryOutbox()));

        OutboxReaderFactory readerFactory = serviceProvider.GetRequiredService<OutboxReaderFactory>();
        OutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<OutboxWriterFactory>();

        readerFactory.AddFactory<OutboxSettings1>((_, _) => new OutboxReader1());
        readerFactory.AddFactory<OutboxSettings2>((_, _) => new OutboxReader2());
        writerFactory.AddFactory<OutboxSettings1>((_, _) => new OutboxWriter1());
        writerFactory.AddFactory<OutboxSettings2>((_, _) => new OutboxWriter2());

        IOutboxReader reader1 = readerFactory.GetReader(new OutboxSettings1(), serviceProvider);
        IOutboxReader reader2 = readerFactory.GetReader(new OutboxSettings2(), serviceProvider);
        IOutboxWriter writer1 = writerFactory.GetWriter(new OutboxSettings1(), serviceProvider);
        IOutboxWriter writer2 = writerFactory.GetWriter(new OutboxSettings2(), serviceProvider);

        reader1.Should().BeOfType<InMemoryOutboxReader>();
        reader2.Should().BeOfType<InMemoryOutboxReader>();
        writer1.Should().BeOfType<InMemoryOutboxWriter>();
        writer2.Should().BeOfType<InMemoryOutboxWriter>();
    }

    private record OutboxSettings1 : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private record OutboxSettings2 : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }

    private class OutboxReader1 : IOutboxReader
    {
        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }

    private class OutboxReader2 : IOutboxReader
    {
        public Task<int> GetLengthAsync() => throw new NotSupportedException();

        public Task<TimeSpan> GetMaxAgeAsync() => throw new NotSupportedException();

        public Task<IReadOnlyCollection<OutboxMessage>> GetAsync(int count) => throw new NotSupportedException();

        public Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages) => throw new NotSupportedException();
    }

    private class OutboxWriter1 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null) =>
            throw new NotSupportedException();
    }

    private class OutboxWriter2 : IOutboxWriter
    {
        public Task AddAsync(OutboxMessage outboxMessage, SilverbackContext? context = null) =>
            throw new NotSupportedException();

        public Task AddAsync(IEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null) =>
            throw new NotSupportedException();

        public Task AddAsync(IAsyncEnumerable<OutboxMessage> outboxMessages, SilverbackContext? context = null) =>
            throw new NotSupportedException();
    }
}
