using System;
using System.Collections.Generic;
using System.Reflection;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// The envelope enclosing the <see cref="IIntegrationMessage" /> that is sent over a message broker.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Messages.IEnvelope" />
    public class Envelope : IEnvelope
    {
        private static readonly Lazy<string> SourceValue = new Lazy<string>(() =>
        {
            // TODO: Allow customization?
            return Assembly.GetEntryAssembly().GetName().Name;
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class.
        /// </summary>
        public Envelope()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Envelope"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public Envelope(IIntegrationMessage message)
        {
            Message = message;

            if (Message.Id == default)
                Message.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Gets or sets a string identifying the source of the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// By default contains the name of the assembly that generated the <see cref="T:Silverback.Messaging.Messages.IMessage" />.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public IIntegrationMessage Message { get; set; }

        /// <summary>
        /// Creates a new envelope enclosing the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        public static Envelope Create(IIntegrationMessage message)
        { 
            return new Envelope(message) { Source = SourceValue.Value };
        }
    }
}