// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics;

internal sealed class NullBrokerLogEnricher : BrokerLogEnricher
{
    private NullBrokerLogEnricher()
    {
    }

    public static NullBrokerLogEnricher Instance { get; } = new();

    protected override string AdditionalPropertyName1 => "unused1";

    protected override string AdditionalPropertyName2 => "unused2";

    public override (string? Value1, string? Value2) GetAdditionalValues(
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier) =>
        (null, null);
}
