// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Encryption;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Broker
{
    [Trait("Category", "E2E")]
    public class ErrorPoliciesTests
    {
        private readonly BusConfigurator _configurator;

        private readonly ServiceProvider _serviceProvider;
        private readonly SpyBrokerBehavior _spyBehavior;

        private static readonly byte[] AesEncryptionKey =
        {
            0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e,
            0x1f, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c
        };

        public ErrorPoliciesTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options
                    .AddInMemoryBroker()
                    .AddInMemoryChunkStore())
                .AddSingletonBrokerBehavior<SpyBrokerBehavior>();

            _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true
            });

            _configurator = _serviceProvider.GetRequiredService<BusConfigurator>();
            _spyBehavior = _serviceProvider.GetServices<IBrokerBehavior>().OfType<SpyBrokerBehavior>().First();
        }

        [Fact]
        public async Task RetryPolicy_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"),
                        policy => policy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.InboundEnvelopes.ForEach(envelope =>
                envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task RetryPolicy_OffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();
            var notCommittedOffsets = new List<IOffset>();

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            var tryCount = 0;

            var broker = _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"),
                        policy => policy.Retry().MaxFailedAttempts(10)))
                .First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);
            ((InMemoryConsumer) broker.Consumers.First()).RollbackCalled +=
                (_, offsetsCollection) => notCommittedOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            committedOffsets.Count.Should().Be(1);
            notCommittedOffsets.Count.Should().Be(0);
        }

        [Fact]
        public void RetryPolicyWithoutSuccess_OffsetNotCommitted()
        {
            var committedOffsets = new List<IOffset>();
            var notCommittedOffsets = new List<IOffset>();

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var broker = _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"),
                        policy => policy.Retry().MaxFailedAttempts(10)))
                .First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);
            ((InMemoryConsumer) broker.Consumers.First()).RollbackCalled +=
                (_, offsetsCollection) => notCommittedOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            Func<Task> act = () => publisher.PublishAsync(message);

            act.Should().Throw<ApplicationException>();

            committedOffsets.Count.Should().Be(0);
            notCommittedOffsets.Count.Should().Be(1);
        }

        [Fact]
        public async Task RetryAndSkipPolicies_OffsetCommitted()
        {
            var committedOffsets = new List<IOffset>();
            var notCommittedOffsets = new List<IOffset>();

            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            var broker = _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e"))
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e"),
                        policy => policy.Chain(policy.Retry().MaxFailedAttempts(10),
                            policy.Skip())))
                .First();

            ((InMemoryConsumer) broker.Consumers.First()).CommitCalled +=
                (_, offsetsCollection) => committedOffsets.AddRange(offsetsCollection);
            ((InMemoryConsumer) broker.Consumers.First()).RollbackCalled +=
                (_, offsetsCollection) => notCommittedOffsets.AddRange(offsetsCollection);

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            committedOffsets.Count.Should().Be(1);
            notCommittedOffsets.Count.Should().Be(0);
        }

        [Fact]
        public async Task EncryptionWithRetries_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };
            byte[] rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);
            var tryCount = 0;

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e")
                    {
                        Encryption = new SymmetricEncryptionSettings
                        {
                            Key = AesEncryptionKey
                        }
                    })
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")
                        {
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        },
                        policy => policy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(1);
            _spyBehavior.OutboundEnvelopes.First().RawMessage.Should().NotBeEquivalentTo(rawMessage);
            _spyBehavior.InboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.InboundEnvelopes.ForEach(envelope =>
                envelope.Message.Should().BeEquivalentTo(message));
        }

        [Fact]
        public async Task EncryptionAndChunkingWithRetries_RetriedMultipleTimes()
        {
            var message = new TestEventOne
            {
                Content = "Hello E2E!"
            };

            byte[] rawMessage = await Endpoint.DefaultSerializer.SerializeAsync(message,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            var tryCount = 0;

            _configurator
                .Subscribe((IIntegrationEvent _, IServiceProvider serviceProvider) =>
                {
                    tryCount++;
                    if (tryCount != 3)
                        throw new ApplicationException("Retry!");
                })
                .Connect(endpoints => endpoints
                    .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint("test-e2e")
                    {
                        Chunk = new ChunkSettings
                        {
                            Size = 10
                        },
                        Encryption = new SymmetricEncryptionSettings
                        {
                            Key = AesEncryptionKey
                        }
                    })
                    .AddInbound(new KafkaConsumerEndpoint("test-e2e")
                        {
                            Encryption = new SymmetricEncryptionSettings
                            {
                                Key = AesEncryptionKey
                            }
                        },
                        policy => policy.Retry().MaxFailedAttempts(10)));

            using var scope = _serviceProvider.CreateScope();
            
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(message);

            _spyBehavior.OutboundEnvelopes.Count.Should().Be(5);
            _spyBehavior.OutboundEnvelopes.First().RawMessage.Should().NotBeEquivalentTo(rawMessage.Take(10));
            _spyBehavior.OutboundEnvelopes.ForEach(envelope => envelope.RawMessage.Length.Should().BeLessOrEqualTo(10));
            _spyBehavior.InboundEnvelopes.Count.Should().Be(3);
            _spyBehavior.InboundEnvelopes.ForEach(envelope =>
                envelope.Message.Should().BeEquivalentTo(message));
        }
    }
}