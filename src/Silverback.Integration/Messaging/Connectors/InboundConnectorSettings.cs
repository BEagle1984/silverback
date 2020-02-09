﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Batch;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors
{
    public class InboundConnectorSettings
    {
        /// <summary>
        ///     The batch settings. Can be used to enable and setup batch processing.
        /// </summary>
        public BatchSettings Batch { get; set; } = new BatchSettings();

        /// <summary>
        ///     The number of parallel consumers. The default is 1.
        /// </summary>
        public int Consumers { get; set; } = 1;

        /// <summary>
        ///     When set to <c>true</c> the incoming messages will be unwrapped (and published twice to the internal bus),
        ///     otherwise they will only be published wrapped into an <see cref="IInboundMessage{TContent}" />. Default is
        ///     <c>true</c>.
        /// </summary>
        public bool UnwrapMessages { get; set; } = true;

        public void Validate()
        {
            if (Batch == null)
                Batch = new BatchSettings();

            Batch.Validate();

            if (Consumers < 1)
                throw new EndpointConfigurationException("Consumers must be greater or equal to 1.");
        }
    }
}