// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Used only as a wrapper to reuse the error policies and logging logic.
    /// </summary>
    internal class InboundBatch : IInboundBatch, IInboundMessage
    {
        public InboundBatch(Guid batchId, IEnumerable<IInboundMessage> messages, IEndpoint endpoint)
        {
            Id = batchId;
            Messages = messages ?? throw new ArgumentNullException(nameof(messages));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Size = messages.Count();
        }

        public Guid Id { get; }

        public IEnumerable<IInboundMessage> Messages { get; }

        public int Size { get; }

        public IEndpoint Endpoint { get; }

        public int FailedAttempts => Messages.Min(m => m.FailedAttempts);

        public object Message => null;

        public MessageHeaderCollection Headers => null;

        public IOffset Offset => null;

        public bool MustUnwrap => true;
    }
}