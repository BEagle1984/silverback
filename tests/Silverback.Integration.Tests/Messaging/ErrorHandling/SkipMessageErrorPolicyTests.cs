// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class SkipMessageErrorPolicyTests
    {
        private readonly SkipMessageErrorPolicy _policy;

        public SkipMessageErrorPolicyTests()
        {
            _policy = new SkipMessageErrorPolicy(null, new NullLogger<SkipMessageErrorPolicy>(), new MessageLogger(new MessageKeyProvider(new[] { new DefaultPropertiesMessageKeyProvider() })));
        }

        [Theory]
        [InlineData(1, ErrorAction.Skip)]
        [InlineData(333, ErrorAction.Skip)]
        public void SkipTest(int failedAttempts, ErrorAction expectedAction)
        {
            var action = _policy.HandleError(new[]
            {
                new InboundMessage(
                    new byte[1],
                    new[] { new MessageHeader(MessageHeader.FailedAttemptsKey, failedAttempts.ToString()) },
                    null, TestEndpoint.Default, true)
            }, new Exception("test"));

            action.Should().Be(expectedAction);
        }
    }
}