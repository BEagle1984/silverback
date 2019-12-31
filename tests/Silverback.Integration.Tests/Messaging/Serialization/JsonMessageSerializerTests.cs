// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization
{
    public class JsonMessageSerializerTests
    {
        // TODO: Properly test added headers!

        [Fact]
        public void SerializeDeserialize_Message_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(message, headers);

            var message2 = serializer.Deserialize(serialized, headers) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void SerializeDeserialize_HardcodedType_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = serializer.Serialize(message, new MessageHeaderCollection());

            Encoding.UTF8.GetString(serialized).Should().NotContain("TestEventOne");

            var message2 = serializer.Deserialize(serialized, new MessageHeaderCollection()) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void Serialize_ByteArray_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(messageBytes, new MessageHeaderCollection());

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        // This is necessary for backward compatibility with messages generated with version <= 0.10
        public void Deserialize_NoTypeHeader_MessageDeserializedByEmbeddedTypeInformation()
        {
            var original = new TestEventOne { Id = Guid.NewGuid(), Content = "abcd" };
            var json = JsonConvert.SerializeObject(original,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
            var buffer = Encoding.UTF8.GetBytes(json);

            var serializer = new JsonMessageSerializer();

            var deserialized = serializer.Deserialize(buffer, new MessageHeaderCollection());

            deserialized.Should().NotBeNull();
            deserialized.Should().BeEquivalentTo(original);
        }
    }
}