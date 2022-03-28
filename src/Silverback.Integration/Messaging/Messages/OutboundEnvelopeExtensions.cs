// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

internal static class OutboundEnvelopeExtensions
{
    public static OutboundEnvelope CloneReplacingStream(this IOutboundEnvelope envelope, Stream? rawMessage) =>
        new(envelope.Message, envelope.Headers, envelope.Endpoint, envelope.Producer)
        {
            RawMessage = rawMessage
        };
}
