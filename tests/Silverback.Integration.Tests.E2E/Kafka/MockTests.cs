// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class MockTests : KafkaTestFixture
    {
        public MockTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task OutboundAndInbound_DefaultSettings_MessageTimestampIsSet()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(endpoint => endpoint.WithName("OUT").ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .WithName("IN")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Headers.GetValue<DateTime>(KafkaMessageHeaders.Timestamp)
                .Should().NotBeBefore(DateTime.Now.AddDays(-1));
        }

        [Fact]
        public async Task OutboundAndInbound_OverridingTimestamp_MessageTimestampIsSet()
        {
            DateTime date = new DateTime(1984, 06, 23);

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .WithName("OUT")
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader(KafkaMessageHeaders.Timestamp, date.ToString(CultureInfo.InvariantCulture)))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .WithName("IN")
                                        .Configure(
                                            config =>
                                            {
                                                config.GroupId = "consumer1";
                                            })))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Headers.GetValue<DateTime>(KafkaMessageHeaders.Timestamp)
                .Should().Be(date);
        }
    }
}
