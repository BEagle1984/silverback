// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldKeepServiceScopeUntilStreamEnds()
    {
        ScopedStreamSubscriberResults results = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<ScopedService>()
            .AddSingleton(results)
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddScopedSubscriber<ScopedStreamSubscriber>());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        results.Messages.ShouldBe(["1", "2", "3"]);
        results.Errors.ShouldBeEmpty();
        ScopedService service = results.Service.ShouldNotBeNull();
        service.IsDisposed.ShouldBeFalse();

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => service.IsDisposed);

        service.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Streaming_ShouldDisposeServiceScope_WhenSubscriberFails()
    {
        ScopedStreamSubscriberResults results = new() { FailingMessage = "2" };

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<ScopedService>()
            .AddSingleton(results)
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddScopedSubscriber<ScopedStreamSubscriber>());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        ScopedService service = results.Service.ShouldNotBeNull();
        await AsyncTestingUtil.WaitAsync(() => service.IsDisposed);

        results.Messages.ShouldBe(["1", "2"]);
        results.Errors.ShouldBeEmpty();
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        service.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task Streaming_ShouldDisposeServiceScope_WhenSubscriberCannotBeCreated()
    {
        ScopedStreamSubscriberResults results = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddScoped<ScopedService>()
            .AddSingleton(results)
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddScopedSubscriber<FailingConstructorStreamSubscriber>());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);
        ScopedService service = results.Service.ShouldNotBeNull();
        await AsyncTestingUtil.WaitAsync(() => service.IsDisposed);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);
        service.IsDisposed.ShouldBeTrue();
    }

    private sealed class ScopedService : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class ScopedStreamSubscriberResults
    {
        public ScopedService? Service { get; set; }

        public string? FailingMessage { get; init; }

        public List<string?> Messages { get; } = [];

        public List<string> Errors { get; } = [];
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class ScopedStreamSubscriber
    {
        private readonly ScopedService _service;

        private readonly IServiceProvider _serviceProvider;

        private readonly ScopedStreamSubscriberResults _results;

        public ScopedStreamSubscriber(ScopedService service, IServiceProvider serviceProvider, ScopedStreamSubscriberResults results)
        {
            _service = service;
            _serviceProvider = serviceProvider;
            _results = results;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used by Silverback")]
        public async Task HandleAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            _results.Service = _service;

            await foreach (TestEventOne message in stream)
            {
                if (_service.IsDisposed)
                    _results.Errors.Add($"{message.ContentEventOne}: injected service disposed");

                if (_serviceProvider.GetRequiredService<ScopedService>() != _service)
                    _results.Errors.Add($"{message.ContentEventOne}: different scope");

                _results.Messages.Add(message.ContentEventOne);

                if (message.ContentEventOne == _results.FailingMessage)
                    throw new InvalidOperationException("Test");
            }
        }
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private sealed class FailingConstructorStreamSubscriber
    {
        public FailingConstructorStreamSubscriber(ScopedService service, ScopedStreamSubscriberResults results)
        {
            results.Service = service;
            throw new InvalidOperationException("Test");
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used by Silverback")]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Subscriber")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Subscriber signature")]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Subscriber signature")]
        public Task HandleAsync(IAsyncEnumerable<TestEventOne> stream) => Task.CompletedTask;
    }
}
