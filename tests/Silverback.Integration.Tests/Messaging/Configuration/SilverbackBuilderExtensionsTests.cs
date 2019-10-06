// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class SilverbackBuilderExtensionsTests
    {
        [Fact]
        public void WithConnectionTo_BrokerRegisteredForDI()
        {
            var serviceProvider = new ServiceCollection()
                .AddSilverback()
                .WithConnectionTo<TestBroker>(options => { })
                .Services.BuildServiceProvider();

            serviceProvider.GetService<IBroker>().Should().NotBeNull();
        }
    }
}