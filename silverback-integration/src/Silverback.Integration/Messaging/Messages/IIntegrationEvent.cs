namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Represent an event message that is exposed to other services through a message broker.
    /// </summary>
    /// <seealso cref="Silverback.Messaging.Messages.IEvent" />
    /// <seealso cref="Silverback.Messaging.Messages.IIntegrationMessage" />
    /// <seealso cref="IMessage" />
    public interface IIntegrationEvent: IEvent, IIntegrationMessage
    {
    }
}