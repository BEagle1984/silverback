// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Provides enrichment for the logs written in the context of the consumers and producers.
    /// </summary>
    public interface IBrokerLogEnricher
    {
        /// <summary>
        ///     Gets the name of the first additional property.
        /// </summary>
        string AdditionalPropertyName1 { get; }

        /// <summary>
        ///     Gets the name of the second additional property.
        /// </summary>
        string AdditionalPropertyName2 { get; }

        /// <summary>
        ///     Returns the values for the two additional properties.
        /// </summary>
        /// <param name="endpoint">
        ///     The target endpoint.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="brokerMessageIdentifier">
        ///     The message identifier at broker level (e.g. the Kafka offset).
        /// </param>
        /// <returns>
        ///     Returns a tuple containing the values for the two additional properties.
        /// </returns>
        (string? Value1, string? Value2) GetAdditionalValues(
            Endpoint endpoint,
            IReadOnlyCollection<MessageHeader>? headers,
            IBrokerMessageIdentifier? brokerMessageIdentifier);
    }
}
