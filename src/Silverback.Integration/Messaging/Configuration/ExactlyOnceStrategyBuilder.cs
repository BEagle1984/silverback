// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ExactlyOnce;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="IExactlyOnceStrategy" />.
    /// </summary>
    public class ExactlyOnceStrategyBuilder : IExactlyOnceStrategyBuilder
    {
        private IExactlyOnceStrategy? _strategy;

        /// <inheritdoc cref="IExactlyOnceStrategyBuilder.StoreOffsets"/>>
        public IExactlyOnceStrategyBuilder StoreOffsets()
        {
            if (_strategy != null)
                throw new InvalidOperationException("Cannot use multiple IExactlyOnceStrategy.");

            _strategy = new OffsetStoreExactlyOnceStrategy();
            return this;
        }

        /// <inheritdoc cref="IExactlyOnceStrategyBuilder.LogMessages"/>>
        public IExactlyOnceStrategyBuilder LogMessages()
        {
            if (_strategy != null)
                throw new InvalidOperationException("Cannot use multiple IExactlyOnceStrategy.");

            _strategy = new LogExactlyOnceStrategy();
            return this;
        }

        /// <summary>
        ///     Builds the <see cref="IExactlyOnceStrategy" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="IExactlyOnceStrategy" />.
        /// </returns>
        public IExactlyOnceStrategy Build()
        {
            if (_strategy == null)
                throw new InvalidOperationException("No strategy was specified.");

            return _strategy;
        }
    }
}
