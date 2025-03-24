// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.Util;

public static class EnvelopesExtensions
{
    public static string? GetRawMessageAsString(this IRawInboundEnvelope envelope) =>
        envelope.RawMessage == null ? null : Encoding.UTF8.GetString(envelope.RawMessage.ReReadAll());

    public static IReadOnlyCollection<string?> GetRawMessageAsString(this IEnumerable<IRawInboundEnvelope> envelopes) =>
        envelopes.Select(GetRawMessageAsString).ToList();
}
