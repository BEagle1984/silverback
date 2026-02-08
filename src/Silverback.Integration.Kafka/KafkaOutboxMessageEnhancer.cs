// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace Silverback;

internal class KafkaOutboxMessageEnhancer : OutboxMessageEnhancer<KafkaProducerEndpointConfiguration>
{
    public override byte[]? GetExtra(IOutboundEnvelope envelope) => envelope.GetKafkaRawKey();

    public override void Enhance(IOutboundEnvelope envelope, OutboxMessage outboxMessage) => envelope.SetKafkaRawKey(outboxMessage.Extra);
}
