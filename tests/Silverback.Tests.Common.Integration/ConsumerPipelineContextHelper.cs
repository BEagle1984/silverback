// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;

namespace Silverback.Tests
{
    public static class ConsumerPipelineContextHelper
    {
        public static ConsumerPipelineContext CreateSubstitute(
            IRawInboundEnvelope? envelope = null,
            IServiceProvider? serviceProvider = null,
            IConsumerTransactionManager? transactionManager = null,
            IConsumer? consumer = null,
            ISequenceStore? sequenceStore = null) =>
            new ConsumerPipelineContext(
                envelope ?? Substitute.For<IRawInboundEnvelope>(),
                consumer ?? Substitute.For<IConsumer>(),
                sequenceStore ?? Substitute.For<ISequenceStore>(),
                serviceProvider ?? Substitute.For<IServiceProvider>())
            {
                TransactionManager = transactionManager ?? Substitute.For<IConsumerTransactionManager>()
            };
    }
}
