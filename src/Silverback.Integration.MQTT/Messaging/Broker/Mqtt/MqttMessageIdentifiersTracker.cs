// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     Tracks the identifiers of the messages added to a sequence, like the <see cref="SimpleMessageIdentifiersTracker" />, and marks
///     the tracked messages in their consumer. A message processed by a sequence that doesn't track it (e.g. because no subscriber stream
///     accepted it) is committed by nobody: Kafka commits it together with the later offsets, while the MQTT consumer must acknowledge it
///     explicitly.
/// </summary>
internal sealed class MqttMessageIdentifiersTracker : IBrokerMessageIdentifiersTracker
{
    // The identifiers are added by the reader, while the rollback of another sequence may read them (see
    // ConsumerPipelineContext.GetRollbackIdentifiers)
    private readonly System.Threading.Lock _lock = new();

    private readonly HashSet<IBrokerMessageIdentifier> _identifiers = [];

    private readonly IConsumerCollection _consumers;

    public MqttMessageIdentifiersTracker(IConsumerCollection consumers)
    {
        _consumers = Check.NotNull(consumers, nameof(consumers));
    }

    public void TrackIdentifier(IBrokerMessageIdentifier identifier)
    {
        lock (_lock)
        {
            if (!_identifiers.Add(identifier))
                return;
        }

        if (identifier is not MqttMessageIdentifier mqttIdentifier)
            return;

        // The client identifier isn't necessarily unique (e.g. when connecting to multiple brokers), the message identifier is
        foreach (IConsumer consumer in _consumers)
        {
            if (consumer is MqttConsumer mqttConsumer && mqttConsumer.TryMarkTracked(mqttIdentifier))
                return;
        }
    }

    public IReadOnlyCollection<IBrokerMessageIdentifier> GetCommitIdentifiers() => GetSnapshot();

    public IReadOnlyCollection<IBrokerMessageIdentifier> GetRollbackIdentifiers() => GetSnapshot();

    private IBrokerMessageIdentifier[] GetSnapshot()
    {
        lock (_lock)
        {
            return [.. _identifiers];
        }
    }
}
