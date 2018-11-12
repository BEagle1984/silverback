using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Messages
{
    public class TestCommandOne : ICommand, ITestMessage
    {
        public string Message { get; set; }
    }
}