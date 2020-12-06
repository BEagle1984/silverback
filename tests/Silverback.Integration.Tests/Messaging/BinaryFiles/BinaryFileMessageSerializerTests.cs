// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.BinaryFiles
{
    public class BinaryFileMessageSerializerTests
    {
        [Fact]
        public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
        {
            var message = new BinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
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
        public async Task SerializeDeserializeAsync_HardcodedType_CorrectlyDeserialized()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
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
        public async Task SerializeAsync_Message_TypeHeaderAdded()
        {
            var message = new BinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
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
        public async Task SerializeAsync_Stream_ReturnedUnmodified()
        {
            var messageStream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

            var serializer = new BinaryFileMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                messageStream,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageStream);
        }

        [Fact]
        public async Task SerializeAsync_StreamWithHardcodedType_ReturnedUnmodified()
        {
            var messageStream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var serialized = await serializer.SerializeAsync(
                messageStream,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageStream);
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

            serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
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

            serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
        }

        [Fact]
        public async Task SerializeAsync_BinaryFileMessage_RawContentProduced()
        {
            var message = new BinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
            };
            var headers = new MessageHeaderCollection();

            var result = await new BinaryFileMessageSerializer()
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);

            result.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public async Task SerializeAsync_InheritedBinaryFileMessage_RawContentProduced()
        {
            var message = new InheritedBinaryFileMessage
            {
                Content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 })
            };
            var headers = new MessageHeaderCollection();

            var result = await new BinaryFileMessageSerializer()
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);

            result.Should().BeEquivalentTo(message.Content);
        }

        [Fact]
        public void SerializeAsync_NonBinaryFileMessage_ExceptionThrown()
        {
            var message = new TestEventOne
            {
                Content = "hey!"
            };
            var headers = new MessageHeaderCollection();

            Func<Task> act = async () => await new BinaryFileMessageSerializer()
                .SerializeAsync(message, headers, MessageSerializationContext.Empty);

            act.Should().Throw<ArgumentException>();
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
        public async Task SerializeAsync_NullMessageWithHardcodedType_NullIsReturned()
        {
            var serialized = await new BinaryFileMessageSerializer()
                .SerializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_Stream_BinaryFileReturned()
        {
            var rawContent = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
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
        public async Task DeserializeAsync_ByteArrayWithTypeHeader_CustomBinaryFileReturned()
        {
            var rawContent = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
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
        public async Task DeserializeAsync_ByteArrayWithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
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
        public async Task DeserializeAsync_EmptyMessage_BinaryFileWithEmptyContentReturned()
        {
            var (deserializedObject, type) = await new BinaryFileMessageSerializer()
                .DeserializeAsync(
                    new MemoryStream(),
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<BinaryFileMessage>();
            deserializedObject.As<BinaryFileMessage>().Content.ReadAll()!.Length.Should().Be(0);
            type.Should().Be(typeof(BinaryFileMessage));
        }

        [Fact]
        public async Task DeserializeAsync_WithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
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
        public async Task DeserializeAsync_EmptyMessageWithHardcodedType_CustomBinaryFileReturned()
        {
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();

            var (deserializedObject, type) = await serializer
                .DeserializeAsync(new MemoryStream(), headers, MessageSerializationContext.Empty);

            deserializedObject.Should().BeOfType<InheritedBinaryFileMessage>();
            deserializedObject.As<BinaryFileMessage>().Content.ReadAll()!.Length.Should().Be(0);
            type.Should().Be(typeof(InheritedBinaryFileMessage));
        }

        [Fact]
        public void DeserializeAsync_BadTypeHeader_ExceptionThrown()
        {
            var rawContent = new MemoryStream(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Tests"
                }
            };
            var serializer = new JsonMessageSerializer();

            Func<Task> act = async () => await serializer
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        private class InheritedBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}
