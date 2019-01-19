// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes.Domain;
using Xunit;

namespace Silverback.Tests.Messaging.Serialization
{
    public class JsonMessageSerializerTests
    {
        [Fact]
        public void SerializeDeserializeTest()
        {
            var message = new TestEventOne {Content = "the message"};

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(message);

            var message2 = serializer.Deserialize(serialized) as TestEventOne;

            message2.Should().NotBeNull();
            message2.Content.Should().Be(message.Content);
        }
    }
}
