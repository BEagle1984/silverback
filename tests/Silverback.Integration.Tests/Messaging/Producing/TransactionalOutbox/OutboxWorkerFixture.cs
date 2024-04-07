// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Producing.TransactionalOutbox;

public class OutboxWorkerFixture
{
    private readonly InMemoryOutbox _inMemoryOutbox = new();

    private readonly IOutboxWriter _outboxWriter;

    private readonly IProducerCollection _producerCollection = Substitute.For<IProducerCollection>();

    private readonly IProducer _producer1 = Substitute.For<IProducer>();

    private readonly IProducer _producer2 = Substitute.For<IProducer>();

    private readonly IProducerLogger<OutboxWorker> _producerLogger = Substitute.For<IProducerLogger<OutboxWorker>>();

    private readonly OutboxMessageEndpoint _outboxEndpoint1 = new("one", null);

    private readonly OutboxMessageEndpoint _outboxEndpoint2 = new("two", null);

    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public OutboxWorkerFixture()
    {
        _outboxWriter = new InMemoryOutboxWriter(_inMemoryOutbox);

        _producerCollection.GetProducerForEndpoint("one").Returns(_producer1);
        _producerCollection.GetProducerForEndpoint("two").Returns(_producer2);

        _producer1.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic1")
            {
                FriendlyName = "one"
            });
        _producer2.EndpointConfiguration.Returns(
            new TestProducerEndpointConfiguration("topic2")
            {
                FriendlyName = "two"
            });

        _producer1
            .When(
                producer => producer.RawProduce(
                    Arg.Any<ProducerEndpoint>(),
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?>>(),
                    Arg.Any<Action<Exception>>()))
            .Do(callInfo => callInfo.ArgAt<Action<IBrokerMessageIdentifier?>>(3).Invoke(null));
        _producer2
            .When(
                producer => producer.RawProduce(
                    Arg.Any<ProducerEndpoint>(),
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>(),
                    Arg.Any<Action<IBrokerMessageIdentifier?>>(),
                    Arg.Any<Action<Exception>>()))
            .Do(callInfo => callInfo.ArgAt<Action<IBrokerMessageIdentifier?>>(3).Invoke(null));
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldDequeueAndProduceMessages_WhenNotEnforcingOrder()
    {
        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, _outboxEndpoint1));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, _outboxEndpoint2));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, _outboxEndpoint1));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = false },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        AssertReceivedCalls(_producer1, 2);
        AssertReceivedCalls(_producer2, 1);
    }

    [Fact]
    public async Task ProcessOutboxAsync_ShouldDequeueAndProduceMessages_WhenEnforcingOrder()
    {
        await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, _outboxEndpoint1));
        await _outboxWriter.AddAsync(new OutboxMessage([0x02], null, _outboxEndpoint2));
        await _outboxWriter.AddAsync(new OutboxMessage([0x03], null, _outboxEndpoint1));

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);

        await AssertReceivedBlockingCallsAsync(_producer1, 2);
        await AssertReceivedBlockingCallsAsync(_producer2, 1);
    }

    // TODO: Reimplement and add tests for headers, etc.
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProcessSpecificOutbox_WhenMultipleOutboxesAreUsed()
    // {
    //     InMemoryOutboxSettings outboxSettings1 = new("outbox1");
    //     InMemoryOutboxSettings outboxSettings2 = new("outbox2");
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings1))
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings2)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventTwo>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter1 = writerFactory.GetWriter(outboxSettings1);
    //     IOutboxWriter outboxWriter2 = writerFactory.GetWriter(outboxSettings2);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker[] outboxWorkers =
    //         serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Select(service => service.OutboxWorker).ToArray();
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter1.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //     await outboxWriter2.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventTwo),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //
    //     await outboxWorkers[0].ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(1);
    //     broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic1");
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //     broker.ProducedMessages[0].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 1));
    //
    //     await outboxWorkers[1].ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(2);
    //     broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic2");
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    //     broker.ProducedMessages[1].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 2));
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceToCorrectEndpoint_WhenMultipleEndpointsAreConfiguredForTheSameType()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventOne>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(2);
    //     broker.ProducedMessages[0].Endpoint.RawName.Should().Be("topic1");
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //     broker.ProducedMessages[0].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 1));
    //     broker.ProducedMessages[1].Endpoint.RawName.Should().Be("topic2");
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    //     broker.ProducedMessages[1].Headers.Should().ContainEquivalentOf(new MessageHeader("#", 2));
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceNewMessagesWhenCalledAgain()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(new OutboxWorkerSettings(outboxSettings)))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)
    //                     .AddOutbound<TestEventTwo>(_configuration2)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventOne),
    //             new byte[] { 0x01 },
    //             new[] { new MessageHeader("#", 1) },
    //             _outboxEndpoint1));
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(1);
    //     broker.ProducedMessages[0].Message.Should().BeEquivalentTo(new byte[] { 0x01 });
    //
    //     await outboxWriter.AddAsync(
    //         new OutboxMessage(
    //             typeof(TestEventTwo),
    //             new byte[] { 0x02 },
    //             new[] { new MessageHeader("#", 2) },
    //             _outboxEndpoint2));
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(2);
    //     broker.ProducedMessages[1].Message.Should().BeEquivalentTo(new byte[] { 0x02 });
    // }
    //
    // [Fact]
    // public async Task ProcessOutboxAsync_ShouldProduceSingleBatch()
    // {
    //     InMemoryOutboxSettings outboxSettings = new();
    //     IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
    //         services => services
    //             .AddFakeLogger()
    //             .AddSilverback()
    //             .WithConnectionToMessageBroker(
    //                 options => options
    //                     .AddBroker<TestBroker>()
    //                     .AddInMemoryOutbox()
    //                     .AddOutboxWorker(
    //                         new OutboxWorkerSettings(outboxSettings)
    //                         {
    //                             BatchSize = 5
    //                         }))
    //             .AddEndpoints(
    //                 endpoints => endpoints
    //                     .AddOutbound<TestEventOne>(_configuration1)));
    //     IOutboxWriterFactory writerFactory = serviceProvider.GetRequiredService<IOutboxWriterFactory>();
    //     IOutboxWriter outboxWriter = writerFactory.GetWriter(outboxSettings);
    //     TestBroker broker = serviceProvider.GetRequiredService<TestBroker>();
    //     IOutboxWorker outboxWorker = serviceProvider.GetServices<IHostedService>().OfType<OutboxWorkerService>().Single().OutboxWorker;
    //
    //     await broker.ConnectAsync();
    //
    //     for (int i = 0; i < 10; i++)
    //     {
    //         await outboxWriter.AddAsync(new OutboxMessage(typeof(TestEventOne), new byte[] { 0x01 }, null, _outboxEndpoint1));
    //     }
    //
    //     await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //
    //     broker.ProducedMessages.Should().HaveCount(5);
    // }
    // [Fact]
    // [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    // public async Task ProcessQueue_ShouldRetryAndEnforceOrder_WhenProduceFails()
    // {
    //     for (int i = 0; i < 10; i++)
    //     {
    //         await _outboxWriter.AddAsync(new OutboxMessage(typeof(TestEventOne), new byte[] { 0x01 }, null, _outboxEndpoint1));
    //     }
    //
    //     int tries = 0;
    //     _producer1
    //         .When(
    //             producer => producer.RawProduceAsync(
    //                 Arg.Any<ProducerEndpoint>(),
    //                 Arg.Any<byte[]>(),
    //                 Arg.Any<IReadOnlyCollection<MessageHeader>>()))
    //         .Do(
    //             callInfo =>
    //             {
    //                 if (Interlocked.Increment(ref tries) is 2 or 5)
    //                     callInfo.ArgAt<Action<Exception>>(4).Invoke(new InvalidOperationException("Test"));
    //                 else
    //                     callInfo.ArgAt<Action<IBrokerMessageIdentifier?>>(3).Invoke(null);
    //             });
    //
    //     await _outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //     await AssertReceivedBlockingCallsAsync(_producer1, 2);
    //
    //     await _outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //     await AssertReceivedBlockingCallsAsync(_producer1, 5);
    //
    //     await _outboxWorker.ProcessOutboxAsync(CancellationToken.None);
    //     await AssertReceivedBlockingCallsAsync(_producer1, 12); // 10 messages + 2 retries
    //
    //     AssertReceivedCalls(_producer1, 0);
    // }
    [Fact]
    public async Task ProcessQueue_ShouldRetryAndEnforceOrder_WhenProduceThrows()
    {
        for (int i = 0; i < 10; i++)
        {
            await _outboxWriter.AddAsync(new OutboxMessage([0x01], null, _outboxEndpoint1));
        }

        int tries = 0;
        _producer1
            .When(
                producer => producer.RawProduceAsync(
                    Arg.Any<ProducerEndpoint>(),
                    Arg.Any<byte[]>(),
                    Arg.Any<IReadOnlyCollection<MessageHeader>>()))
            .Do(
                _ =>
                {
                    if (Interlocked.Increment(ref tries) is 2 or 5)
                        throw new InvalidOperationException("Test");
                });

        OutboxWorker outboxWorker = new(
            new OutboxWorkerSettings(new InMemoryOutboxSettings()) { EnforceMessageOrder = true },
            new InMemoryOutboxReader(_inMemoryOutbox),
            _producerCollection,
            _producerLogger);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await AssertReceivedBlockingCallsAsync(_producer1, 2);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await AssertReceivedBlockingCallsAsync(_producer1, 5);

        await outboxWorker.ProcessOutboxAsync(CancellationToken.None);
        await AssertReceivedBlockingCallsAsync(_producer1, 12); // 10 messages + 2 retries

        AssertReceivedCalls(_producer1, 0);
    }

    private static void AssertReceivedCalls(IProducer producer, int requiredNumberOfCalls) =>
        producer.Received(requiredNumberOfCalls).RawProduce(
            Arg.Any<ProducerEndpoint>(),
            Arg.Any<byte[]>(),
            Arg.Any<IReadOnlyCollection<MessageHeader>?>(),
            Arg.Any<Action<IBrokerMessageIdentifier?>>(),
            Arg.Any<Action<Exception>>());

    private static ValueTask<IBrokerMessageIdentifier?> AssertReceivedBlockingCallsAsync(IProducer producer, int requiredNumberOfCalls) =>
        producer.Received(requiredNumberOfCalls).RawProduceAsync(
            Arg.Any<ProducerEndpoint>(),
            Arg.Any<byte[]>(),
            Arg.Any<IReadOnlyCollection<MessageHeader>?>());
}
