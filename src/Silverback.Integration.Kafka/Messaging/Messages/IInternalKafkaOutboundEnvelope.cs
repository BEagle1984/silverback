// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Messages;

internal interface IInternalKafkaOutboundEnvelope : IInternalOutboundEnvelope
{
    IInternalKafkaOutboundEnvelope SetOffset(KafkaOffset? offset);
}
