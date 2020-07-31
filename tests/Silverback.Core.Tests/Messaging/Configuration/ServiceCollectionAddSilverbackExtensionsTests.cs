// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Configuration
{
    public class ServiceCollectionAddSilverbackExtensionsTests
    {
        [Fact]
        public void AddSilverback_PublisherIsRegisteredAndWorking()
        {
            var servicesProvider = new ServiceCollection()
                .AddNullLogger()
                .AddSilverback()
                .Services.BuildServiceProvider();

            var publisher = servicesProvider.GetRequiredService<IPublisher>();

            publisher.Should().NotBeNull();

            Action act = () => publisher.Publish(new TestEventOne());

            act.Should().NotThrow();
        }
    }
}
