// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Transaction;

public class ConsumerTransactionManagerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RollbackAsync_ShouldUseOriginalMessageIdentifier_WhenStoppingConsumer(bool stopConsuming)
    {
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute();
        ConsumerTransactionManager transaction = new(context, Substitute.For<ISilverbackLogger<ConsumerTransactionManager>>());

        await transaction.RollbackAsync(null, stopConsuming: stopConsuming);

        await context.Consumer.Received(stopConsuming ? 1 : 0).StopAsync(context.Envelope.BrokerMessageIdentifier, false);
        await context.Consumer.DidNotReceive().StopAsync(Arg.Any<bool>());
    }
}
