// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     An <see cref="ISilverbackLogger{TCategoryName}" /> with some specific methods to log outbound messages
    ///     related events.
    /// </summary>
    /// <typeparam name="TCategoryName">
    ///     The type who's name is used for the logger category name.
    /// </typeparam>
    public interface IOutboundLogger<out TCategoryName> : ISilverbackLogger<TCategoryName>
    {
        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.MessageProduced" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IOutboundEnvelope" />.
        /// </param>
        void LogProduced(IOutboundEnvelope envelope);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.MessageWrittenToOutbox" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IOutboundEnvelope" />.
        /// </param>
        void LogWrittenToOutbox(IOutboundEnvelope envelope);

        /// <summary>
        ///     Logs the <see cref="IntegrationLogEvents.ErrorProducingOutboxStoredMessage" /> event.
        /// </summary>
        /// <param name="envelope">
        ///     The <see cref="IOutboundEnvelope" />.
        /// </param>
        /// <param name="exception">
        ///     The <see cref="Exception" />.
        /// </param>
        void LogErrorProducingOutboxStoredMessage(IOutboundEnvelope envelope, Exception exception);
    }
}
