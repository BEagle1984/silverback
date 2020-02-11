// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyChainTests
    {
        private readonly ErrorPolicyBuilder _errorPolicyBuilder;

        // TODO: Test with multiple messages (batch)

        public ErrorPolicyChainTests()
        {
            var services = new ServiceCollection();

            services.AddSilverback().WithConnectionTo<TestBroker>();

            services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

            _errorPolicyBuilder =
                new ErrorPolicyBuilder(
                    services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true }),
                    NullLoggerFactory.Instance);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        public void HandleError_RetryWithMaxFailedAttempts_AppliedAccordingToMaxFailedAttempts(int failedAttempts)
        {
            var testPolicy = new TestErrorPolicy();

            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(3),
                testPolicy);

            chain.HandleError(new[]
            {
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString()) },
                    null, TestConsumerEndpoint.GetDefault())
            }, new Exception("test"));

            testPolicy.Applied.Should().Be(failedAttempts > 3);
        }

        [Theory]
        [InlineData(1, ErrorAction.Retry)]
        [InlineData(2, ErrorAction.Retry)]
        [InlineData(3, ErrorAction.Skip)]
        [InlineData(4, ErrorAction.Skip)]
        public void HandleError_RetryTwiceThenSkip_CorrectPolicyApplied(int failedAttempts, ErrorAction expectedAction)
        {
            var chain = _errorPolicyBuilder.Chain(
                _errorPolicyBuilder.Retry().MaxFailedAttempts(2),
                _errorPolicyBuilder.Skip());

            var action = chain.HandleError(new[]
            {
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString()) },
                    null, TestConsumerEndpoint.GetDefault())
            }, new Exception("test"));

            action.Should().Be(expectedAction);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(5, 2)]
        public void HandleError_MultiplePoliciesWithSetMaxFailedAttempts_CorrectPolicyApplied(
            int failedAttempts,
            int expectedAppliedPolicy)
        {
            var policies = new[]
            {
                new TestErrorPolicy().MaxFailedAttempts(2),
                new TestErrorPolicy().MaxFailedAttempts(2),
                new TestErrorPolicy().MaxFailedAttempts(2)
            };

            var chain = _errorPolicyBuilder.Chain(policies);

            chain.HandleError(new[]
            {
                new InboundEnvelope(
                    new byte[1],
                    new[] { new MessageHeader(DefaultMessageHeaders.FailedAttempts, failedAttempts.ToString()) },
                    null, TestConsumerEndpoint.GetDefault())
            }, new Exception("test"));

            for (var i = 0; i < policies.Length; i++)
            {
                policies[i].As<TestErrorPolicy>().Applied.Should().Be(i == expectedAppliedPolicy);
            }
        }
    }
}