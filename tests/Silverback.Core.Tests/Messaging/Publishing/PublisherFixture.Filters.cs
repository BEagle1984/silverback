// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing;

public partial class PublisherFixture
{
    [Fact]
    public async Task PublishAndPublishAsync_ShouldApplyMessageFilters()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<TestFilteredSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        TestFilteredSubscriber filteredSubscriber = serviceProvider.GetRequiredService<TestFilteredSubscriber>();

        publisher.Publish(new TestEventOne { Message = "yes" });
        publisher.Publish(new TestEventOne { Message = "no" });
        await publisher.PublishAsync(new TestEventOne { Message = "yes" });
        await publisher.PublishAsync(new TestEventOne { Message = "no" });

        filteredSubscriber.ReceivedMessages.Should().HaveCount(2);
    }

    [Fact]
    public async Task PublishAndPublishAsync_ShouldApplyEnvelopeFilters()
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddScopedSubscriber<TestFilteredSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        TestFilteredSubscriber filteredSubscriber = serviceProvider.GetRequiredService<TestFilteredSubscriber>();

        publisher.Publish(new TestEnvelope(new TestEventOne { Message = "yes" }));
        publisher.Publish(new TestEnvelope(new TestEventOne { Message = "no" }));
        await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "yes" }));
        await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "no" }));

        filteredSubscriber.ReceivedEnvelopes.Should().HaveCount(2);
        filteredSubscriber.ReceivedMessages.Should().HaveCount(2);
    }

    private sealed class EventOneFilterAttribute : MessageFilterAttribute
    {
        public override bool MustProcess(object message) =>
            message is TestEventOne { Message: "yes" } or IEnvelope { Message: TestEventOne { Message: "yes" } };
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private class TestFilteredSubscriber
    {
        public TestingCollection<IEvent> ReceivedMessages { get; } = new();

        public TestingCollection<IEnvelope> ReceivedEnvelopes { get; } = new();

        [EventOneFilter]
        [UsedImplicitly]
        public void OnMessageReceived(IEvent message) => ReceivedMessages.Add(message);

        [EventOneFilter]
        [UsedImplicitly]
        public void OnEnvelopeReceived(IEnvelope envelope) => ReceivedEnvelopes.Add(envelope);
    }
}
