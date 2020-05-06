// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        private static MessageHeaderCollection _testEventOneMessageTypeHeaders = new MessageHeaderCollection
        {
            {
                "x-message-type",
                "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests"
            }
        };

        [Fact]
        public void SerializeDeserialize_Message_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            var (deserialized, _) = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as TestEventOne;

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

            var (deserialized, _) = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as TestEventOne;

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

            var (deserialized, _) = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as TestEventOne;

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

            var (deserialized, _) = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as TestEventOne;

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

            headers.Should().ContainEquivalentOf(
                new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
        }

        [Fact]
        public async Task SerializeAsync_Message_TypeHeaderAdded()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            headers.Should().ContainEquivalentOf(
                new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
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
        public void Serialize_NullMessage_NullReturned()
        {
            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessage_NullReturned()
        {
            var serializer = new JsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public void Serialize_NullMessageWithHardcodedType_NullReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = serializer
                .Serialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessageWithHardcodedType_NullReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var serialized = await serializer
                .SerializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_MessageWithIncompleteTypeHeader_Deserialized()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");

            var (deserializedObject, _) = serializer
                .Deserialize(rawMessage, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            var message = deserializedObject as TestEventOne;

            message.Should().NotBeNull();
            message.Should().BeOfType<TestEventOne>();
            message.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public void Deserialize_MessageWithIncompleteTypeHeader_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");

            var (_, type) = serializer
                .Deserialize(rawMessage, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_Deserialized()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(rawMessage, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            var message = deserializedObject as TestEventOne;

            message.Should().NotBeNull();
            message.Should().BeOfType<TestEventOne>();
            message.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_MissingTypeHeader_ExceptionThrown()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            Action act = () => serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<MessageSerializerException>();
        }

        [Fact]
        public void DeserializeAsync_MissingTypeHeader_ExceptionThrown()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            Action act = () => serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<MessageSerializerException>();
        }

        [Fact]
        public void Deserialize_BadTypeHeader_ExceptionThrown()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Tests"
                }
            };

            var serializer = new JsonMessageSerializer();

            Action act = () => serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void DeserializeAsync_BadTypeHeader_ExceptionThrown()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Tests"
                }
            };
            var serializer = new JsonMessageSerializer();

            Action act = () => serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void Deserialize_MissingTypeHeaderWithHardcodedType_Deserialized()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().NotBeNull();
            deserializedObject.Should().BeOfType<TestEventOne>();
            deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public void Deserialize_MissingTypeHeaderWithHardcodedType_TypeReturned()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_MissingTypeHeaderWithHardcodedType_Deserialized()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().NotBeNull();
            deserializedObject.Should().BeOfType<TestEventOne>();
            deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public async Task DeserializeAsync_MissingTypeHeaderWithHardcodedType_TypeReturned()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_WrongTypeHeaderWithHardcodedType_Deserialized()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().NotBeNull();
            deserializedObject.Should().BeOfType<TestEventOne>();
            deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public void Deserialize_WrongTypeHeaderWithHardcodedType_TypeReturned()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_WrongTypeHeaderWithHardcodedType_Deserialized()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().NotBeNull();
            deserializedObject.Should().BeOfType<TestEventOne>();
            deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public async Task DeserializeAsync_WrongTypeHeaderWithHardcodedType_TypeReturned()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{ 'Content': 'the message' }");
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_NullMessage_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (deserializedObject, _) = serializer
                .Deserialize(null, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public void Deserialize_NullMessage_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (_, type) = serializer
                .Deserialize(null, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(null, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (_, type) = await serializer
                .DeserializeAsync(null, _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_NullMessageWithHardcodedType_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = serializer
                .Deserialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public void Deserialize_NullMessageWithHardcodedType_TypeReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = serializer
                .Deserialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_NullMessageWithHardcodedType_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessageWithHardcodedType_TypeReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_EmptyArrayMessage_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (deserializedObject, _) = serializer
                .Deserialize(Array.Empty<byte>(), _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyArrayMessage_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (_, type) = serializer
                .Deserialize(Array.Empty<byte>(), _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessage_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(Array.Empty<byte>(), _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessage_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (_, type) = await serializer
                .DeserializeAsync(Array.Empty<byte>(), _testEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }
        
        [Fact]
        public void Deserialize_EmptyArrayMessageWithHardcodedType_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = serializer
                .Deserialize(Array.Empty<byte>(), new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyArrayMessageWithHardcodedType_TypeReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = serializer
                .Deserialize(Array.Empty<byte>(), new MessageHeaderCollection(), MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessageWithHardcodedType_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(
                    Array.Empty<byte>(),
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessageWithHardcodedType_TypeReturned()
        {
            var serializer = new JsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(
                    Array.Empty<byte>(),
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }
    }
}
