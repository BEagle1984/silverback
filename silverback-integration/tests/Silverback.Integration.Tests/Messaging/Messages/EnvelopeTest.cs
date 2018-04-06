using System;
using NUnit.Framework;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Messages
{
    [TestFixture]
    public class EnvelopeTest
    {
        [Test]
        public void GenerateMessageIdTest()
        {
            var message = new TestEventOne();

            Envelope.Create(message);

            Assert.That(message.Id, Is.Not.EqualTo(Guid.Empty));
        }
    }
}
