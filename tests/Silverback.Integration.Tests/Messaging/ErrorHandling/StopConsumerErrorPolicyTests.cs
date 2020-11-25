// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class StopConsumerErrorPolicyTests
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly ServiceProvider _serviceProvider;

        public StopConsumerErrorPolicyTests()
        {
            var services = new ServiceCollection();

            services
                .AddNullLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddBroker<TestBroker>());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanHandle_Whatever_TrueReturned()
        {
            var policy = ErrorPolicy.Stop().Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var canHandle = policy.CanHandle(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task HandleErrorAsync_Whatever_FalseReturned()
        {
            var policy = ErrorPolicy.Stop().Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(),
                null,
                new TestOffset(),
                TestConsumerEndpoint.GetDefault(),
                TestConsumerEndpoint.GetDefault().Name);

            var result = await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider),
                new InvalidOperationException("test"));

            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleErrorAsync_Whatever_TransactionNotCommittedNorAborted()
        {
            /* The consumer will be stopped and the transaction aborted by the consumer/behavior */

            var policy = ErrorPolicy.Stop().Build(_serviceProvider);
            var envelope = new InboundEnvelope(
                "hey oh!",
                new MemoryStream(Encoding.UTF8.GetBytes("hey oh!")),
                null,
                new TestOffset(),
                new TestConsumerEndpoint("source-endpoint"),
                "source-endpoint");

            var transactionManager = Substitute.For<IConsumerTransactionManager>();

            await policy.HandleErrorAsync(
                ConsumerPipelineContextHelper.CreateSubstitute(envelope, _serviceProvider, transactionManager),
                new InvalidOperationException("test"));

            await transactionManager.ReceivedWithAnyArgs(0).RollbackAsync(Arg.Any<InvalidOperationException>());
        }
    }
}
