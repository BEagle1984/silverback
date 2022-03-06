﻿// Copyright (c) 2020 Sergio Aquilini
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
        public void GetMessageKey_NullMessage_NullReturned()
        {
            object? message = null;

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_MessageWithoutProperties_NullReturned()
        {
            object message = new { };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_NoKeyMembersMessage_NullReturned()
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
        public void GetMessageKey_SingleKeyMemberMessage_PropertyValueReturned()
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
        public void GetMessageKey_SingleKeyMemberMessage_NullReturned()
        {
            var message = new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = string.Empty,
                Two = "2",
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessageWithEmptyValues_NullReturned()
        {
            var message = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = string.Empty,
                Two = string.Empty,
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessageWithNullValues_NullReturned()
        {
            var message = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = null,
                Two = null,
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessagesWithSameKey_ComposedKeyReturned()
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
        public void GetMessageKey_MultipleKeyMembersMessagesSecondEmpty_ComposedKeyReturned()
        {
            var message = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = string.Empty,
                Two = "2",
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().Be("Two=2");
        }

        [Fact]
        public void GetMessageKey_DifferentMessagesMixture_CorrectKeyReturned()
        {
            // This is actually to test the cache.
            var message1 = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var message2 = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
            };

            var message3 = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = string.Empty,
            };

            var message4 = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = null,
                Two = "2"
            };

            var key1 = KafkaKeyHelper.GetMessageKey(message1);
            var key2 = KafkaKeyHelper.GetMessageKey(message2);
            var key3 = KafkaKeyHelper.GetMessageKey(message3);
            var key4 = KafkaKeyHelper.GetMessageKey(message4);

            key1.Should().Be("One=1,Two=2");
            key2.Should().Be("One=1,Two=2");
            key3.Should().Be("One=1");
            key4.Should().Be("Two=2");
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessagesWithOneKeyEmpty_OneKeyReturned()
        {
            var message = new MultipleKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = string.Empty,
                Three = "3"
            };

            var key = KafkaKeyHelper.GetMessageKey(message);

            key.Should().Be("One=1");
        }
    }
}
