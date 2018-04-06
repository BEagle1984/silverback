using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestCommandOne : ICommand
    {
        public string Message { get; set; }
    }
}