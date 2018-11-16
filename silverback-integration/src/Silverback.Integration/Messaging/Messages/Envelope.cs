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
        // TODO: Allow customization?
        private static readonly Lazy<string> SourceValue =
            new Lazy<string>(() => Assembly.GetEntryAssembly().GetName().Name);

        public Envelope() { }

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

        public IIntegrationMessage Message { get; set; }

        public static Envelope Create(IIntegrationMessage message)
        {
            return new Envelope(message) { Source = SourceValue.Value };
        }
    }
}