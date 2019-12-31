// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.Kafka.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Messages
{
    public class KafkaKeyHelperTests
    {
        [Fact]
        public void GetMessageKey_NoKeyMembersMessage_NullIsReturned()
        {
            var message = new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_SingleKeyMemberMessage_PropertyValueIsReturned()
        {
            var message = new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().Be("1");
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessagesWithSameKey_ComposedKeyIsReturned()
        {
            var message = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().Be("One=1,Two=2");
        }

        [Fact]
        public void GetMessageKey_LegacyKeyMemberAttribute_PropertyValueIsReturned()
        {
            var message = new LegacyKeyMemberAttributeMessage
            {
                Id = Guid.NewGuid(),
                One = "11",
                Two = "22",
                Three = "33"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().Be("11");
        }
    }
}