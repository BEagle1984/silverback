// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class PublisherEnvelopeTests
    {
        [Fact]
        public void Publish_Envelope_EnvelopeAndUnwrappedReceived()
        {
            var messages = new List<object>();
            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((object message) => messages.Add(message))
                    .AddDelegateSubscriber((IEnvelope envelope) => messages.Add(envelope)));

            publisher.Publish(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Should().HaveCount(1);
            messages.OfType<TestCommandOne>().Should().HaveCount(1);
        }

        // This test simulates the case where the IRawInboundEnvelope isn't really an IEnvelope
        [Fact]
        public void Publish_Envelope_CastedEnvelopeReceived()
        {
            var messages = new List<object>();
            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((ITestRawEnvelope envelope) => messages.Add(envelope)));

            publisher.Publish(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Should().HaveCount(1);
        }

        [Fact]
        public async Task PublishAsync_Envelope_EnvelopeAndUnwrappedReceived()
        {
            var messages = new List<object>();
            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((object message) => messages.Add(message))
                    .AddDelegateSubscriber((IEnvelope envelope) => messages.Add(envelope)));

            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne()));

            messages.OfType<TestEnvelope>().Should().HaveCount(1);
            messages.OfType<TestCommandOne>().Should().HaveCount(1);
        }

        [Fact]
        public void Publish_EnvelopeWithoutAutoUnwrap_EnvelopeOnlyIsReceived()
        {
            var messages = new List<object>();
            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((ICommand message) => messages.Add(message))
                    .AddDelegateSubscriber((IEnvelope envelope) => messages.Add(envelope)));

            publisher.Publish(new TestEnvelope(new TestCommandOne(), false));

            messages.OfType<TestEnvelope>().Should().HaveCount(1);
            messages.OfType<TestCommandOne>().Should().BeEmpty();
        }

        [Fact]
        public async Task PublishAsync_EnvelopeWithoutAutoUnwrap_EnvelopeOnlyIsReceived()
        {
            var messages = new List<object>();
            var publisher = PublisherTestsHelper.GetPublisher(
                builder => builder
                    .AddDelegateSubscriber((ICommand message) => messages.Add(message))
                    .AddDelegateSubscriber((IEnvelope envelope) => messages.Add(envelope)));

            await publisher.PublishAsync(new TestEnvelope(new TestCommandOne(), false));

            messages.OfType<TestEnvelope>().Should().HaveCount(1);
            messages.OfType<TestCommandOne>().Should().BeEmpty();
        }

        [Fact]
        public async Task Publish_MessagesWithFilter_FilteredMessagesReceived()
        {
            var filteredSubscriber = new TestFilteredSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(filteredSubscriber);

            publisher.Publish(new TestEventOne { Message = "yes" });
            publisher.Publish(new TestEventOne { Message = "no" });
            await publisher.PublishAsync(new TestEventOne { Message = "yes" });
            await publisher.PublishAsync(new TestEventOne { Message = "no" });

            filteredSubscriber.ReceivedMessagesCount.Should().Be(2);
        }

        [Fact]
        public async Task Publish_EnvelopesWithFilter_FilteredEnvelopesAndMessagesReceived()
        {
            var filteredSubscriber = new TestFilteredSubscriber();
            var publisher = PublisherTestsHelper.GetPublisher(filteredSubscriber);

            publisher.Publish(new TestEnvelope(new TestEventOne { Message = "yes" }));
            publisher.Publish(new TestEnvelope(new TestEventOne { Message = "no" }));
            await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "yes" }));
            await publisher.PublishAsync(new TestEnvelope(new TestEventOne { Message = "no" }));

            filteredSubscriber.ReceivedEnvelopesCount.Should().Be(2);
            filteredSubscriber.ReceivedMessagesCount.Should().Be(2);
        }
    }
}
