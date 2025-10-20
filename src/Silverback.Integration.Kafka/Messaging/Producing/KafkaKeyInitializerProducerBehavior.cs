// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

/// <summary>
///     Sets the Kafka key with the value from the properties decorated with the <see cref="KafkaKeyMemberAttribute" />.
/// </summary>
public class KafkaKeyInitializerProducerBehavior : IProducerBehavior
{
    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => BrokerBehaviorsSortIndexes.Producer.MessageKeyInitializer;

    /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
    public ValueTask HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next, CancellationToken cancellationToken)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(next, nameof(next));

        if (context.Envelope is IKafkaOutboundEnvelope<object, string> { Key: null } kafkaEnvelope)
        {
            if (context.Envelope is { IsTombstone: true, Message: ITombstone tombstone })
            {
                kafkaEnvelope.SetKey(tombstone.MessageKey);
            }
            else
            {
                string? key = KafkaKeyHelper.GetMessageKey(context.Envelope.Message);

                if (key != null)
                    kafkaEnvelope.SetKey(key);
            }
        }
       
        return next(context, cancellationToken);
    }
}
