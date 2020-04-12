// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    public static class RabbitMessageHeaders
    {
        /// <summary>
        ///     The header that will be filled with the routing key (if defined via
        ///     <see cref="RabbitRoutingKeyAttribute"/> for the message being produced).
        /// </summary>
        public const string RoutingKey = "x-rabbit-routing-key";
    }
}