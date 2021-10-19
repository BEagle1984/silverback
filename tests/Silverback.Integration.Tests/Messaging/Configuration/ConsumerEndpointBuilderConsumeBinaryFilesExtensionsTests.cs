// TODO: TEST

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using System.Diagnostics.CodeAnalysis;
// using System.Text.Json;
// using FluentAssertions;
// using Silverback.Messaging.BinaryFiles;
// using Silverback.Messaging.Configuration;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Serialization;
// using Silverback.Tests.Types;
// using Silverback.Tests.Types.Domain;
// using Xunit;
//
// namespace Silverback.Tests.Integration.Messaging.Configuration;
//
// public class ConsumerEndpointBuilderConsumeBinaryFilesExtensionsTests
// {
//     [Fact]
//     public void ConsumeBinaryFiles_Default_SerializerSet()
//     {
//         TestConsumerConfigurationBuilder<object> builder = new();
//
//         TestConsumerConfiguration endpoint = builder.ConsumeBinaryFiles().Build();
//
//         endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer>();
//         endpoint.Serializer.Should().NotBeSameAs(BinaryFileMessageSerializer.Default);
//     }
//
//     [Fact]
//     public void ConsumeBinaryFiles_SetMessageType_SerializerSet()
//     {
//         TestConsumerConfigurationBuilder<CustomBinaryFileMessage> builder = new();
//
//         TestConsumerConfiguration endpoint = builder.ConsumeBinaryFiles().Build();
//
//         endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
//     }
//
//     [Fact]
//     public void ConsumeBinaryFiles_WithInvalidMessageType_ExceptionThrown()
//     {
//         TestConsumerConfigurationBuilder<TestEventOne> builder = new();
//
//         Action act = () => builder.ConsumeBinaryFiles();
//
//         act.Should().ThrowExactly<ArgumentException>()
//             .WithMessage("The type *.TestEventOne does not implement IBinaryFileMessage. *");
//     }
//
//     [Fact]
//     public void ConsumeBinaryFiles_MessageTypeWithoutEmptyConstructor_ExceptionThrown()
//     {
//         TestConsumerConfigurationBuilder<BinaryFileMessageWithoutDefaultConstructor> builder = new();
//
//         Action act = () => builder.ConsumeBinaryFiles();
//
//         act.Should().ThrowExactly<ArgumentException>()
//             .WithMessage("The type *+BinaryFileMessageWithoutDefaultConstructor does not have a default constructor. *");
//     }
//
//     [Fact]
//     public void ConsumeBinaryFiles_UseModel_SerializerSet()
//     {
//         TestConsumerConfigurationBuilder<object> builder = new();
//
//         TestConsumerConfiguration endpoint = builder
//             .ConsumeBinaryFiles(serializer => serializer.UseModel<CustomBinaryFileMessage>())
//             .Build();
//
//         endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
//     }
//
//     private sealed class CustomBinaryFileMessage : BinaryFileMessage
//     {
//     }
//
//     private sealed class BinaryFileMessageWithoutDefaultConstructor : BinaryFileMessage
//     {
//         [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "Required for testing.")]
//         public BinaryFileMessageWithoutDefaultConstructor(string value)
//         {
//         }
//     }
// }
