// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.BinaryFiles;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration
{
    public class ProducerEndpointBuilderProduceBinaryFilesExtensionsTests
    {
        [Fact]
        public void ProduceBinaryFiles_Default_SerializerSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.ProduceBinaryFiles().Build();

            endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer>();
            endpoint.Serializer.Should().NotBeSameAs(JsonMessageSerializer.Default);
        }

        [Fact]
        public void ProduceBinaryFiles_UseFixedType_SerializerSet()
        {
            var builder = new TestProducerEndpointBuilder();

            var endpoint = builder.ProduceBinaryFiles(serializer => serializer.UseModel<CustomBinaryFileMessage>())
                .Build();

            endpoint.Serializer.Should().BeOfType<BinaryFileMessageSerializer<CustomBinaryFileMessage>>();
        }

        private class CustomBinaryFileMessage : BinaryFileMessage
        {
        }
    }
}
