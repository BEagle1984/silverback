// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>SerializeAsAvro</c> method to the <see cref="ProducerEndpoint" />.
    /// </summary>
    public static class ProducerEndpointBuilderSerializeAsAvroExtensions
    {
        /// <summary>
        ///     Sets the serializer to an instance of <see cref="AvroMessageSerializer{TMessage}" /> to serialize the
        ///     produced messages as Avro.
        /// </summary>
        /// <typeparam name="TBuilder">
        ///     The actual builder type.
        /// </typeparam>
        /// <param name="endpointBuilder">
        ///     The endpoint builder.
        /// </param>
        /// <param name="serializerBuilderAction">
        ///     An optional <see cref="Action{T}" /> that takes the <see cref="IAvroMessageSerializerBuilder" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        public static TBuilder SerializeAsAvro<TBuilder>(
            this IProducerEndpointBuilder<TBuilder> endpointBuilder,
            Action<IAvroMessageSerializerBuilder>? serializerBuilderAction = null)
            where TBuilder : IProducerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            var serializerBuilder = new AvroMessageSerializerBuilder();

            if (endpointBuilder.MessageType != null)
                serializerBuilder.UseType(endpointBuilder.MessageType);

            serializerBuilderAction?.Invoke(serializerBuilder);
            endpointBuilder.SerializeUsing(serializerBuilder.Build());

            return (TBuilder)endpointBuilder;
        }
    }
}
