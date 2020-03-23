// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestBrokerConfigurator : IBrokerOptionsConfigurator<TestBroker>
    {
        public void Configure(IBrokerOptionsBuilder options) =>
            options.SilverbackBuilder.AddSingletonBrokerBehavior<EmptyBehavior>();
    }
}