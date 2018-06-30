using System;
using System.Threading.Tasks;
using Silverback.Extensions;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    /// Subscribes to the messages published in a bus and forward them to 
    /// all <see cref="ISubscriber"/> of type <typeparamref name="TSubscriber"/>.
    /// thenewly instantiated for every single message.
    /// </summary>
    /// <typeparam name="TSubscriber">The type of the <see cref="ISubscriber"/> to be instanciated to handle the messages.</typeparam>
    /// <seealso cref="ISubscriber" />
    public class SubscriberFactory<TSubscriber> : ISubscriber
        where TSubscriber : ISubscriber
    {
        private readonly ITypeFactory _typeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubscriberFactory{THandler}" /> class.
        /// </summary>
        /// <param name="typeFactory">The <see cref="ITypeFactory" /> that will be used to get an <see cref="ISubscriber" /> instance to process each received message.</param>
        public SubscriberFactory(ITypeFactory typeFactory)
        {
            _typeFactory = typeFactory ?? throw new ArgumentNullException(nameof(typeFactory));
        }

        /// <summary>
        /// Gets a new subscriber instance.
        /// </summary>
        /// <returns></returns>
        private TSubscriber[] GetSubscribers()
        {
            var subscribers = _typeFactory.GetInstances<TSubscriber>();

            if (subscribers == null)
                throw new InvalidOperationException($"Couldn't instantiate subscriber of type {typeof(TSubscriber)}.");

            return subscribers;
        }

        /// <summary>
        /// Called when a message is published, forwards the message to a new instance of <typeparamref name="TSubscriber"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        public void OnNext(IMessage message)
            => GetSubscribers().ForEach(s => s.OnNext(message));

        /// <summary>
        /// Called when a message is published asynchronously, forwards the message to a new instance of <typeparamref name="TSubscriber"/>.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public Task OnNextAsync(IMessage message)
            => GetSubscribers().ForEachAsync(s => s.OnNextAsync(message));
    }
}