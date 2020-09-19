// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Inbound.ErrorHandling;
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
            _policy = new SkipMessageErrorPolicy(
                Substitute.For<IServiceProvider>(),
                Substitute.For<ISilverbackIntegrationLogger<SkipMessageErrorPolicy>>());
        }

        [Theory]
        [InlineData(1, ErrorAction.Skip)]
        [InlineData(333, ErrorAction.Skip)]
        public async Task SkipTest(int failedAttempts, ErrorAction expectedAction)
        {
            var rawMessage = new MemoryStream();
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };
            var rawInboundEnvelopes = new[]
            {
                new InboundEnvelope(
                    rawMessage,
                    headers,
                    null,
                    TestConsumerEndpoint.GetDefault(),
                    TestConsumerEndpoint.GetDefault().Name),
            };

            var action = await _policy.HandleError(rawInboundEnvelopes, new InvalidOperationException("test"));

            action.Should().Be(expectedAction);
        }
    }
}
