// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
        IServiceProvider serviceProvider = ServiceProviderHelper.GetServiceProvider(
            services => services
                .AddFakeLogger()
                .AddSilverback()
                .AddSingletonSubscriber<TestFilteredSubscriber>());
        IPublisher publisher = serviceProvider.GetRequiredService<IPublisher>();
        TestFilteredSubscriber filteredSubscriber = serviceProvider.GetRequiredService<TestFilteredSubscriber>();

        publisher.Publish(new TestEventOne { Message = "yes" });
        publisher.Publish(new TestEventOne { Message = "no" });
        await publisher.PublishAsync(new TestEventOne { Message = "yes" });
        await publisher.PublishAsync(new TestEventOne { Message = "no" });

        filteredSubscriber.ReceivedMessages.Count.ShouldBe(2);
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

        filteredSubscriber.ReceivedEnvelopes.Count.ShouldBe(2);
        filteredSubscriber.ReceivedMessages.Count.ShouldBe(2);
    }

    private sealed class EventOneFilterAttribute : MessageFilterAttribute
    {
        public override bool MustProcess(object message) =>
            message is TestEventOne { Message: "yes" } or IEnvelope { Message: TestEventOne { Message: "yes" } };
    }

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Class used via DI")]
    private class TestFilteredSubscriber
    {
        public TestingCollection<IEvent> ReceivedMessages { get; } = [];

        public TestingCollection<IEnvelope> ReceivedEnvelopes { get; } = [];

        [EventOneFilter]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public void OnMessageReceived(IEvent message) => ReceivedMessages.Add(message);

        [EventOneFilter]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Invoked via reflection")]
        public void OnEnvelopeReceived(IEnvelope envelope) => ReceivedEnvelopes.Add(envelope);
    }
}
