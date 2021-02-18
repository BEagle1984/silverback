// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Silverback.Tests.Logging;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherEnvelopeTests
    {
        [Fact]
        public async Task Publish_Envelope_EnvelopeAndUnwrappedReceived()
        {
            var messages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((object message) => messages.Add(message))
                    .AddDelegateSubscriber((IEnvelope envelope) => messages.Add(envelope)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEnvelope(new TestCommandOne()));
            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Should().HaveCount(2);
            messages.OfType<TestCommandOne>().Should().HaveCount(2);
        }

        // This test simulates the case where the IRawInboundEnvelope isn't really an IEnvelope
        [Fact]
        public async Task Publish_Envelope_CastedEnvelopeReceived()
        {
            var messages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((ITestRawEnvelope envelope) => messages.Add(envelope)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEnvelope(new TestCommandOne()));
            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Should().HaveCount(2);
        }

        [Fact]
        public async Task Publish_EnvelopeWithoutAutoUnwrap_EnvelopeOnlyIsReceived()
        {
            var messages = new List<object>();
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddDelegateSubscriber((ICommand message) => messages.Add(message))
                    .AddDelegateSubscriber((IEnvelope envelope) => messages.Add(envelope)));
            var publisher = serviceProvider.GetRequiredService<IPublisher>();

            publisher.Publish(new TestEnvelope(new TestCommandOne(), false));
            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne(), false));

            messages.OfType<TestEnvelope>().Should().HaveCount(2);
            messages.OfType<TestCommandOne>().Should().BeEmpty();
        }

        [Fact]
        public async Task Publish_MessagesWithFilter_FilteredMessagesReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber<TestFilteredSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var filteredSubscriber = serviceProvider.GetRequiredService<TestFilteredSubscriber>();

            publisher.Publish(new TestEventOne { Message = "yes" });
            publisher.Publish(new TestEventOne { Message = "no" });
            await publisher.PublishAsync(new TestEventOne { Message = "yes" });
            await publisher.PublishAsync(new TestEventOne { Message = "no" });

            filteredSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task Publish_EnvelopesWithFilter_FilteredEnvelopesAndMessagesReceived()
        {
            var serviceProvider = ServiceProviderHelper.GetServiceProvider(
                services => services
                    .AddFakeLogger()
                    .AddSilverback()
                    .AddScopedSubscriber<TestFilteredSubscriber>());
            var publisher = serviceProvider.GetRequiredService<IPublisher>();
            var filteredSubscriber = serviceProvider.GetRequiredService<TestFilteredSubscriber>();

            publisher.Publish(new TestEnvelope(new TestEventOne { Message = "yes" }));
            publisher.Publish(new TestEnvelope(new TestEventOne { Message = "no" }));
            await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "yes" }));
            await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "no" }));

            filteredSubscriber.ReceivedEnvelopesCount.Should().Be(2);
            filteredSubscriber.ReceivedMessagesCount.Should().Be(2);
        }
    }
}
