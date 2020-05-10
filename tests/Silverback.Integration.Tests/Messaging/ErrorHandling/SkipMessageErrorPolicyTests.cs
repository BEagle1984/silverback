// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
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
            _policy = new SkipMessageErrorPolicy(null, new NullLogger<SkipMessageErrorPolicy>(), new MessageLogger());
        }

        [Theory]
        [InlineData(1, ErrorAction.Skip)]
        [InlineData(333, ErrorAction.Skip)]
        public async Task SkipTest(int failedAttempts, ErrorAction expectedAction)
        {
            var rawInboundEnvelopes = new[]
            {
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString()) },
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    TestConsumerEndpoint.GetDefault().Name),
            };

            var action = await _policy.HandleError(rawInboundEnvelopes, new Exception("test"));

            action.Should().Be(expectedAction);
        }
    }
}
