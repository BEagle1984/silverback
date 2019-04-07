// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using FluentAssertions;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization
{
    public class JsonMessageSerializerTests
    {
        [Fact]
        public void SerializeDeserialize_Message_CorrectlyDeserialized()
        {
            var message = new TestEventOne {Content = "the message"};

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(message);

            var message2 = serializer.Deserialize(serialized) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Content.Should().Be(message.Content);
        }

        [Fact]
        public void SerializeDeserialize_HardcodedType_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = serializer.Serialize(message);

            Encoding.UTF8.GetString(serialized).Should().NotContain("TestEventOne");

            var message2 = serializer.Deserialize(serialized) as TestEventOne;
            
            message2.Should().NotBeNull();
            message2.Content.Should().Be(message.Content);
        }
    }
}
