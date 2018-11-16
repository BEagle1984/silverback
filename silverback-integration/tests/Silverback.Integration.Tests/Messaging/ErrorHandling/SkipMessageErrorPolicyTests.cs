using System;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class SkipMessageErrorPolicyTests
    {
        private SkipMessageErrorPolicy _policy;

        [SetUp]
        public void Setup()
        {
            _policy = new SkipMessageErrorPolicy(new NullLogger<SkipMessageErrorPolicy>());
        }

        [Test]
        public void SuccessTest()
        {
            var executed = false;
            _policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => executed = true);

            Assert.That(executed, Is.True);
        }

        [Test]
        public void ErrorTest()
        {
            _policy.TryHandleMessage(
                Envelope.Create(new TestEventOne()),
                _ => throw new Exception("test"));
        }
    }
}