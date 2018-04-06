using Silverback.Messaging.Messages;

namespace Silverback.Tests.TestTypes.Domain
{
    public class TestCommandTwo : ICommand
    {
        public string Message { get; set; }
    }
}