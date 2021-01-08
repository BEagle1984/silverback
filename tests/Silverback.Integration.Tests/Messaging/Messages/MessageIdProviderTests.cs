// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class MessageIdProviderTests
    {
        [Fact]
        public void EnsureMessageIdIsInitialized_NoHeaderSet_HeaderInitialized()
        {
            var headers = new MessageHeaderCollection();

            MessageIdProvider.EnsureMessageIdIsInitialized(headers);

            headers.Should().HaveCount(1);
            headers[0].Name.Should().Be("x-message-id");
            headers[0].Value.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void EnsureMessageIdIsInitialized_IdHeaderAlreadySet_HeaderPreserved()
        {
            var headers = new MessageHeaderCollection
            {
                { "x-message-id", "12345" }
            };

            MessageIdProvider.EnsureMessageIdIsInitialized(headers);

            headers.Should().BeEquivalentTo(new MessageHeader("x-message-id", "12345"));
        }
    }
}
