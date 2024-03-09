// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ConsumerEndpointFixture
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsumeWithoutGroupId_WhenStaticallyAssigningAndNotCommitting()
    {
        int received = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .DisableOffsetsCommit()
                                .Consume(DefaultTopicName, endpoint => endpoint.ConsumeFrom(DefaultTopicName, 0, 1, 2))))
                .AddDelegateSubscriber<TestEventOne>(_ => Interlocked.Increment(ref received))
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 5);
        await Task.Delay(100);

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(5);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(5);

        Helper.ConsumerGroups.Should().HaveCount(1);
        Helper.ConsumerGroups.First().CommittedOffsets.Should().BeEmpty();
    }
}
