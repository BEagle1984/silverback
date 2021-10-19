// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Integration.TestTypes;

public class TestBrokerConfigurator : IBrokerOptionsConfigurator<TestBroker>
{
    public void Configure(BrokerOptionsBuilder brokerOptionsBuilder) =>
        brokerOptionsBuilder.SilverbackBuilder.AddSingletonBrokerBehavior<EmptyBehavior>();
}
