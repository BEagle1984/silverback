// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;

namespace Silverback.Tests
{
    public static class ConsumerPipelineContextHelper
    {
        public static ConsumerPipelineContext CreateSubstitute(
            IRawInboundEnvelope? envelope = null,
            IServiceProvider? serviceProvider = null,
            IConsumerTransactionManager? transactionManager = null,
            IConsumer? consumer = null)
        {
            var context = new ConsumerPipelineContext(
                envelope ?? Substitute.For<IRawInboundEnvelope>(),
                consumer ?? Substitute.For<IConsumer>(),
                serviceProvider ?? Substitute.For<IServiceProvider>());

            context.TransactionManager = transactionManager ?? Substitute.For<IConsumerTransactionManager>();

            return context;
        }
    }
}
