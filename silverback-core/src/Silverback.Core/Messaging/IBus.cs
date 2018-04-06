using System;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging
{
    /// <summary>
    /// A publish-subscribe based observable bus.
    /// </summary>
    public interface IBus
    {
        #region Publish

        /// <summary>
        /// Publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        void Publish(IMessage message);

        /// <summary>
        /// Asynchronously publishes the specified message.
        /// </summary>
        /// <param name="message">The message to be published.</param>
        /// <returns></returns>
        Task PublishAsync(IMessage message);

        #endregion

        #region Subscribe / Unsubscribe

        /// <summary>
        /// Subscribes to the messages stream. The function should return an <see cref="IDisposable"/> 
        /// to let the <see cref="IBus"/> handle the subscriber lifecycle.
        /// </summary>
        /// <param name="subscription">The method performing the subscription.</param>
        void Subscribe(Action<IObservable<IMessage>> subscription);

        /// <summary>
        /// Subscribes to the messages stream. The function must return an <see cref="IDisposable" />
        /// to let the <see cref="IBus" /> handle the subscriber lifecycle.
        /// </summary>
        /// <param name="subscription">The method performing the subscription.</param>
        /// <returns>
        /// Returns the subscriber.
        /// </returns>
        IDisposable Subscribe(Func<IObservable<IMessage>, IDisposable> subscription);

        /// <summary>
        /// Dispose the specified subscriber.
        /// </summary>
        /// <param name="subscriber">The subscriber.</param>
        void Unsubscribe(IDisposable subscriber);

        #endregion
    }
}
