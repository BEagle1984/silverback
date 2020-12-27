// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    /// <summary>
    ///     Routes the outbound messages to one or multiple endpoints.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be routed.
    /// </typeparam>
    /// <typeparam name="TEndpoint">
    ///     The type of the <see cref="IProducerEndpoint" />.
    /// </typeparam>
    public class DictionaryOutboundRouter<TMessage, TEndpoint> : OutboundRouter<TMessage>
        where TEndpoint : IProducerEndpoint
    {
        private readonly IReadOnlyDictionary<string, TEndpoint> _endpoints;

        private readonly RouterFunction _routerFunction;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DictionaryOutboundRouter{TMessage, TEndpoint}" /> class.
        /// </summary>
        /// <param name="routerFunction">
        ///     The <see cref="SingleEndpointRouterFunction" />.
        /// </param>
        /// <param name="endpoints">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the endpoints and their key.
        /// </param>
        public DictionaryOutboundRouter(
            SingleEndpointRouterFunction routerFunction,
            IReadOnlyDictionary<string, TEndpoint> endpoints)
            : this(
                (message, headers, endpointsDictionary) =>
                    new[] { routerFunction.Invoke(message, headers, endpointsDictionary) },
                endpoints)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DictionaryOutboundRouter{TMessage, TEndpoint}" /> class.
        /// </summary>
        /// <param name="routerFunction">
        ///     The <see cref="RouterFunction" />.
        /// </param>
        /// <param name="endpoints">
        ///     The <see cref="IReadOnlyDictionary{TKey,TValue}" /> containing the endpoints and their key.
        /// </param>
        public DictionaryOutboundRouter(
            RouterFunction routerFunction,
            IReadOnlyDictionary<string, TEndpoint> endpoints)
        {
            _routerFunction = Check.NotNull(routerFunction, nameof(routerFunction));
            _endpoints = Check.NotNull(endpoints, nameof(endpoints));
        }

        /// <summary>
        ///     The actual router method that receives the message (including its headers) together with the
        ///     dictionary containing all endpoints and returns the destination endpoints.
        /// </summary>
        /// <param name="message">
        ///     The message to be routed.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpoints">
        ///     The dictionary containing all configured endpoints for this router.
        /// </param>
        /// <returns>
        ///     The destination endpoints.
        /// </returns>
        public delegate IEnumerable<TEndpoint> RouterFunction(
            TMessage message,
            MessageHeaderCollection headers,
            IReadOnlyDictionary<string, TEndpoint> endpoints);

        /// <summary>
        ///     The actual router method that receives the message (including its headers) together with the
        ///     dictionary containing all endpoints and returns the destination endpoint.
        /// </summary>
        /// <param name="message">
        ///     The message to be routed.
        /// </param>
        /// <param name="headers">
        ///     The message headers.
        /// </param>
        /// <param name="endpoints">
        ///     The dictionary containing all configured endpoints for this router.
        /// </param>
        /// <returns>
        ///     The destination endpoint.
        /// </returns>
        public delegate TEndpoint SingleEndpointRouterFunction(
            TMessage message,
            MessageHeaderCollection headers,
            IReadOnlyDictionary<string, TEndpoint> endpoints);

        /// <inheritdoc cref="IOutboundRouter.Endpoints" />
        public override IEnumerable<IProducerEndpoint> Endpoints =>
            (IEnumerable<IProducerEndpoint>)_endpoints.Values;

        /// <inheritdoc cref="IOutboundRouter{TMessage}.GetDestinationEndpoints(TMessage,MessageHeaderCollection)" />
        public override IEnumerable<IProducerEndpoint> GetDestinationEndpoints(
            TMessage message,
            MessageHeaderCollection headers) =>
            (IEnumerable<IProducerEndpoint>)_routerFunction.Invoke(message, headers, _endpoints);
    }
}
