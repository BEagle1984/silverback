// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles
{
    public class BinaryFileMessageSerializerTests
    {
        [Fact]
        public void SerializeDeserialize_Message_CorrectlyDeserialized()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer();

            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            var (deserialized, _) = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as BinaryFileMessage;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer();

            var serialized = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            var (deserialized, _) = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as BinaryFileMessage;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void SerializeDeserialize_HardcodedType_CorrectlyDeserialized()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();
            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            var (deserialized, _) = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as BinaryFileMessage;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task SerializeDeserializeAsync_HardcodedType_CorrectlyDeserialized()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();
            var serialized = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            var (deserialized, _) = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as BinaryFileMessage;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void Serialize_Message_TypeHeaderAdded()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer();

            serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            var typeHeaderValue = headers["x-message-type"];
            typeHeaderValue.Should().NotBeNullOrEmpty();
            typeHeaderValue.Should()
                .StartWith("Silverback.Messaging.Messages.BinaryFileMessage, Silverback.Integration,");
        }

        [Fact]
        public async Task SerializeAsync_Message_TypeHeaderAdded()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer();

            await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            var typeHeaderValue = headers["x-message-type"];
            typeHeaderValue.Should().NotBeNullOrEmpty();
            typeHeaderValue.Should()
                .StartWith("Silverback.Messaging.Messages.BinaryFileMessage, Silverback.Integration,");
        }

        [Fact]
        public void Serialize_ByteArray_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new BinaryFileMessageSerializer();

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

            var serializer = new BinaryFileMessageSerializer();

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

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

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

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var serialized = await serializer.SerializeAsync(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public void Serialize_BinaryFileMessage_RawContentProduced()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var result = new BinaryFileMessageSerializer()
                .Serialize(message, headers, MessageSerializationContext.Empty);

            result.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public async Task SerializeAsync_BinaryFileMessage_RawContentProduced()
        {
            var message = new BinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var result = await new BinaryFileMessageSerializer()
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);

            result.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public void Serialize_InheritedBinaryFileMessage_RawContentProduced()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var result = new BinaryFileMessageSerializer()
                .Serialize(message, headers, MessageSerializationContext.Empty);

            result.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public async Task SerializeAsync_InheritedBinaryFileMessage_RawContentProduced()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var result = await new BinaryFileMessageSerializer()
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);

            result.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public void Serialize_NonBinaryFileMessage_ExceptionThrown()
        {
            var message = new TestEventOne
            {
                Content = "hey!"
            };
            var headers = new MessageHeaderCollection();

            Action act = () => new BinaryFileMessageSerializer()
                .Serialize(message, headers, MessageSerializationContext.Empty);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SerializeAsync_NonBinaryFileMessage_ExceptionThrown()
        {
            var message = new TestEventOne
            {
                Content = "hey!"
            };
            var headers = new MessageHeaderCollection();

            Action act = () => new BinaryFileMessageSerializer()
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Serialize_NullMessage_NullIsReturned()
        {
            var result = new BinaryFileMessageSerializer().Serialize(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            result.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessage_NullIsReturned()
        {
            var result = await new BinaryFileMessageSerializer().SerializeAsync(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            result.Should().BeNull();
        }

        [Fact]
        public void Serialize_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serialized = new BinaryFileMessageSerializer()
                .Serialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serialized = await new BinaryFileMessageSerializer()
                .SerializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_ByteArray_BinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var (deserializedObject, type) = new BinaryFileMessageSerializer()
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new BinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_ByteArray_BinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var (deserializedObject, type) = await new BinaryFileMessageSerializer()
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new BinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public void Deserialize_ByteArrayWithTypeHeader_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection
            {
                { "x-message-type", typeof(InheritedBinaryFileMessage).AssemblyQualifiedName! }
            };

            var (deserializedObject, type) = new BinaryFileMessageSerializer()
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_ByteArrayWithTypeHeader_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection
            {
                { "x-message-type", typeof(InheritedBinaryFileMessage).AssemblyQualifiedName! }
            };

            var (deserializedObject, type) = await new BinaryFileMessageSerializer()
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public void Deserialize_ByteArrayWithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var (deserializedObject, type) = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_ByteArrayWithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var (deserializedObject, type) = await new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public void Deserialize_NullMessage_BinaryFileWithNullContentReturned()
        {
            var (deserializedObject, type) = new BinaryFileMessageSerializer()
                .Deserialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new BinaryFileMessage
                {
                    Content = null
                });
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_BinaryFileWithNullContentReturned()
        {
            var (deserializedObject, type) = await new BinaryFileMessageSerializer()
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new BinaryFileMessage
                {
                    Content = null
                });
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public void Deserialize_EmptyMessage_BinaryFileWithEmptyContentReturned()
        {
            var (deserializedObject, type) = new BinaryFileMessageSerializer()
                .Deserialize(Array.Empty<byte>(), new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new BinaryFileMessage
                {
                    Content = Array.Empty<byte>()
                });
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyMessage_BinaryFileWithEmptyContentReturned()
        {
            var (deserializedObject, type) = await new BinaryFileMessageSerializer()
                .DeserializeAsync(
                    Array.Empty<byte>(),
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new BinaryFileMessage
                {
                    Content = Array.Empty<byte>()
                });
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public void Deserialize_WithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = serializer
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_WithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = await serializer
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = rawContent
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public void Deserialize_NullMessageWithHardcodedType_CustomBinaryFileReturned()
        {
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = serializer
                .Deserialize(null, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = null
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_NullMessageWithHardcodedType_CustomBinaryFileReturned()
        {
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = await serializer
                .DeserializeAsync(null, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = null
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public void Deserialize_EmptyMessageWithHardcodedType_CustomBinaryFileReturned()
        {
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = serializer
                .Deserialize(Array.Empty<byte>(), headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = Array.Empty<byte>()
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyMessageWithHardcodedType_CustomBinaryFileReturned()
        {
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = await serializer
                .DeserializeAsync(Array.Empty<byte>(), headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.Should().BeEquivalentTo(
                new InheritedBinaryFileMessage
                {
                    Content = Array.Empty<byte>()
                });
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public void Deserialize_BadTypeHeader_ExceptionThrown()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Tests"
                }
            };

            var serializer = new BinaryFileMessageSerializer();

            Action act = () => serializer
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void DeserializeAsync_BadTypeHeader_ExceptionThrown()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Tests"
                }
            };
            var serializer = new JsonMessageSerializer();

            Action act = () => serializer
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        private class InheritedBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}
