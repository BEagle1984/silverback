// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Diagnostics
{
    public class MessageHeaderExtensionsTests
    {
        [Fact]
        public void GetFromHeaders_Successful()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader("key1", "value1"),
                new MessageHeader("key2", "value2"),
                new MessageHeader("key3", "value3")
            };

            var header = messageHeaders.GetFromHeaders("key1");
            Assert.Equal("value1", header);
        }

        [Fact]
        public void GetFromHeaders_ReturnsNull_IfValueIsNullOrEmpty()
        {
            IList<MessageHeader> messageHeaders = new List<MessageHeader>
            {
                new MessageHeader("key1", string.Empty),
                new MessageHeader("key2", "value2")
            };

            var header = messageHeaders.GetFromHeaders("key1");
            Assert.Null(header);
        }

        [Fact]
        public void GetFromHeaders_ReturnsNull_IfCollectionIsNull()
        {
            IList<MessageHeader> messageHeaders = null;
            var header = messageHeaders.GetFromHeaders("key1");
            Assert.Null(header);
        }
    }
}