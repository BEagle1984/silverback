// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class DependencyInjectionExtensionsTests
    {
        private IServiceProvider GetServiceProvider(Action<IServiceCollection> configAction)
        {
            var services = new ServiceCollection()
                .AddSingleton<ILoggerFactory, NullLoggerFactory>()
                .AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            configAction(services);

            return services.BuildServiceProvider();
        }

        [Fact]
        public void AddSilverback_PublisherIsRegisteredAndWorking()
        {
            var servicesProvider = GetServiceProvider(services => services
                .AddSilverback());

            var publisher = servicesProvider.GetRequiredService<IPublisher>();

            publisher.Should().NotBeNull();

            Action act = () => publisher.Publish(new TestEventOne());

            act.Should().NotThrow();
        }
    }
}