// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.LargeMessages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class ProducerTests
    {
        private readonly TestBroker _broker;

        public ProducerTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddBroker<TestBroker>());

            var serviceProvider = services.BuildServiceProvider();

            _broker = serviceProvider.GetRequiredService<TestBroker>();
        }

        [Fact]
        public void Produce_IntegrationMessage_IdIsSet()
        {
            var producer = _broker.GetProducer(TestProducerEndpoint.GetDefault());

            var message = new TestEventOne();

            producer.Produce(message);

            message.Id.Should().NotBeEmpty();
        }

        [Fact]
        public async Task ProduceAsync_IntegrationMessage_IdIsSet()
        {
            var producer = _broker.GetProducer(TestProducerEndpoint.GetDefault());

            var message = new TestEventOne();

            await producer.ProduceAsync(message);

            message.Id.Should().NotBeEmpty();
        }

        [Fact]
        public void Produce_LargeMessage_Chunked()
        {
            var producer = (TestProducer) _broker.GetProducer(new TestProducerEndpoint("point")
            {
                Chunk = new ChunkSettings
                {
                    Size = 10
                }
            });

            var message = new TestEventOne { Content = "abcdefghijklmnopqrstuvwxyz", Id = Guid.NewGuid() };

            producer.Produce(message);

            producer.ProducedMessages.Count.Should().Be(9);
        }

        [Fact]
        public void ProduceAsync_LargeMessage_Chunked()
        {
            var producer = (TestProducer) _broker.GetProducer(new TestProducerEndpoint("point")
            {
                Chunk = new ChunkSettings
                {
                    Size = 10
                }
            });

            var message = new TestEventOne { Content = "abcdefghijklmnopqrstuvwxyz", Id = Guid.NewGuid() };

            producer.Produce(message);

            producer.ProducedMessages.Count.Should().Be(9);
        }
    }
}