// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class TestingHelperTests : MqttTests
{
    public TestingHelperTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitAllTopicsAndPartitions()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                    .Produce<TestEventThree>(endpoint => endpoint.ProduceTo("topic3"))
                    .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
            .AddDelegateSubscriber<IIntegrationEvent>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitSpecifiedTopicsOnly()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(30) // needed to avoid deadlocks
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("topic2"))
                    .Produce<TestEventThree>(endpoint => endpoint.ProduceTo("topic3"))
                    .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
            .AddDelegateSubscriber<TestEventOne>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddDelegateSubscriber<TestEventTwo>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddDelegateSubscriber<TestEventThree>(_ => taskCompletionSource.Task)
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        try
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync("topic1", "topic2");

            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("topic1").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("topic2").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("topic3").ShouldBe(10);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitSpecifiedFriendlyEndpointNamesOnly()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(30) // needed to avoid deadlocks
                    .Produce<TestEventOne>("one", endpoint => endpoint.ProduceTo("topic1"))
                    .Produce<TestEventTwo>("two", endpoint => endpoint.ProduceTo("topic2"))
                    .Produce<TestEventThree>("three", endpoint => endpoint.ProduceTo("topic3"))
                    .Consume(endpoint => endpoint.ConsumeFrom("topic1", "topic2", "topic3"))))
            .AddDelegateSubscriber<TestEventOne>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddDelegateSubscriber<TestEventTwo>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddDelegateSubscriber<TestEventThree>(_ => taskCompletionSource.Task)
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        try
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync("one", "two");

            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("topic1").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("topic2").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("topic3").ShouldBe(10);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }

    [Theory]
    [InlineData("readings")]
    [InlineData("sensors/+/readings")]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitTopicsMatchingConsumerEndpointFilters(string endpointName)
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(30) // needed to avoid deadlocks
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("sensors/1/readings"))
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("sensors/2/readings"))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("alerts/1"))
                    .Consume<TestEventOne>("readings", endpoint => endpoint.ConsumeFrom("sensors/+/readings"))
                    .Consume<TestEventTwo>("alerts", endpoint => endpoint.ConsumeFrom("alerts/#"))))
            .AddDelegateSubscriber<TestEventOne>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddDelegateSubscriber<TestEventTwo>(_ => taskCompletionSource.Task)
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        try
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync(endpointName);

            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("sensors/1/readings").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("sensors/2/readings").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("alerts/1").ShouldBe(10);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitTopicMatchingConsumerEndpointFilter()
    {
        TaskCompletionSource taskCompletionSource = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .EnableParallelProcessing(30) // needed to avoid deadlocks
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(message => $"sensors/{message?.ContentEventOne}/readings"))
                    .Produce<TestEventTwo>(endpoint => endpoint.ProduceTo("alerts/1"))
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("sensors/+/readings"))
                    .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("alerts/#"))))
            .AddDelegateSubscriber<TestEventOne>(_ => Task.Delay(Random.Shared.Next(5, 50)))
            .AddDelegateSubscriber<TestEventTwo>(_ => taskCompletionSource.Task)
            .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 10; i++)
        {
            await publisher.PublishAsync(new TestEventOne { ContentEventOne = "1" });
            await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        try
        {
            await Helper.WaitUntilAllMessagesAreConsumedAsync("sensors/1/readings");

            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("sensors/1/readings").ShouldBe(0);
            Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount("alerts/1").ShouldBe(10);
        }
        finally
        {
            taskCompletionSource.SetResult();
        }
    }
}
