using Silverback.Messaging;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Core.Tests.TestTypes.Handlers
{
    public class TestCommandOneHandler : MessageHandler<TestCommandOne>
    {
        public static int Counter { get; set; }

        public override void Handle(TestCommandOne message)
        {
            Counter++;
        }
    }
}