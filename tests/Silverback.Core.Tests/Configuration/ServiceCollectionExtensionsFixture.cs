// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Configuration
{
    public class ServiceCollectionExtensionsFixture
    {
        [Fact]
        public void AddSilverback_ShouldReturnSilverbackBuilder()
        {
            ServiceCollection serviceCollection = new();

            SilverbackBuilder builder = serviceCollection.AddSilverback();

            builder.Should().BeOfType<SilverbackBuilder>();
            builder.Services.Should().BeSameAs(serviceCollection);
        }

        [Fact]
        public void AddSilverback_ShouldRegisterBasicServices()
        {
            ServiceCollection serviceCollection = new();

            serviceCollection.AddLogging().AddSilverback();

            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            serviceProvider.GetService<BusOptions>().Should().BeOfType<BusOptions>();
            serviceProvider.GetService<IPublisher>().Should().BeOfType<Publisher>();
        }

        [Fact]
        public void AddSilverback_ShouldBeIdempotent()
        {
            ServiceCollection serviceCollection = new();

            serviceCollection.AddSilverback();
            serviceCollection.AddSilverback();

            serviceCollection.GetAll<BusOptions>().Should().HaveCount(1);
        }

        [Fact]
        public void ConfigureSilverback_ShouldReturnSilverbackBuilder()
        {
            ServiceCollection serviceCollection = new();

            SilverbackBuilder builder = serviceCollection.AddSilverback();

            builder.Should().BeOfType<SilverbackBuilder>();
            builder.Services.Should().BeSameAs(serviceCollection);
        }
    }
}
