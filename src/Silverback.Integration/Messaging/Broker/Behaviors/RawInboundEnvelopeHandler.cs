// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors
{
    public delegate Task RawInboundEnvelopeHandler(
        IReadOnlyCollection<IRawInboundEnvelope> envelopes,
        IServiceProvider serviceProvider,
        IConsumer consumer);
}