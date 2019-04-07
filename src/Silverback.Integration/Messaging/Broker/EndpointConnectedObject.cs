// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// An object that is connected to a specific <see cref="IEndpoint"/> through an <see cref="IBroker"/>.
    /// This is the base class of both <see cref="Consumer"/> and <see cref="Producer"/>.
    /// </summary>
    public abstract class EndpointConnectedObject
    {
        protected EndpointConnectedObject(IBroker broker, IEndpoint endpoint)
        {
            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public IBroker Broker { get; }

        public IEndpoint Endpoint { get; }
    }
}