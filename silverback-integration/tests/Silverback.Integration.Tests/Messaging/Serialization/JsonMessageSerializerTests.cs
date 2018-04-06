using NUnit.Framework;
using Silverback.Messaging.Messages;
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
            var envelope = Envelope.Create(message);

            var serializer = new JsonMessageSerializer();

            var serialized = serializer.Serialize(envelope);

            var envelope2 = serializer.Deserialize(serialized);
            var message2 = envelope2?.Message as TestEventOne;

            Assert.That(envelope2, Is.Not.Null);
            Assert.That(message2, Is.Not.Null);
            Assert.That(message2.Content, Is.EqualTo(message.Content));
        }
    }
}
