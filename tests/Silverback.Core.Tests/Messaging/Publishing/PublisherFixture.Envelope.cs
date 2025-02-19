// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldPublishEnvelope()
    {
        List<object> messages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<IEnvelope>(Handle));

        void Handle(IEnvelope envelope) => messages.Add(envelope);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne()));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

        messages.Count.ShouldBe(2);
        messages.ShouldAllBe(message => message is TestEnvelope);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldUnwrapEnvelope()
    {
        List<object> messages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<TestCommandOne>(Handle));

        void Handle(TestCommandOne message) => messages.Add(message);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne()));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

        messages.Count.ShouldBe(2);
        messages.ShouldAllBe(message => message is TestCommandOne);
    }

    // This test simulates the case where the IRawInboundEnvelope isn't really an IEnvelope
    [Fact]
    public async Task PublishAndPublishAsync_ShouldCastEnvelope()
    {
        List<object> messages = [];
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddDelegateSubscriber<ITestRawEnvelope>(Handle));

        void Handle(ITestRawEnvelope envelope) => messages.Add(envelope);

        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();

        publisher.Publish(new TestEnvelope(new TestCommandOne()));
        await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

        messages.Count.ShouldBe(2);
        messages.ShouldAllBe(message => message is TestEnvelope);
    }
}
