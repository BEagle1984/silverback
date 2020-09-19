// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.Newtonsoft.TestTypes.Domain;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Newtonsoft.Messaging.Serialization
{
    public class TypedNewtonsoftJsonMessageSerializerTests
    {
        [Fact]
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
        {
            var message = new TestEventOne { Content = "the message" };
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var serialized = (await serializer.SerializeAsync(message, headers, MessageSerializationContext.Empty))!;

            Encoding.UTF8.GetString(serialized.ReadAll()!).Should().NotContain("TestEventOne");

            var (deserialized, _) = await serializer
                .DeserializeAsync(serialized, headers, MessageSerializationContext.Empty);

            var message2 = deserialized as TestEventOne;

            message2.Should().NotBeNull();
            message2.Should().BeEquivalentTo(message);
        }

        [Fact]
        public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
        {
            var messageBytes = Encoding.UTF8.GetBytes("test");

            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var serialized = await serializer.SerializeAsync(
                messageBytes,
                new MessageHeaderCollection(),
                MessageSerializationContext.Empty);

            serialized.Should().BeSameAs(messageBytes);
        }

        [Fact]
        public async Task SerializeAsync_NullMessage_NullReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var serialized = await serializer
                .SerializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            serialized.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_MissingTypeHeader_Deserialized()
        {
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().NotBeNull();
            deserializedObject.Should().BeOfType<TestEventOne>();
            deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public async Task DeserializeAsync_MissingTypeHeader_TypeReturned()
        {
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_WrongTypeHeader_Deserialized()
        {
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            deserializedObject.Should().NotBeNull();
            deserializedObject.Should().BeOfType<TestEventOne>();
            deserializedObject.As<TestEventOne>().Content.Should().Be("the message");
        }

        [Fact]
        public async Task DeserializeAsync_WrongTypeHeader_TypeReturned()
        {
            var rawMessage = new MemoryStream(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
            var headers = new MessageHeaderCollection();

            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(rawMessage, headers, MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_NullObjectReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_NullMessage_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(null, new MessageHeaderCollection(), MessageSerializationContext.Empty);

            type.Should().Be(typeof(TestEventOne));
        }

        [Fact]
        public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (deserializedObject, _) = await serializer
                .DeserializeAsync(
                    new MemoryStream(),
                    new MessageHeaderCollection(),
                    MessageSerializationContext.Empty);

            deserializedObject.Should().BeNull();
        }

        [Fact]
        public async Task DeserializeAsync_EmptyStream_TypeReturned()
        {
            var serializer = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            var (_, type) = await serializer
                .DeserializeAsync(
                    new MemoryStream(),
                    new MessageHeaderCollection(),
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
            var serializer1 = new NewtonsoftJsonMessageSerializer<TestEventOne>
            {
                Settings = new JsonSerializerSettings
                {
                    MaxDepth = 42,
                    NullValueHandling = NullValueHandling.Ignore
                }
            };

            var serializer2 = new NewtonsoftJsonMessageSerializer<TestEventOne>
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
            var serializer1 = new NewtonsoftJsonMessageSerializer<TestEventOne>();
            var serializer2 = new NewtonsoftJsonMessageSerializer<TestEventOne>();

            // ReSharper disable once EqualExpressionComparison
            var result = Equals(serializer1, serializer2);

            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_DifferentSettings_FalseReturned()
        {
            var serializer1 = new NewtonsoftJsonMessageSerializer<TestEventOne>
            {
                Settings = new JsonSerializerSettings
                {
                    MaxDepth = 42,
                    NullValueHandling = NullValueHandling.Ignore
                }
            };

            var serializer2 = new NewtonsoftJsonMessageSerializer<TestEventOne>
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
