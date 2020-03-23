// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.HealthChecks;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.HealthChecks
{
    public class OutboundEndpointsHealthCheckServiceTests
    {
        [Fact]
        public async Task PingAllEndpoints_AllEndpointsWorking_EachEndpointIsPinged()
        {
            var broker = Substitute.For<IBroker>();
            broker.IsConnected.Returns(true);
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            var producer1 = Substitute.For<IProducer>();
            var producer2 = Substitute.For<IProducer>();
            var producer3 = Substitute.For<IProducer>();
            broker.GetProducer(new TestProducerEndpoint("endpoint1")).Returns(producer1);
            broker.GetProducer(new TestProducerEndpoint("endpoint2")).Returns(producer2);
            broker.GetProducer(new TestProducerEndpoint("endpoint3")).Returns(producer3);
            var configuration = Substitute.For<IOutboundRoutingConfiguration>();
            configuration.Routes.Returns(new List<IOutboundRoute>
            {
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventOne),
                    new TestProducerEndpoint("endpoint1"), typeof(OutboundConnector)),
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventTwo),
                    new TestProducerEndpoint("endpoint2"), typeof(OutboundConnector)),
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventThree),
                    new TestProducerEndpoint("endpoint3"), typeof(OutboundConnector)),
            });

            var service = new OutboundEndpointsHealthCheckService(configuration,
                new BrokerCollection(new[] { broker }));

            await service.PingAllEndpoints();

            await producer1.ReceivedWithAnyArgs(1).ProduceAsync(null);
            await producer2.ReceivedWithAnyArgs(1).ProduceAsync(null);
            await producer3.ReceivedWithAnyArgs(1).ProduceAsync(null);
        }

        [Fact]
        public async Task PingAllEndpoints_AllEndpointsWorking_ResultsAreAllSuccess()
        {
            var broker = Substitute.For<IBroker>();
            broker.IsConnected.Returns(true);
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            var producer1 = Substitute.For<IProducer>();
            var producer2 = Substitute.For<IProducer>();
            var producer3 = Substitute.For<IProducer>();
            broker.GetProducer(new TestProducerEndpoint("endpoint1")).Returns(producer1);
            broker.GetProducer(new TestProducerEndpoint("endpoint2")).Returns(producer2);
            broker.GetProducer(new TestProducerEndpoint("endpoint3")).Returns(producer3);
            var configuration = Substitute.For<IOutboundRoutingConfiguration>();
            configuration.Routes.Returns(new List<IOutboundRoute>
            {
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventOne),
                    new TestProducerEndpoint("endpoint1"), typeof(OutboundConnector)),
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventTwo),
                    new TestProducerEndpoint("endpoint2"), typeof(OutboundConnector)),
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventThree),
                    new TestProducerEndpoint("endpoint3"), typeof(OutboundConnector)),
            });

            var service = new OutboundEndpointsHealthCheckService(configuration,
                new BrokerCollection(new[] { broker }));

            var results = await service.PingAllEndpoints();

            results.ForEach(r => r.IsSuccessful.Should().BeTrue());
        }

        [Fact]
        public async Task PingAllEndpoints_SomeEndpointNotWorking_FailureIsProperlyReported()
        {
            var broker = Substitute.For<IBroker>();
            broker.IsConnected.Returns(true);
            broker.ProducerEndpointType.Returns(typeof(TestProducerEndpoint));
            broker.ConsumerEndpointType.Returns(typeof(TestConsumerEndpoint));
            var producer1 = Substitute.For<IProducer>();
            var producer2 = Substitute.For<IProducer>();
            producer2.ProduceAsync(null).ThrowsForAnyArgs<ProduceException>();
            var producer3 = Substitute.For<IProducer>();
            broker.GetProducer(new TestProducerEndpoint("endpoint1")).Returns(producer1);
            broker.GetProducer(new TestProducerEndpoint("endpoint2")).Returns(producer2);
            broker.GetProducer(new TestProducerEndpoint("endpoint3")).Returns(producer3);
            var configuration = Substitute.For<IOutboundRoutingConfiguration>();
            configuration.Routes.Returns(new List<IOutboundRoute>
            {
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventOne),
                    new TestProducerEndpoint("endpoint1"), typeof(OutboundConnector)),
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventTwo),
                    new TestProducerEndpoint("endpoint2"), typeof(OutboundConnector)),
                new OutboundRoutingConfiguration.OutboundRoute(typeof(TestEventThree),
                    new TestProducerEndpoint("endpoint3"), typeof(OutboundConnector)),
            });

            var service = new OutboundEndpointsHealthCheckService(configuration,
                new BrokerCollection(new[] { broker }));

            var results = (await service.PingAllEndpoints()).ToList();

            results[0].EndpointName.Should().Be("endpoint1");
            results[0].IsSuccessful.Should().BeTrue();
            results[0].ErrorMessage.Should().BeNull();

            results[1].EndpointName.Should().Be("endpoint2");
            results[1].IsSuccessful.Should().BeFalse();
            results[1].ErrorMessage.Should().NotBeNullOrEmpty();

            results[2].EndpointName.Should().Be("endpoint3");
            results[2].IsSuccessful.Should().BeTrue();
            results[2].ErrorMessage.Should().BeNull();
        }
    }
}