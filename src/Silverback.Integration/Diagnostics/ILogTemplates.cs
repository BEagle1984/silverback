// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging;

namespace Silverback.Diagnostics
{
    /// <summary>
    ///     Handles the templates of the string appended to the log message by the
    ///     <see cref="ISilverbackIntegrationLogger" /> to log additional message data such as endpoint name,
    ///     message type, etc.
    /// </summary>
    public interface ILogTemplates
    {
        /// <summary>
        ///     Configures the additional log data expected for the specified endpoint type.
        /// </summary>
        /// <typeparam name="TEndpoint">The type of the endpoint.</typeparam>
        /// <param name="additionalData">The keys of the additional log data.</param>
        /// <returns>
        ///     The <see cref="ILogTemplates" /> so that additional calls can be chained.
        /// </returns>
        ILogTemplates ConfigureAdditionalData<TEndpoint>(params string[] additionalData);

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a message consumed from the specified
        ///     endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        string GetInboundMessageLogTemplate(IEndpoint? endpoint);

        /// <summary>
        ///     Gets the additional arguments expected for a message consumed from the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The expected arguments.</returns>
        string[] GetInboundMessageArguments(IEndpoint? endpoint);

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a batch of messages consumed from the
        ///     specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        string GetInboundBatchLogTemplate(IEndpoint? endpoint);

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a message produced to the specified
        ///     endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        string GetOutboundMessageLogTemplate(IEndpoint? endpoint);

        /// <summary>
        ///     Gets the additional arguments expected for a message produced to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The expected arguments.</returns>
        string[] GetOutboundMessageArguments(IEndpoint? endpoint);

        /// <summary>
        ///     Gets the message template to be appended to the logs related to a batch of messages produced to the
        ///     specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns>The message template.</returns>
        string GetOutboundBatchLogTemplate(IEndpoint? endpoint);
    }
}
