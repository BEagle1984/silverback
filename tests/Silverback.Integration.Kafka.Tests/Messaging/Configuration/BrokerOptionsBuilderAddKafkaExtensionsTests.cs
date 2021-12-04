// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration;

public class BrokerOptionsBuilderAddKafkaExtensionsTests
{
    [Fact]
    public void AddBroker_GenericAndSpecificVersions_BehavesTheSame()
    {
        ServiceCollection servicesGeneric = new();
        ServiceCollection servicesSpecific = new();

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
