// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration;

internal sealed class KafkaLogEnricher
    : BrokerLogEnricher, IBrokerLogEnricher<KafkaProducerEndpointConfiguration>, IBrokerLogEnricher<KafkaConsumerEndpointConfiguration>
{
    protected override string AdditionalPropertyName1 => "offset";

    protected override string AdditionalPropertyName2 => "kafkaKey";

    public override (string? Value1, string? Value2) GetAdditionalValues(
        Endpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier) =>
        (brokerMessageIdentifier?.ToLogString(), headers?.GetValue(KafkaMessageHeaders.KafkaMessageKey));
}
