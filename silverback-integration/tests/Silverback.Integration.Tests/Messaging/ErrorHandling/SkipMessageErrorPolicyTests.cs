// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
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
        [TestCase(1, ErrorAction.Skip)]
        [TestCase(333, ErrorAction.Skip)]
        public void SkipTest(int failedAttempts, ErrorAction expectedAction)
        {
            var action = _policy.HandleError(new FailedMessage(new TestEventOne(), failedAttempts), new Exception("test"));

            Assert.That(action, Is.EqualTo(expectedAction));
        }
    }
}