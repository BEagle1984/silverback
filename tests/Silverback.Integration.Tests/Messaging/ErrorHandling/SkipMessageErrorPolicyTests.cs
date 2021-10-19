// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Configuration;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class SkipMessageErrorPolicyTests
    {
        private readonly ServiceProvider _serviceProvider;

        public SkipMessageErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services
                .AddLoggerSubstitute()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanHandle_Whatever_TrueReturned()
        {
            var policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task HandleErrorAsync_Whatever_TrueReturned()
        {
            var policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault());

            var result = await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            result.Should().BeTrue();
        }

        [Fact]
        public async Task HandleError_Whatever_ConsumerCommittedButTransactionAborted()
        {
            var policy = new SkipMessageErrorPolicy().Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                new TestConsumerConfiguration("source-endpoint").GetDefaultEndpoint());

            var transactionManager = Substitute.For<IConsumerTransactionManager>();

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
                new InvalidOperationException("test"));

            await transactionManager.Received(1).RollbackAsync(Arg.Any<InvalidOperationException>(), true);
        }
    }
}
