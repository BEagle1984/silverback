// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ConsumerEndpointBuilderConsumeBinaryFilesExtensionsTests
    {
        [Fact]
        public void ConsumeBinaryFiles_Default_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.ConsumeBinaryFiles().Build();

            endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer>();
            endpoint.Serializer.Should().NotBeSameAs(BinaryFileMessageSerializer.Default);
        }

        [Fact]
        public void ConsumeBinaryFiles_UseModel_SerializerSet()
        {
            var builder = new TestConsumerEndpointBuilder();

            var endpoint = builder.ConsumeBinaryFiles(serializer => serializer.UseModel<CustomBinaryFileMessage>())
                .Build();

            endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
        }

        private class CustomBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}
