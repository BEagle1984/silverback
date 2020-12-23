// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.RabbitMQ.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.RabbitMQ.Messaging.Messages
{
    public class RabbitRoutingKeyHelperTests
    {
        [Fact]
        public void GetRoutingKey_NoKeyAttribute_NullIsReturned()
        {
            var message = new NoRoutingKeyMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var key = RabbitRoutingKeyHelper.GetRoutingKey(message);

            key.Should().BeNull();
        }

        [Fact]
        public void GetRoutingKey_SingleKeyMemberMessage_PropertyValueIsReturned()
        {
            var message = new RoutingKeyMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            var key = RabbitRoutingKeyHelper.GetRoutingKey(message);

            key.Should().Be("1");
        }

        [Fact]
        public void GetRoutingKey_MultipleKeyAttributesWithSameKey_ExceptionIsThrown()
        {
            var message = new MultipleRoutingKeyAttributesMessage
            {
                Id = Guid.NewGuid(),
                One = "1",
                Two = "2",
                Three = "3"
            };

            Action act = () => RabbitRoutingKeyHelper.GetRoutingKey(message);

            act.Should().Throw<InvalidOperationException>();
        }
    }
}
