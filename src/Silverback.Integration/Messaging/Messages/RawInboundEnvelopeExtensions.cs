// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

internal static class RawInboundEnvelopeExtensions
{
    public static RawInboundEnvelope CloneReplacingStream(this IRawInboundEnvelope envelope, Stream? rawMessage) =>
        new(rawMessage, envelope.Headers, envelope.Endpoint, envelope.BrokerMessageIdentifier);
}
