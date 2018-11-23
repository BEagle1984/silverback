using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public interface ITestMessage : IMessage
    {
        string Message { get; }
    }
}