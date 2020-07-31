// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class ServiceCollectionConfigureSilverbackExtensionsTests
    {
        [Fact]
        public void ConfigureSilverback_SilverbackBuilderReturned()
        {
            var services = new ServiceCollection();
            services.AddSilverback();

            var builder = services.ConfigureSilverback();

            builder.Should().NotBeNull();
        }

        [Fact]
        public void ConfigureSilverback_WorkingSilverbackBuilderReturned()
        {
            var services = new ServiceCollection();
            services.AddSilverback();

            var builder = services.ConfigureSilverback();

            builder.AddTransientSubscriber<TestSubscriber>();

            var provider = services.BuildServiceProvider();
            var subscriber = provider.GetService<TestSubscriber>();

            subscriber.Should().NotBeNull();
        }
    }
}
