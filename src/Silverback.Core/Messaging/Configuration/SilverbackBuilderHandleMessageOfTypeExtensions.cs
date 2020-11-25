// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>HandleMessageOfType</c> methods to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderHandleMessageOfTypeExtensions
    {
        /// <summary>
        ///     Configures the type <typeparamref name="TMessage" /> to be recognized as a message to enable
        ///     features like automatic republishing.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage">
        ///     The (base) message type.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder HandleMessagesOfType<TMessage>(this ISilverbackBuilder silverbackBuilder) =>
            silverbackBuilder.HandleMessagesOfType(typeof(TMessage));

        /// <summary>
        ///     Configures the specified type to be recognized as a message to enable features like automatic
        ///     republishing.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IBusOptions" /> to be configured.
        /// </param>
        /// <param name="messageType">
        ///     The (base) message type.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder HandleMessagesOfType(
            this ISilverbackBuilder silverbackBuilder,
            Type messageType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            if (!silverbackBuilder.BusOptions.MessageTypes.Contains(messageType))
                silverbackBuilder.BusOptions.MessageTypes.Add(messageType);

            return silverbackBuilder;
        }
    }
}
