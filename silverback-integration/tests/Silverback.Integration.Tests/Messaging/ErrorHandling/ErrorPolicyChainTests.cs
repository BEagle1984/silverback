// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.TestTypes;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.ErrorHandling
{
    [TestFixture]
    public class ErrorPolicyChainTests
    {
        private readonly ErrorPolicyBuilder _errorPolicyBuilder = new ErrorPolicyBuilder(new ServiceCollection().BuildServiceProvider(), NullLoggerFactory.Instance);

        [Test]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(4)]
        public void ChainingTest(int failedAttempts)
        {
            var testPolicy = new TestErrorPolicy();

            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
                testPolicy);

            chain.HandleError(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            Assert.That(testPolicy.Applied, Is.EqualTo(failedAttempts > 3));
        }

        [Test]
        [TestCase(1, ErrorAction.RetryMessage)]
        [TestCase(2, ErrorAction.RetryMessage)]
        [TestCase(3, ErrorAction.SkipMessage)]
        [TestCase(4, ErrorAction.SkipMessage)]
        public void ChainingTest2(int failedAttempts, ErrorAction expectedAction)
        {
            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(2),
                _errorPolicyBuilder.Skip());

            var action = chain.HandleError(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            Assert.That(action, Is.EqualTo(expectedAction));
        }
    }
}
