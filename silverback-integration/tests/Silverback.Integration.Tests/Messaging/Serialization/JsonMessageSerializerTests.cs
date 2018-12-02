// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using NUnit.Framework;
using Silverback.Messaging.Serialization;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Serialization
{
    [TestFixture]
    public class JsonMessageSerializerTests
    {
        [Test]
        public void SerializeDeserializeTest()
        {
            var message = new TestEventOne {Content = "the message"};

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(message);

            var message2 = serializer.Deserialize(serialized) as TestEventOne;

            Assert.That(message2, Is.Not.Null);
            Assert.That(message2.Content, Is.EqualTo(message.Content));
        }
    }
}
