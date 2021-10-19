// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Publishing;

/// <summary>
///     The purpose of this class is to ensure that the publisher is still working when the broker
///     subscribers are added.
/// </summary>
public class PublisherTests
{
    [Fact]
    public void Publish_HandlersReturnValue_ResultsReturned()
    {
        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber<object>(_ => "response")
                .AddDelegateSubscriber<object>(_ => "response2"));

        IReadOnlyCollection<string> results = publisher.Publish<string>("test");

        results.Should().Equal("response", "response2");
    }

    [Fact]
    public async Task PublishAsync_HandlersReturnValue_ResultsReturned()
    {
        IPublisher publisher = GetPublisher(
            builder => builder
                .AddDelegateSubscriber<object>(_ => "response")
                .AddDelegateSubscriber<object>(_ => "response2"));

        IReadOnlyCollection<string> results = await publisher.PublishAsync<string>("test");

        results.Should().Equal("response", "response2");
    }

    private static IPublisher GetPublisher(Action<SilverbackBuilder> buildAction)
    {
        ServiceCollection services = new();

        services.AddLoggerSubstitute();

        SilverbackBuilder builder = services.AddSilverback();

        buildAction(builder);

        ServiceProvider? serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });

        return serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IPublisher>();
    }
}
