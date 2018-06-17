using System;
using NUnit.Framework;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class NoErrorPolicyTests
    {
        [Test]
        public void SuccessTest()
        {
            var executed = false;
            new NoErrorPolicy().TryHandleMessage(
                Envelope.Create( new TestEventOne()),
                _ => executed = true);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void ErrorTest()
        {
            Assert.Throws<Exception>(() =>
                new NoErrorPolicy().TryHandleMessage(
                    Envelope.Create(new TestEventOne()),
                    _ => throw new Exception("test")));
        }

        [Test]
        public void ChainingTest()
        {
            Assert.Throws<NotSupportedException>(() =>
                new NoErrorPolicy().Wrap(new TestErrorPolicy()));
        }
    }
}