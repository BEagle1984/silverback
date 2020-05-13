// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Messages
{
    public class MessageHeaderTests
    {
        [Fact]
        public void Ctor_FillsProperties_IfParametersAreValid()
        {
            var messageHeader = new MessageHeader("key", "value");
            messageHeader.Name.Should().Be("key");
            messageHeader.Value.Should().Be("value");
        }

        [Fact]
        public void Ctor_Throws_IfKeyIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new MessageHeader(null, "value"));
        }

        [Fact]
        public void Key_Throws_IfValueIsNull()
        {
            var messageHeader = new MessageHeader("key", "value");
            Assert.Throws<ArgumentNullException>(() => messageHeader.Name = null);
        }

        [Fact]
        public void KeyValue_FillsProperties_IfParametersAreValid()
        {
            var messageHeader = new MessageHeader("key", "value");
            messageHeader.Name = "key1";
            messageHeader.Value = "value1";
            messageHeader.Name.Should().Be("key1");
            messageHeader.Value.Should().Be("value1");
        }
    }
}