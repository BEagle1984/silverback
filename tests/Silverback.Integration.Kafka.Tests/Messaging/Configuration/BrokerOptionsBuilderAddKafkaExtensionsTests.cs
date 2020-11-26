// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration
{
    public class BrokerOptionsBuilderAddKafkaExtensionsTests
    {
        [Fact]
        public void AddBroker_GenericAndSpecificVersions_BehavesTheSame()
        {
            var servicesGeneric = new ServiceCollection();
            var servicesSpecific = new ServiceCollection();

            servicesGeneric
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options =>
                        options.AddBroker<KafkaBroker>());
            servicesSpecific
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options =>
                        options.AddKafka());

            servicesGeneric.Should().HaveCount(servicesSpecific.Count);
        }
    }
}
