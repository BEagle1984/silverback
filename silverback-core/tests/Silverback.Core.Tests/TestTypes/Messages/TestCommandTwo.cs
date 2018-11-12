using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestCommandTwo : ICommand, ITestMessage
    {
        public string Message { get; set; }
    }
}