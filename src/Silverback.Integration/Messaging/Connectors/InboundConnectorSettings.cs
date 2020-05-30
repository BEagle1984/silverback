// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Batch;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Connectors
{
    /// <summary>
    ///     The inbound connector settings such as batch consuming and number of parallel consumers.
    /// </summary>
    public class InboundConnectorSettings : IValidatableEndpointSettings
    {
        /// <summary>
        ///     Gets or sets the batch settings. Can be used to enable and setup batch processing.
        /// </summary>
        public BatchSettings Batch { get; set; } = new BatchSettings();

        /// <summary>
        ///     Gets or sets the number of parallel consumers to be instantiated. The default is 1.
        /// </summary>
        public int Consumers { get; set; } = 1;

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public void Validate()
        {
            Batch?.Validate();

            if (Consumers < 1)
                throw new EndpointConfigurationException("Consumers must be greater or equal to 1.");
        }
    }
}
