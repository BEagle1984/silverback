// TODO: DELETE

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.IO;
// using System.Text;
// using System.Text.Json;
// using System.Threading.Tasks;
// using FluentAssertions;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Serialization;
// using Silverback.Tests.Types;
// using Silverback.Tests.Types.Domain;
// using Silverback.Util;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Messaging.Serialization;
//
// public class JsonMessageSerializerTests
// {
//     private static readonly MessageHeaderCollection TestEventOneMessageTypeHeaders = new()
//     {
//         {
//             "x-message-type",
//             "Silverback.Tests.Types.Domain.TestEventOne, Silverback.Tests.Common.Integration"
//         }
//     };
//
//     [Fact]
//     public async Task SerializeAsync_WithDefaultSettings_CorrectlySerialized()
//     {
//         TestEventOne message = new() { Content = "the message" };
//         MessageHeaderCollection headers = new();
//
//         JsonMessageSerializer serializer = new();
//
//         Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
//
//         byte[] expected = Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}");
//         serialized.ReadAll().Should().BeEquivalentTo(expected);
//     }
//
//     [Fact]
//     public async Task SerializeDeserializeAsync_Message_CorrectlyDeserialized()
//     {
//         TestEventOne message = new() { Content = "the message" };
//         MessageHeaderCollection headers = new();
//
//         JsonMessageSerializer serializer = new();
//
//         Stream? serialized = await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
//         (object? deserialized, _) = await serializer.DeserializeAsync(serialized, headers, TestConsumerEndpoint.GetDefault());
//
//         TestEventOne? message2 = deserialized as TestEventOne;
//
//         message2.Should().NotBeNull();
//         message2.Should().BeEquivalentTo(message);
//     }
//
//     [Fact]
//     public async Task SerializeAsync_Message_TypeHeaderAdded()
//     {
//         TestEventOne message = new() { Content = "the message" };
//         MessageHeaderCollection headers = new();
//
//         JsonMessageSerializer serializer = new();
//
//         await serializer.SerializeAsync(message, headers, TestProducerEndpoint.GetDefault());
//
//         headers.Should().ContainEquivalentOf(new MessageHeader("x-message-type", typeof(TestEventOne).AssemblyQualifiedName));
//     }
//
//     [Fact]
//     public async Task SerializeAsync_Stream_ReturnedUnmodified()
//     {
//         MemoryStream messageStream = new(Encoding.UTF8.GetBytes("test"));
//
//         JsonMessageSerializer serializer = new();
//
//         Stream? serialized = await serializer.SerializeAsync(
//             messageStream,
//             new MessageHeaderCollection(),
//             TestProducerEndpoint.GetDefault());
//
//         serialized.Should().BeSameAs(messageStream);
//     }
//
//     [Fact]
//     public async Task SerializeAsync_ByteArray_ReturnedUnmodified()
//     {
//         byte[] messageBytes = Encoding.UTF8.GetBytes("test");
//
//         JsonMessageSerializer serializer = new();
//
//         Stream? serialized = await serializer.SerializeAsync(
//             messageBytes,
//             new MessageHeaderCollection(),
//             TestProducerEndpoint.GetDefault());
//
//         serialized.ReadAll().Should().BeEquivalentTo(messageBytes);
//     }
//
//     [Fact]
//     public async Task SerializeAsync_NullMessage_NullReturned()
//     {
//         JsonMessageSerializer serializer = new();
//
//         Stream? serialized = await serializer.SerializeAsync(
//             null,
//             new MessageHeaderCollection(),
//             TestProducerEndpoint.GetDefault());
//
//         serialized.Should().BeNull();
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_Deserialized()
//     {
//         JsonMessageSerializer serializer = new();
//         MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
//
//         (object? deserializedObject, _) = await serializer
//             .DeserializeAsync(rawMessage, TestEventOneMessageTypeHeaders, TestConsumerEndpoint.GetDefault());
//
//         TestEventOne? message = deserializedObject as TestEventOne;
//
//         message.Should().NotBeNull();
//         message.Should().BeOfType<TestEventOne>();
//         message.As<TestEventOne>().Content.Should().Be("the message");
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_MessageWithIncompleteTypeHeader_TypeReturned()
//     {
//         JsonMessageSerializer serializer = new();
//         MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
//
//         (_, Type type) = await serializer
//             .DeserializeAsync(rawMessage, TestEventOneMessageTypeHeaders, TestConsumerEndpoint.GetDefault());
//
//         type.Should().Be(typeof(TestEventOne));
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_MissingTypeHeader_ExceptionThrown()
//     {
//         JsonMessageSerializer serializer = new();
//         MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
//         MessageHeaderCollection headers = new();
//
//         Func<Task> act = async () => await serializer
//             .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());
//
//         await act.Should().ThrowAsync<MessageSerializerException>();
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_BadTypeHeader_ExceptionThrown()
//     {
//         MemoryStream rawMessage = new(Encoding.UTF8.GetBytes("{\"Content\":\"the message\"}"));
//         MessageHeaderCollection headers = new()
//         {
//             {
//                 "x-message-type",
//                 "Bad.TestEventOne, Silverback.Integration.Tests"
//             }
//         };
//         JsonMessageSerializer serializer = new();
//
//         Func<Task> act = async () => await serializer
//             .DeserializeAsync(rawMessage, headers, TestConsumerEndpoint.GetDefault());
//
//         await act.Should().ThrowAsync<TypeLoadException>();
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_NullMessage_NullObjectReturned()
//     {
//         JsonMessageSerializer serializer = new();
//
//         (object? deserializedObject, _) = await serializer
//             .DeserializeAsync(null, TestEventOneMessageTypeHeaders, TestConsumerEndpoint.GetDefault());
//
//         deserializedObject.Should().BeNull();
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_NullMessage_TypeReturned()
//     {
//         JsonMessageSerializer serializer = new();
//
//         (_, Type type) = await serializer
//             .DeserializeAsync(null, TestEventOneMessageTypeHeaders, TestConsumerEndpoint.GetDefault());
//
//         type.Should().Be(typeof(TestEventOne));
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_EmptyStream_NullObjectReturned()
//     {
//         JsonMessageSerializer serializer = new();
//
//         (object? deserializedObject, _) = await serializer
//             .DeserializeAsync(
//                 new MemoryStream(),
//                 TestEventOneMessageTypeHeaders,
//                 TestConsumerEndpoint.GetDefault());
//
//         deserializedObject.Should().BeNull();
//     }
//
//     [Fact]
//     public async Task DeserializeAsync_EmptyStream_TypeReturned()
//     {
//         JsonMessageSerializer serializer = new();
//
//         (_, Type type) = await serializer
//             .DeserializeAsync(
//                 new MemoryStream(),
//                 TestEventOneMessageTypeHeaders,
//                 TestConsumerEndpoint.GetDefault());
//
//         type.Should().Be(typeof(TestEventOne));
//     }
//
//     [Fact]
//     public void Equals_SameInstance_TrueReturned()
//     {
//         JsonMessageSerializer serializer = new();
//
//         // ReSharper disable once EqualExpressionComparison
//         bool result = Equals(serializer, serializer);
//
//         result.Should().BeTrue();
//     }
//
//     [Fact]
//     public void Equals_SameSettings_TrueReturned()
//     {
//         JsonMessageSerializer serializer1 = new()
//         {
//             Options = new JsonSerializerOptions
//             {
//                 AllowTrailingCommas = true,
//                 DefaultBufferSize = 42
//             }
//         };
//
//         JsonMessageSerializer serializer2 = new()
//         {
//             Options = new JsonSerializerOptions
//             {
//                 AllowTrailingCommas = true,
//                 DefaultBufferSize = 42
//             }
//         };
//
//         // ReSharper disable once EqualExpressionComparison
//         bool result = Equals(serializer1, serializer2);
//
//         result.Should().BeTrue();
//     }
//
//     [Fact]
//     public void Equals_DefaultSettings_TrueReturned()
//     {
//         JsonMessageSerializer serializer1 = new();
//         JsonMessageSerializer serializer2 = new();
//
//         // ReSharper disable once EqualExpressionComparison
//         bool result = Equals(serializer1, serializer2);
//
//         result.Should().BeTrue();
//     }
//
//     [Fact]
//     public void Equals_DifferentSettings_FalseReturned()
//     {
//         JsonMessageSerializer serializer1 = new()
//         {
//             Options = new JsonSerializerOptions
//             {
//                 AllowTrailingCommas = true,
//                 DefaultBufferSize = 42
//             }
//         };
//
//         JsonMessageSerializer serializer2 = new()
//         {
//             Options = new JsonSerializerOptions
//             {
//                 AllowTrailingCommas = false
//             }
//         };
//
//         // ReSharper disable once EqualExpressionComparison
//         bool result = Equals(serializer1, serializer2);
//
//         result.Should().BeFalse();
//     }
// }
