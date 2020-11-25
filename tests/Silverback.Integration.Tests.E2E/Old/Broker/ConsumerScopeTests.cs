// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Old.Broker
{
    public class ConsumerScopeTests : E2ETestFixture
    {
        public ConsumerScopeTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact(Skip = "Deprecated")]
        public async Task MultipleMessages_NewScopeCreatedForEachMessage()
        {
            var lastScopeId = Guid.Empty;
            var scopes = 0;

            var message = new TestEventOne { Content = "Hello E2E!" };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddScoped<ScopeIdentifier>()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(new KafkaConsumerEndpoint("test-e2e")))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _, IServiceProvider localServiceProvider) =>
                            {
                                var newScopeId = localServiceProvider.GetRequiredService<ScopeIdentifier>().ScopeId;
                                newScopeId.Should().NotBe(lastScopeId);
                                lastScopeId = newScopeId;
                                scopes++;
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);

            scopes.Should().Be(3);
        }

        [Fact(Skip = "Deprecated")]
        public async Task WithFailuresAndRetryPolicy_NewScopeCreatedForEachRetry()
        {
            var lastScopeId = Guid.Empty;
            var scopes = 0;

            var message = new TestEventOne { Content = "Hello E2E!" };

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddScoped<ScopeIdentifier>()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                                .AddInbound(
                                    new KafkaConsumerEndpoint("test-e2e")
                                    {
                                        ErrorPolicy = ErrorPolicy.Retry().MaxFailedAttempts(10)
                                    }))
                        .AddDelegateSubscriber(
                            (IIntegrationEvent _, IServiceProvider localServiceProvider) =>
                            {
                                var newScopeId = localServiceProvider.GetRequiredService<ScopeIdentifier>().ScopeId;
                                newScopeId.Should().NotBe(lastScopeId);
                                lastScopeId = newScopeId;
                                scopes++;

                                if (scopes != 3)
                                    throw new InvalidOperationException("Retry!");
                            }))
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            scopes.Should().Be(3);
        }
    }
}
