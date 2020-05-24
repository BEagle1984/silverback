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
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E")]
    public class ConsumerScopeTests
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IBusConfigurator _configurator;

        public ConsumerScopeTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddScoped<ScopeIdentifier>()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker());

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

            _configurator = _serviceProvider.GetRequiredService<IBusConfigurator>();
        }

        [Fact]
        public async Task MultipleMessages_NewScopeCreatedForEachMessage()
        {
            var lastScopeId = Guid.Empty;
            var scopes = 0;

            var message = new TestEventOne { Content = "Hello E2E!" };

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    var newScopeId = serviceProvider.GetRequiredService<ScopeIdentifier>().ScopeId;
                    newScopeId.Should().NotBe(lastScopeId);
                    lastScopeId = newScopeId;
                    scopes++;
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e")));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);
            await publisher.PublishAsync(message);

            scopes.Should().Be(3);
        }

        [Fact]
        public async Task WithFailuresAndRetryPolicy_NewScopeCreatedForEachRetry()
        {
            var lastScopeId = Guid.Empty;
            var scopes = 0;

            var message = new TestEventOne { Content = "Hello E2E!" };

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    var newScopeId = serviceProvider.GetRequiredService<ScopeIdentifier>().ScopeId;
                    newScopeId.Should().NotBe(lastScopeId);
                    lastScopeId = newScopeId;
                    scopes++;

                    if (scopes != 3)
                        throw new InvalidOperationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(
                        new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(
                        new KafkaConsumerEndpoint("test-e2e"),
                        errorPolicy => errorPolicy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            scopes.Should().Be(3);
        }
    }
}