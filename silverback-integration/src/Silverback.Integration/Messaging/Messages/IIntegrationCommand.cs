namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Represent a command message that is sent to another service through a message broker.
    /// </summary>
    /// <seealso cref="IMessage" />
    public interface IIntegrationCommand : ICommand, IIntegrationMessage
    {
    }
}