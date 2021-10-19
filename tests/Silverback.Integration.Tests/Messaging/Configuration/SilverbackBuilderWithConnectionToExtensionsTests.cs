// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class SilverbackBuilderWithConnectionToExtensionsTests
    {
        [Fact]
        public void WithConnectionToMessageBroker_BrokerCollectionRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionToMessageBroker(_ => { })
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBrokerCollection>().Should().NotBeNull();
        }
    }
}
