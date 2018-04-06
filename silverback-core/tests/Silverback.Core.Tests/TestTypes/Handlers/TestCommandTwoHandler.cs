using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Core.Tests.TestTypes.Handlers
{
    public class TestCommandTwoHandler : MessageHandler<TestCommandTwo>
    {
        public static int Counter { get; set; }

        public override void Handle(TestCommandTwo message)
        {
            Counter++;
        }
    }
}