// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Serialization
{
    public class JsonMessageSerializerTests
    {
        private static readonly MessageHeaderCollection TestEventOneMessageTypeHeaders = new MessageHeaderCollection
        {
            {
                "x-message-type",
                "Silverback.Tests.Integration.TestTypes.Domain.TestEventOne, Silverback.Integration.Tests"
            }
        };

        [Fact]
        public async Task SerializeAsync_WithDefaultSettings_CorrectlySerialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new JsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            var expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
            serialized.ReadAll().Should().BeEquivalentTo(expected);
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
        public async Task SerializeAsync_Stream_ReturnedUnmodified()
        {
            var messageStream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

            var serializer = new JsonMessageSerializer();

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

            var serializer = new JsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
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
        public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_Deserialized()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(rawMessage, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            var message = deserializedObject as TestEventOne;

            message.Should().NotBeNull();
            message.Should().BeOfType<TestEventOne>();
            message.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void DeserializeAsync_MissingTypeHeader_ExceptionThrown()
        {
            var serializer = new JsonMessageSerializer();
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
            var headers = new MessageHeaderCollection();

            Func<Task> act = async () => await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<MessageSerializerException>();
        }

        [Fact]
        public void DeserializeAsync_BadTypeHeader_ExceptionThrown()
        {
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Tests"
                }
            };
            var serializer = new JsonMessageSerializer();

            Func<Task> act = async () => await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(null, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (_, type) = await serializer
                .DeserializeAsync(null, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(
                    new MemoryStream(),
                    TestEventOneMessageTypeHeaders,
                    MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_EmptyStream_TypeReturned()
        {
            var serializer = new JsonMessageSerializer();

            var (_, type) = await serializer
                .DeserializeAsync(
                    new MemoryStream(),
                    TestEventOneMessageTypeHeaders,
                    MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Equals_SameInstance_TrueReturned()
        {
            var serializer = new JsonMessageSerializer();

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer, serializer);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameSettings_TrueReturned()
        {
            var serializer1 = new JsonMessageSerializer
            {
                Options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    DefaultBufferSize = 42
                }
            };

            var serializer2 = new JsonMessageSerializer
            {
                Options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    DefaultBufferSize = 42
                }
            };

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultSettings_TrueReturned()
        {
            var serializer1 = new JsonMessageSerializer();
            var serializer2 = new JsonMessageSerializer();

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSettings_FalseReturned()
        {
            var serializer1 = new JsonMessageSerializer
            {
                Options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = true,
                    DefaultBufferSize = 42
                }
            };

            var serializer2 = new JsonMessageSerializer
            {
                Options = new JsonSerializerOptions
                {
                    AllowTrailingCommas = false
                }
            };

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeFalse();
        }
    }
}
