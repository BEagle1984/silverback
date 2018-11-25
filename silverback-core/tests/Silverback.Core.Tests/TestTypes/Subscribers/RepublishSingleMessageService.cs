// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class RepublishMessagesTestService : ISubscriber
    {
        [Subscribe]
        private TestCommandOne OnCommandReceived(TestEventOne message) => new TestCommandOne();

        [Subscribe]
        private IEnumerable<ICommand> OnCommandReceived(TestEventTwo message) => new ICommand[] { new TestCommandOne(), new TestCommandTwo () };
    }
}
