// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Newtonsoft.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Serialization
{
    public class NewtonsoftJsonMessageSerializerTests
    {
        private static readonly MessageHeaderCollection TestEventOneMessageTypeHeaders = new MessageHeaderCollection
        {
            {
                "x-message-type",
                "Silverback.Tests.Integration.Newtonsoft.TestTypes.Domain.TestEventOne, Silverback.Integration.Newtonsoft.Tests"
            }
        };

        [Fact]
        public void Serialize_WithDefaultSettings_CorrectlySerialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer();

            var serialized = serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            serialized.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
        }

        [Fact]
        public void SerializeDeserialize_Message_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer();

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

            var serializer = new NewtonsoftJsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

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

            var serializer = new NewtonsoftJsonMessageSerializer();

            serializer.Serialize(message, headers, MessageSerializationContext.Empty);

            headers.Should().ContainEquivalentOf(
                new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
        }

        [Fact]
        public async Task SerializeAsync_Message_TypeHeaderAdded()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer();

            await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty);

            headers.Should().ContainEquivalentOf(
                new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
        }

        [Fact]
        public void Serialize_ByteArray_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new NewtonsoftJsonMessageSerializer();

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

            var serializer = new NewtonsoftJsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public void Serialize_NullMessage_NullReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var serialized = serializer.Serialize(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task SerializeAsync_NullMessage_NullReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var serialized = await serializer.SerializeAsync(
                null,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public void Deserialize_MessageWithIncompleteTypeHeader_Deserialized()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");

            var (deserializedObject, _) = serializer
                .Deserialize(rawMessage, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            var message = deserializedObject as TestEventOne;

            message.Should().NotBeNull();
            message.Should().BeOfType<TestEventOne>();
            message.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public void Deserialize_MessageWithIncompleteTypeHeader_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");

            var (_, type) = serializer
                .Deserialize(rawMessage, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_Deserialized()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");

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
            var serializer = new NewtonsoftJsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_MissingTypeHeader_ExceptionThrown()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
            var headers = new MessageHeaderCollection();

            Action act = () => serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<MessageSerializerException>();
        }

        [Fact]
        public void DeserializeAsync_MissingTypeHeader_ExceptionThrown()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
            var headers = new MessageHeaderCollection();

            Action act = () => serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<MessageSerializerException>();
        }

        [Fact]
        public void Deserialize_BadTypeHeader_ExceptionThrown()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Newtonsoft.Tests"
                }
            };

            var serializer = new NewtonsoftJsonMessageSerializer();

            Action act = () => serializer
                .Deserialize(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void DeserializeAsync_BadTypeHeader_ExceptionThrown()
        {
            var rawMessage = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
            var headers = new MessageHeaderCollection
            {
                {
                    "x-message-type",
                    "Bad.TestEventOne, Silverback.Integration.Newtonsoft.Tests"
                }
            };
            var serializer = new NewtonsoftJsonMessageSerializer();

            Action act = () => serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            act.Should().Throw<TypeLoadException>();
        }

        [Fact]
        public void Deserialize_NullMessage_NullObjectReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (deserializedObject, _) = serializer
                .Deserialize(null, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public void Deserialize_NullMessage_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (_, type) = serializer
                .Deserialize(null, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_NullObjectReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(null, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (_, type) = await serializer
                .DeserializeAsync(null, TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Deserialize_EmptyArrayMessage_NullObjectReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (deserializedObject, _) = serializer
                .Deserialize(Array.Empty<byte>(), TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public void Deserialize_EmptyArrayMessage_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (_, type) = serializer
                .Deserialize(Array.Empty<byte>(), TestEventOneMessageTypeHeaders, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessage_NullObjectReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(
                    Array.Empty<byte>(),
                    TestEventOneMessageTypeHeaders,
                    MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_EmptyArrayMessage_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            var (_, type) = await serializer
                .DeserializeAsync(
                    Array.Empty<byte>(),
                    TestEventOneMessageTypeHeaders,
                    MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public void Equals_SameInstance_TrueReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer();

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer, serializer);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_SameSettings_TrueReturned()
        {
            var serializer1 = new NewtonsoftJsonMessageSerializer
            {
                Settings = new JsonSerializerSettings
                {
                    MaxDepth = 42,
                    NullValueHandling = NullValueHandling.Ignore
                }
            };

            var serializer2 = new NewtonsoftJsonMessageSerializer
            {
                Settings = new JsonSerializerSettings
                {
                    MaxDepth = 42,
                    NullValueHandling = NullValueHandling.Ignore
                }
            };

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultSettings_TrueReturned()
        {
            var serializer1 = new NewtonsoftJsonMessageSerializer();
            var serializer2 = new NewtonsoftJsonMessageSerializer();

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSettings_FalseReturned()
        {
            var serializer1 = new NewtonsoftJsonMessageSerializer
            {
                Settings = new JsonSerializerSettings
                {
                    MaxDepth = 42,
                    NullValueHandling = NullValueHandling.Ignore
                }
            };

            var serializer2 = new NewtonsoftJsonMessageSerializer
            {
                Settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Include
                }
            };

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeFalse();
        }
    }
}
