// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ErrorPolicyChainTests
    {
        private readonly IServiceProvider _serviceProvider;

        public ErrorPolicyChainTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(130)]
        public void CanHandle_Whatever_TrueReturned(int failedAttempts)
        {
            var rawMessage = new MemoryStream();
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };

            var testPolicy = new TestErrorPolicy();

            var chain = new ErrorPolicyChain(
                    new RetryErrorPolicy().MaxFailedAttempts(3),
                    testPolicy)
                .Build(_serviceProvider);

            var result = chain.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(
                    new InboundEnvelope(
                        rawMessage,
                        headers,
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)),
                new InvalidOperationException("test"));

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        public async Task HandleErrorAsync_RetryWithMaxFailedAttempts_AppliedAccordingToMaxFailedAttempts(int failedAttempts)
        {
            var rawMessage = new MemoryStream();
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };

            var testPolicy = new TestErrorPolicy();

            var chain = new ErrorPolicyChain(
                    new[]
                    {
                        new RetryErrorPolicy().MaxFailedAttempts(3),
                        testPolicy
                    })
                .Build(_serviceProvider);

            await chain.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(
                    new InboundEnvelope(
                        rawMessage,
                        headers,
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)),
                new InvalidOperationException("test"));

            testPolicy.Applied.Should().Be(failedAttempts > 3);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 0)]
        [InlineData(3, 1)]
        [InlineData(4, 1)]
        [InlineData(5, 2)]
        public async Task HandleErrorAsync_MultiplePoliciesWithMaxFailedAttempts_CorrectPolicyApplied(
            int failedAttempts,
            int expectedAppliedPolicy)
        {
            var rawMessage = new MemoryStream();
            var headers = new[]
            {
                new MessageHeader(
                    DefaultMessageHeaders.FailedAttempts,
                    failedAttempts.ToString(CultureInfo.InvariantCulture))
            };

            var policies = new[]
            {
                new TestErrorPolicy().MaxFailedAttempts(2),
                new TestErrorPolicy().MaxFailedAttempts(2),
                new TestErrorPolicy().MaxFailedAttempts(2)
            };

            var chain = new ErrorPolicyChain(policies)
                .Build(_serviceProvider);

            await chain.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(
                    new InboundEnvelope(
                        rawMessage,
                        headers,
                        new TestOffset(),
                        TestConsumerEndpoint.GetDefault(),
                        TestConsumerEndpoint.GetDefault().Name)),
                new InvalidOperationException("test"));

            for (var i = 0; i < policies.Length; i++)
            {
                policies[i].As<TestErrorPolicy>().Applied.Should().Be(i == expectedAppliedPolicy);
            }
        }
    }
}
