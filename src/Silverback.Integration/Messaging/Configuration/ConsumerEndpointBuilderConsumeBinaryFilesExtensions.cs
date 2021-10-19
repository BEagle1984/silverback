// TODO: DELETE

// // Copyright (c) 2020 Sergio Aquilini
// // This code is licensed under MIT license (see LICENSE file for details)
//
// using System;
// using Silverback.Messaging.BinaryFiles;
// using Silverback.Messaging.Messages;
// using Silverback.Messaging.Serialization;
// using Silverback.Util;
//
// namespace Silverback.Messaging.Configuration;
//
// /// <summary>
// ///     Adds the <see cref="ConsumeBinaryFiles{TMessage,TConfiguration,TBuilder}" />> method to the <see cref="ConsumerConfigurationBuilder{TMessage,TConfiguration,TBuilder}" />.
// /// </summary>
// public static class ConsumerEndpointBuilderConsumeBinaryFilesExtensions
// {/// <summary>
//     ///     <para>
//     ///         Sets the serializer to an instance of <see cref="BinaryFileMessageSerializer" />
//     ///         (or <see cref="BinaryFileMessageSerializer{TModel}" />) to wrap the consumed binary files into a
//     ///         <see cref="BinaryFileMessage" />.
//     ///     </para>
//     ///     <para>
//     ///         This settings will force the <see cref="BinaryFileMessageSerializer" /> to be used regardless of the message type header.
//     ///     </para>
//     /// </summary>
//     /// <remarks>
//     ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with binary files.
//     /// </remarks>
//     /// <typeparam name="TMessage">
//     ///     The type of the messages being consumed.
//     /// </typeparam>
//     /// <typeparam name="TConfiguration">
//     ///     The type of the configuration being built.
//     /// </typeparam>
//     /// <typeparam name="TBuilder">
//     ///     The actual builder type.
//     /// </typeparam>
//     /// <param name="endpointBuilder">
//     ///     The endpoint builder.
//     /// </param>
//     /// <param name="serializerBuilderAction">
//     ///     An optional <see cref="Action{T}" /> that takes the <see cref="BinaryFileMessageSerializerBuilder" /> and configures it.
//     /// </param>
//     /// <returns>
//     ///     The endpoint builder so that additional calls can be chained.
//     /// </returns>
//     public static TBuilder ConsumeBinaryFiles<TMessage, TConfiguration, TBuilder>(
//         this ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder> endpointBuilder,
//         Action<BinaryFileMessageSerializerBuilder>? serializerBuilderAction = null)
//         where TConfiguration : ConsumerConfiguration
//         where TBuilder : ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder>
//     {
//         Check.NotNull(endpointBuilder, nameof(endpointBuilder));
//
//         BinaryFileMessageSerializerBuilder serializerBuilder = new();
//         serializerBuilderAction?.Invoke(serializerBuilder);
//         return endpointBuilder.DeserializeUsing(serializerBuilder.Build());
//     }
//
//     /// <summary>
//     ///     <para>
//     ///         Sets the serializer to an instance of <see cref="BinaryFileMessageSerializer" />
//     ///         (or <see cref="BinaryFileMessageSerializer{TModel}" />) to wrap the consumed binary files into a
//     ///         <see cref="BinaryFileMessage" />.
//     ///     </para>
//     ///     <para>
//     ///         This settings will force the <see cref="BinaryFileMessageSerializer" /> to be used regardless of the message type header.
//     ///     </para>
//     /// </summary>
//     /// <remarks>
//     ///     This replaces the <see cref="IMessageSerializer" /> and the endpoint will only be able to deal with binary files.
//     /// </remarks>
//     /// <typeparam name="TMessage">
//     ///     The type of the messages being consumed.
//     /// </typeparam>
//     /// <typeparam name="TConfiguration">
//     ///     The type of the configuration being built.
//     /// </typeparam>
//     /// <typeparam name="TBuilder">
//     ///     The actual builder type.
//     /// </typeparam>
//     /// <param name="endpointBuilder">
//     ///     The endpoint builder.
//     /// </param>
//     /// <param name="serializerBuilderAction">
//     ///     An optional <see cref="Action{T}" /> that takes the <see cref="BinaryFileMessageSerializerBuilder" /> and configures it.
//     /// </param>
//     /// <returns>
//     ///     The endpoint builder so that additional calls can be chained.
//     /// </returns>
//     public static TBuilder ConsumeBinaryFiles<TMessage, TConfiguration, TBuilder>(
//         this ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder> endpointBuilder,
//         Action<BinaryFileMessageSerializerBuilder>? serializerBuilderAction = null)
//         where TMessage : IBinaryFileMessage, new()
//         where TConfiguration : ConsumerConfiguration
//         where TBuilder : ConsumerConfigurationBuilder<TMessage, TConfiguration, TBuilder>
//     {
//         Check.NotNull(endpointBuilder, nameof(endpointBuilder));
//
//         BinaryFileMessageSerializerBuilder serializerBuilder = new();
//
//         if (typeof(TMessage) != typeof(object))
//             serializerBuilder.UseModel<TMessage>();
//
//         serializerBuilderAction?.Invoke(serializerBuilder);
//         return endpointBuilder.DeserializeUsing(serializerBuilder.Build());
//     }
// }
