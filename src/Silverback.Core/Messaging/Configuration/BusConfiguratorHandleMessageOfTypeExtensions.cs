// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c> HandleMessageOfType </c> methods to the <see cref="IBusConfigurator" />.
    /// </summary>
    public static class BusConfiguratorHandleMessageOfTypeExtensions
    {
        /// <summary>
        ///     Configures the type <typeparamref name="TMessage" /> to be recognized
        ///     as a message to enable features like automatic republishing.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to be configured.
        /// </param>
        /// <typeparam name="TMessage"> The (base) message type. </typeparam>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator HandleMessagesOfType<TMessage>(this IBusConfigurator busConfigurator) =>
            busConfigurator.HandleMessagesOfType(typeof(TMessage));

        /// <summary>
        ///     Configures the specified type to be recognized
        ///     as a message to enable features like automatic republishing.
        /// </summary>
        /// <param name="busConfigurator">
        ///     The <see cref="IBusConfigurator" /> that references the <see cref="BusOptions" /> to be configured.
        /// </param>
        /// <param name="messageType"> The (base) message type. </param>
        /// <returns>
        ///     The <see cref="IBusConfigurator" /> so that additional calls can be chained.
        /// </returns>
        public static IBusConfigurator HandleMessagesOfType(this IBusConfigurator busConfigurator, Type messageType)
        {
            if (busConfigurator == null)
                throw new ArgumentNullException(nameof(busConfigurator));

            if (!busConfigurator.BusOptions.MessageTypes.Contains(messageType))
                busConfigurator.BusOptions.MessageTypes.Add(messageType);

            return busConfigurator;
        }
    }
}
