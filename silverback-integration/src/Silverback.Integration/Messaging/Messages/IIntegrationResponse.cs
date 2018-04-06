namespace Silverback.Messaging.Messages
{
    /// <summary>
    /// Reprensent the response received to an <see cref="IIntegrationRequest"/>.
    /// </summary>
    /// <seealso cref="IMessage" />
    public interface IIntegrationResponse : IResponse, IIntegrationMessage
    {
    }
}