// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Serialization
{
    /// <summary>
    ///     Serializes the message being produced using the configured <see cref="IMessageSerializer" />.
    /// </summary>
    public class SerializerProducerBehavior : IProducerBehavior
    {
        /// <inheritdoc cref="ISorted.SortIndex" />
        public int SortIndex => BrokerBehaviorsSortIndexes.Producer.Serializer;

        /// <inheritdoc cref="IProducerBehavior.HandleAsync" />
        public async Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(next, nameof(next));

            if (context.Envelope.Message is not Tombstone)
            {
                context.Envelope.RawMessage ??=
                    await context.Envelope.Endpoint.Configuration.Serializer.SerializeAsync(
                            context.Envelope.Message,
                            context.Envelope.Headers,
                            context.Envelope.Endpoint)
                        .ConfigureAwait(false);
            }
            else if (context.Envelope.Message.GetType().GenericTypeArguments.Length == 1)
            {
                context.Envelope.Headers.AddOrReplace(
                    DefaultMessageHeaders.MessageType,
                    context.Envelope.Message.GetType().GenericTypeArguments[0].AssemblyQualifiedName);
            }

            await next(context).ConfigureAwait(false);
        }
    }
}
