using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// An object that is connected to a specific <see cref="IEndpoint"/> through an <see cref="IBroker"/>.
    /// This is the base class of both <see cref="Consumer"/> and <see cref="Producer"/>.
    /// </summary>
    public abstract class EndpointConnectedObject : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EndpointConnectedObject"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        protected EndpointConnectedObject(IBroker broker, IEndpoint endpoint)
        {
            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Serializer = broker.GetSerializer();
        }

        /// <summary>
        /// Gets the associated <see cref="IBroker"/>.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        /// Gets the endpoint this instance is connected to.
        /// </summary>
        public IEndpoint Endpoint { get; }

        /// <summary>
        /// Gets the message serializer to be used.
        /// </summary>
        public IMessageSerializer Serializer { get; }

        #region IDisposable

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        internal bool IsDisposed { get; private set; }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            IsDisposed = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}