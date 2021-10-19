// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Configuration;

internal sealed class KafkaLogEnricher : IBrokerLogEnricher<KafkaProducerConfiguration>, IBrokerLogEnricher<KafkaConsumerConfiguration>
{
    public string AdditionalPropertyName1 => "offset";

    public string AdditionalPropertyName2 => "kafkaKey";

    public (string? Value1, string? Value2) GetAdditionalValues(
        Endpoint endpoint,
        IReadOnlyCollection<MessageHeader>? headers,
        IBrokerMessageIdentifier? brokerMessageIdentifier) =>
        (brokerMessageIdentifier?.ToLogString(), headers?.GetValue(KafkaMessageHeaders.KafkaMessageKey));
}
