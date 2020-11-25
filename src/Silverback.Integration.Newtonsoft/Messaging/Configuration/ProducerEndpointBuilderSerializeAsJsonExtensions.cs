// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>SerializeAsJsonUsingNewtonsoft</c> method to the <see cref="ProducerEndpoint" />.
    /// </summary>
    public static class ProducerEndpointBuilderSerializeAsJsonExtensions
    {
        /// <summary>
        ///     <para>
        ///         Sets the serializer to an instance of <see cref="NewtonsoftJsonMessageSerializer" /> (or
        ///         <see cref="NewtonsoftJsonMessageSerializer{TMessage}" />) to serialize the produced messages as
        ///         JSON.
        ///     </para>
        ///     <para>
        ///         By default this serializer forwards the message type in an header to let the consumer know which
        ///         type has to be deserialized. This approach allows to mix messages of different types in the same
        ///         endpoint and it's ideal when both the producer and the consumer are using Silverback but might not
        ///         be optimal for interoperability. This behavior can be changed using the builder action and
        ///         specifying a fixed message type.
        ///     </para>
        /// </summary>
        /// <typeparam name="TBuilder">
        ///     The actual builder type.
        /// </typeparam>
        /// <param name="endpointBuilder">
        ///     The endpoint builder.
        /// </param>
        /// <param name="serializerBuilderAction">
        ///     An optional <see cref="Action{T}" /> that takes the
        ///     <see cref="INewtonsoftJsonMessageSerializerBuilder" /> and
        ///     configures it.
        /// </param>
        /// <returns>
        ///     The endpoint builder so that additional calls can be chained.
        /// </returns>
        public static TBuilder SerializeAsJsonUsingNewtonsoft<TBuilder>(
            this IProducerEndpointBuilder<TBuilder> endpointBuilder,
            Action<INewtonsoftJsonMessageSerializerBuilder>? serializerBuilderAction = null)
            where TBuilder : IProducerEndpointBuilder<TBuilder>
        {
            Check.NotNull(endpointBuilder, nameof(endpointBuilder));

            var serializerBuilder = new NewtonsoftJsonMessageSerializerBuilder();
            serializerBuilderAction?.Invoke(serializerBuilder);
            endpointBuilder.SerializeUsing(serializerBuilder.Build());

            return (TBuilder)endpointBuilder;
        }
    }
}
