// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
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
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            var message2 = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            var message2 = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void SerializeDeserialize_HardcodedType_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            Encoding.UTF8.GetString(serialized).Should().NotContain("TestEventOne");

            var message2 = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task SerializeDeserializeAsync_HardcodedType_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            Encoding.UTF8.GetString(serialized).Should().NotContain("TestEventOne");

            var message2 = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void Serialize_Message_TypeHeaderAdded()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            headers.Should().ContainEquivalentOf(new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
        }

        [Fact]
        public async Task SerializeAsync_Message_TypeHeaderAdded()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            headers.Should().ContainEquivalentOf(new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
        }

        [Fact]
        public void Serialize_ByteArray_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new JsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public void Serialize_ByteArrayWithHardcodedType_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = serializer.Serialize(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public async Task SerializeAsync_ByteArrayWithHardcodedType_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = await serializer.SerializeAsync(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public void Serialize_NullMessage_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessage_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public void Serialize_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = serializer
                .Serialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = await serializer
                .SerializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }
        
        [Fact]
        public void Deserialize_MessageWithIncompleteTypeHeader_CorrectlyDeserialized()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests"
                }
            };

            var serializer = new JsonMessageSerializer();

            var message = serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty) as TestEventOne;

            message.Should().NotBeNull();
            message.Should().BeOfType<TestEventOne>();
            message.As<TestEventOne>().Content.Should().Be("the message");
        }
        
        [Fact]
        public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_CorrectlyDeserialized()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests"
                }
            };

            var serializer = new JsonMessageSerializer();

            var message = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty) as TestEventOne;

            message.Should().NotBeNull();
            message.Should().BeOfType<TestEventOne>();
            message.As<TestEventOne>().Content.Should().Be("the message");
        }
        
        [Fact]
        public void Deserialize_NullMessage_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer();

            var deserialized = serializer
                .Deserialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }
        
        [Fact]
        public async Task DeserializeAsync_NullMessage_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer();

            var deserialized = await serializer
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var deserialized = serializer
                .Deserialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }
        
        [Fact]
        public async Task DeserializeAsync_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var deserialized = await serializer
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyArrayMessage_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer();

            var deserialized = serializer
                .Deserialize(new byte[0], new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessage_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer();

            var deserialized = await serializer
                .DeserializeAsync(new byte[0], new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyArrayMessageWithHardcodedType_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var deserialized = serializer
                .Deserialize(new byte[0], new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }
        
        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessageWithHardcodedType_NullIsReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var deserialized = await serializer
                .DeserializeAsync(new byte[0], new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }
    }
}