// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent an envelope that has been already processed by the behaviors. It is used to avoid processing
    ///     again the messages that have been stored in the outbox.
    /// </summary>
    internal class ProcessedOutboundEnvelope : OutboundEnvelope
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ProcessedOutboundEnvelope(
            byte[]? messageContent,
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint)
            : base(messageContent, headers, endpoint)
        {
        }

        public ProcessedOutboundEnvelope(
            Stream? messageStream,
            IEnumerable<MessageHeader>? headers,
            IProducerEndpoint endpoint)
            : base(messageStream, headers, endpoint)
        {
        }
    }
}
