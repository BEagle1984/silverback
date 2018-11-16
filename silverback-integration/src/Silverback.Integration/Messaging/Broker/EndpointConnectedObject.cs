using System;
using Silverback.Messaging.Serialization;

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

        public IMessageSerializer Serializer => Broker?.Serializer;
    }
}