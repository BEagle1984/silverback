// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Storage;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Producing.TransactionalOutbox;

public class InMemoryOutboxWriterTests
{
    public enum AddOverload
    {
        OneMessage,
        MessagesEnumerable,
        MessagesAsyncEnumerable
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        await outboxWriter.AddAsync(outboxMessage1);
        await outboxWriter.AddAsync(outboxMessage2);
        await outboxWriter.AddAsync(outboxMessage3);

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).ShouldBe([outboxMessage1, outboxMessage2, outboxMessage3]);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemsToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        await outboxWriter.AddAsync([outboxMessage1, outboxMessage2, outboxMessage3]);

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).ShouldBe([outboxMessage1, outboxMessage2, outboxMessage3]);
    }

    [Fact]
    public async Task AddAsync_ShouldAddAsyncItemsToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new([0x01], null, "test");
        OutboxMessage outboxMessage2 = new([0x02], null, "test");
        OutboxMessage outboxMessage3 = new([0x03], null, "test");
        await outboxWriter.AddAsync(new[] { outboxMessage1, outboxMessage2, outboxMessage3 }.ToAsyncEnumerable());

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).ShouldBe([outboxMessage1, outboxMessage2, outboxMessage3]);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemsWithNullContentToStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);

        OutboxMessage outboxMessage1 = new(null, null, "test1");
        OutboxMessage outboxMessage2 = new(null, null, "test2");
        OutboxMessage outboxMessage3 = new(null, null, "test3");
        await outboxWriter.AddAsync(outboxMessage1);
        await outboxWriter.AddAsync([outboxMessage2]);
        await outboxWriter.AddAsync(new[] { outboxMessage3 }.ToAsyncEnumerable());

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).ShouldBe([outboxMessage1, outboxMessage2, outboxMessage3]);
    }

    [Fact]
    public async Task AddAsync_ShouldAddItemToCorrectStorage()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddFakeLogger()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings1 = new("outbox1");
        InMemoryOutboxSettings outboxSettings2 = new("outbox2");
        IOutboxWriter outboxWriter1 = writerFactory.GetWriter(outboxSettings1, serviceProvider);
        IOutboxWriter outboxWriter2 = writerFactory.GetWriter(outboxSettings2, serviceProvider);

        await outboxWriter1.AddAsync(new OutboxMessage([0x01], null, "test"));
        await outboxWriter2.AddAsync(new OutboxMessage([0x02], null, "test"));

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox1 = outboxFactory.GetOutbox(outboxSettings1);
        InMemoryOutbox outbox2 = outboxFactory.GetOutbox(outboxSettings2);
        outbox1.Get(10).Select(message => message.Content).ShouldBe(
        [
            [0x01]
        ]);
        outbox2.Get(10).Select(message => message.Content).ShouldBe(
        [
            [0x02]
        ]);
    }

    [Theory]
    [InlineData(AddOverload.OneMessage)]
    [InlineData(AddOverload.MessagesEnumerable)]
    [InlineData(AddOverload.MessagesAsyncEnumerable)]
    public async Task AddAsync_ShouldLogWarningAndAddItem_WhenTransactionIsEnlisted(AddOverload overload)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        LoggerSubstitute<InMemoryOutboxWriter> logger =
            (LoggerSubstitute<InMemoryOutboxWriter>)serviceProvider.GetRequiredService<ILogger<InMemoryOutboxWriter>>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);
        using IServiceScope scope = serviceProvider.CreateScope();
        ISilverbackContext context = scope.ServiceProvider.GetRequiredService<ISilverbackContext>();
        context.EnlistTransaction(Substitute.For<IStorageTransaction>());

        OutboxMessage outboxMessage = new([0x01], null, "test");
        Task addTask = overload switch
        {
            AddOverload.OneMessage => outboxWriter.AddAsync(outboxMessage, context),
            AddOverload.MessagesEnumerable => outboxWriter.AddAsync([outboxMessage], context),
            _ => outboxWriter.AddAsync(new[] { outboxMessage }.ToAsyncEnumerable(), context)
        };
        await addTask;

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).ShouldBe([outboxMessage]);
        logger.Received(
            LogLevel.Warning,
            null,
            "Ambient transaction not supported by the in-memory outbox implementation; the messages will be stored ignoring the transaction",
            StorageMemoryLogEvents.OutboxTransactionUnsupported.EventId.Id);
    }

    [Fact]
    public async Task AddAsync_ShouldNotLogWarning_WhenContextHasNoTransaction()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(services => services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddInMemoryOutbox()));
        LoggerSubstitute<InMemoryOutboxWriter> logger =
            (LoggerSubstitute<InMemoryOutboxWriter>)serviceProvider.GetRequiredService<ILogger<InMemoryOutboxWriter>>();
        IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
        InMemoryOutboxSettings outboxSettings = new();
        IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings, serviceProvider);
        using IServiceScope scope = serviceProvider.CreateScope();
        ISilverbackContext context = scope.ServiceProvider.GetRequiredService<ISilverbackContext>();

        OutboxMessage outboxMessage = new([0x01], null, "test");
        await outboxWriter.AddAsync(outboxMessage, context);

        InMemoryOutboxFactory outboxFactory = serviceProvider.GetRequiredService<InMemoryOutboxFactory>();
        InMemoryOutbox outbox = outboxFactory.GetOutbox(outboxSettings);
        outbox.Get(10).ShouldBe([outboxMessage]);
        logger.DidNotReceive(LogLevel.Warning, null, eventId: StorageMemoryLogEvents.OutboxTransactionUnsupported.EventId.Id);
    }
}
