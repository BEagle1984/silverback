using System.Collections.Generic;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The envelope enclosing the <see cref="IIntegrationMessage"/> that is sent over a message broker.
    /// </summary>
    // TODO: Maybe add type parameter?
    public interface IEnvelope
    {
        /// <summary>
        /// Gets or sets a string identifying the source of the <see cref="IMessage"/>.
        /// It usually contains the name of the microservice that generated the <see cref="IMessage"/>.
        /// </summary>
        string Source { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        IIntegrationMessage Message { get; set; }

        // TODO: Uncomment if we implement automated retry
        ///// <summary>
        ///// Gets or sets the current try count, automatically incremented with each retry.
        ///// </summary>
        //int TryCount { get; set; }

        ///// <summary>
        ///// Gets or sets a value indicating whether the maximum alotted retries count has been reached.
        ///// </summary>
        //bool IsLastTry { get; set; }
    }
}