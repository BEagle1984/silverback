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
        [TestCase(0, ErrorAction.SkipMessage)]
        [TestCase(1, ErrorAction.SkipMessage)]
        [TestCase(333, ErrorAction.SkipMessage)]
        public void SkipTest(int retryCount, ErrorAction expectedAction)
        {
            var action = _policy.HandleError(new TestEventOne(), retryCount, new Exception("test"));

            Assert.That(action, Is.EqualTo(expectedAction));
        }
    }
}