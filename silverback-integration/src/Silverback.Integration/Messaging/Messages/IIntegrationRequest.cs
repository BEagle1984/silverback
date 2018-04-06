namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Reprensent a request message awaiting a response (see <see cref="IResponse"/>).
    /// This request is sent to another service through a message broker. 
    /// </summary>
    /// <seealso cref="IMessage" />
    public interface IIntegrationRequest : IRequest, IIntegrationMessage
    {
    }
}