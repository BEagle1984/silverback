// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Diagnostics
{
    /// <summary>
    ///     Creates a <see cref="IBrokerActivityEnricher" /> for a given endpoint.
    /// </summary>
    public interface IActivityEnricherFactory
    {
        /// <summary>
        ///     Returns a <see cref="IBrokerActivityEnricher" /> for the given endpoint type.
        /// </summary>
        /// <param name="endpointType">
        ///     The endpoint type.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerActivityEnricher" /> that matches the specified endpoint type.
        /// </returns>
        IBrokerActivityEnricher GetActivityEnricher(Type endpointType);
    }
}
