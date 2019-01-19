// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Integration.Kafka.Tests.TestTypes.Messages;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Integration.Kafka.Tests.Messaging.Messages
{
    public class KeyHelperTests
    {
        [Fact]
        public void GetMessageKey_NoKeyMembersMessage_KeyIsEmpty()
        {
            var message = new NoKeyMembersMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var key = KeyHelper.GetMessageKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetMessageKey_SingleKeyMemberMessagesWithSameKey_KeyIsEqual()
        {
            var message1 = new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };
            var message2 = new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2-diff",
                Three = "3-diff"
            };

            var key1 = KeyHelper.GetMessageKey(message1);
            var key2 = KeyHelper.GetMessageKey(message2);

            key2.Should().BeEquivalentTo(key1);
        }

        [Fact]
        public void GetMessageKey_SingleKeyMemberMessagesWithDifferentKey_KeyIsNotEqual()
        {
            var message1 = new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };
            var message2 = new SingleKeyMemberMessage
            {
                Id = Guid.NewGuid(),
                One = "1-diff",
                Two = "2",
                Three = "3"
            };

            var key1 = KeyHelper.GetMessageKey(message1);
            var key2 = KeyHelper.GetMessageKey(message2);

            key2.Should().NotBeEquivalentTo(key1);
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessagesWithSameKey_KeyIsEqual()
        {
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
                Three = "3-diff"
            };

            var key1 = KeyHelper.GetMessageKey(message1);
            var key2 = KeyHelper.GetMessageKey(message2);

            key2.Should().BeEquivalentTo(key1);
        }

        [Fact]
        public void GetMessageKey_MultipleKeyMembersMessagesWithDifferentKey_KeyIsNotEqual()
        {
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
                Two = "2-diff",
                Three = "3"
            };

            var key1 = KeyHelper.GetMessageKey(message1);
            var key2 = KeyHelper.GetMessageKey(message2);

            key2.Should().NotBeEquivalentTo(key1);
        }
    }
}
