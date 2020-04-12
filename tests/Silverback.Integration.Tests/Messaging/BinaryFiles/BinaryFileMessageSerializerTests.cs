// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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

            var message2 = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty) as BinaryFileMessage;

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

            var message2 = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty) as BinaryFileMessage;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public void SerializeDeserialize_HardcodedType_CorrectlyDeserialized()
        {
            var message = new InheritedBinaryFileMessage()
            {
                Content = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }
            };
            var headers = new MessageHeaderCollection();

            var serializer = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>();
            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            var message2 = serializer
                .Deserialize(serialized, headers, MessageSerializationContext.Empty) as BinaryFileMessage;

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

            var message2 = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty) as BinaryFileMessage;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
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

            var result = new BinaryFileMessageSerializer()
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            result.Should().BeOfType<BinaryFileMessage>();
            result.Should().BeEquivalentTo(new BinaryFileMessage
            {
                Content = rawContent
            });
        }

        [Fact]
        public async Task DeserializeAsync_ByteArray_BinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var result = await new BinaryFileMessageSerializer()
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            result.Should().BeOfType<BinaryFileMessage>();
            result.Should().BeEquivalentTo(new BinaryFileMessage
            {
                Content = rawContent
            });
        }

        [Fact]
        public void Deserialize_ByteArrayWithTypeHeader_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection
            {
                { "x-message-type", typeof(InheritedBinaryFileMessage).AssemblyQualifiedName }
            };

            var result = new BinaryFileMessageSerializer()
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            result.Should().BeOfType<InheritedBinaryFileMessage>();
            result.Should().BeEquivalentTo(new InheritedBinaryFileMessage()
            {
                Content = rawContent
            });
        }

        [Fact]
        public async Task DeserializeAsync_ByteArrayWithTypeHeader_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection
            {
                { "x-message-type", typeof(InheritedBinaryFileMessage).AssemblyQualifiedName }
            };

            var result = await new BinaryFileMessageSerializer()
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            result.Should().BeOfType<InheritedBinaryFileMessage>();
            result.Should().BeEquivalentTo(new InheritedBinaryFileMessage
            {
                Content = rawContent
            });
        }

        [Fact]
        public void Deserialize_ByteArrayWithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var result = new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
                .Deserialize(rawContent, headers, MessageSerializationContext.Empty);

            result.Should().BeOfType<InheritedBinaryFileMessage>();
            result.Should().BeEquivalentTo(new InheritedBinaryFileMessage()
            {
                Content = rawContent
            });
        }

        [Fact]
        public async Task DeserializeAsync_ByteArrayWithHardcodedType_CustomBinaryFileReturned()
        {
            var rawContent = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };
            var headers = new MessageHeaderCollection();

            var result = await new BinaryFileMessageSerializer<InheritedBinaryFileMessage>()
                .DeserializeAsync(rawContent, headers, MessageSerializationContext.Empty);

            result.Should().BeOfType<InheritedBinaryFileMessage>();
            result.Should().BeEquivalentTo(new InheritedBinaryFileMessage
            {
                Content = rawContent
            });
        }

        [Fact]
        public void Deserialize_NullMessage_NullIsReturned()
        {
            var deserialized = new BinaryFileMessageSerializer()
                .Deserialize(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_NullIsReturned()
        {
            var deserialized = await new BinaryFileMessageSerializer()
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserialized.Should().BeNull();
        }

        private class InheritedBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}