// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.Routing
{
    public class OutboxProduceStrategyTests
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly TransactionalOutboxBroker _mockTransactionalOutboxBroker;

        private readonly IOutboundEnvelope _message1;

        private readonly IOutboundEnvelope _message2;

        private readonly TestProducerEndpoint _endpoint1;

        private readonly TestProducerEndpoint _endpoint2;

        public OutboxProduceStrategyTests()
        {
            ServiceProvider serviceProvider = CreateServiceProviderRequiredForTransactionalOutboxBroker();
            _mockTransactionalOutboxBroker = Substitute.For<TransactionalOutboxBroker>(Substitute.For<IOutboxWriter>(), serviceProvider);

            var services = new ServiceCollection();
            services
                .AddScoped(_ => Substitute.For<IOutboundLogger<OutboxProduceStrategy>>())
                .AddScoped(_ => _mockTransactionalOutboxBroker);

            _serviceProvider = services.BuildServiceProvider();

            _message1 = Substitute.For<IOutboundEnvelope>();
            _endpoint1 = new TestProducerEndpoint("endpoint1");
            _message1.Endpoint
                .Returns(_endpoint1);

            _message2 = Substitute.For<IOutboundEnvelope>();
            _endpoint2 = new TestProducerEndpoint("endpoint2");
            _message2.Endpoint
                .Returns(_endpoint2);
        }

        [Fact]
        public async Task ProduceAsync_WithMultipleMessagesWithDifferentEndpoints_ShouldFindAndProduceToCorrectProducers()
        {
            // Arrange
            var outboxProduceStrategyImplementation = new OutboxProduceStrategy().Build(_serviceProvider);
            var producerForMessage1 = Substitute.For<IProducer>();
            producerForMessage1.Endpoint.Returns(_endpoint1);
            _mockTransactionalOutboxBroker.GetProducer(_message1.Endpoint)
                .Returns(producerForMessage1);

            var producerForMessage2 = Substitute.For<IProducer>();
            producerForMessage2.Endpoint.Returns(_endpoint2);
            _mockTransactionalOutboxBroker.GetProducer(_message2.Endpoint)
                .Returns(producerForMessage2);

            // Act
            await outboxProduceStrategyImplementation.ProduceAsync(_message1);
            await outboxProduceStrategyImplementation.ProduceAsync(_message2);

            // Assert
            await producerForMessage1.Received().ProduceAsync(_message1);
            await producerForMessage2.Received().ProduceAsync(_message2);
        }

        [Fact]
        public async Task ProduceAsync_WithMultipleMessagesWithSameEndpoints_ShouldFindAndProduceToCorrectProducers()
        {
            // Arrange
            var outboxProduceStrategyImplementation = new OutboxProduceStrategy().Build(_serviceProvider);
            var producerForMessage1 = Substitute.For<IProducer>();
            producerForMessage1.Endpoint.Returns(_endpoint1);
            _mockTransactionalOutboxBroker.GetProducer(_message1.Endpoint)
                .Returns(producerForMessage1);

            var messageWithSameEndpoint = Substitute.For<IOutboundEnvelope>();
            messageWithSameEndpoint.Endpoint.Returns(_endpoint1);

            await outboxProduceStrategyImplementation.ProduceAsync(_message1);

            // Act
            _mockTransactionalOutboxBroker.ClearReceivedCalls();
            await outboxProduceStrategyImplementation.ProduceAsync(messageWithSameEndpoint);

            // Assert
            await producerForMessage1.Received(1).ProduceAsync(_message1);
            _mockTransactionalOutboxBroker.DidNotReceive().GetProducer(Arg.Any<IProducerEndpoint>());
        }

        private static ServiceProvider CreateServiceProviderRequiredForTransactionalOutboxBroker()
        {
            var services = new ServiceCollection();
            services.AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddBroker<TestBroker>());

            services.AddSingleton(Substitute.For<IHostApplicationLifetime>())
                .AddLoggerSubstitute();

            var serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
