// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Core.Tests.TestTypes.Messages;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.TestTypes.Subscribers
{
    public class RepublishMessagesTestService : ISubscriber
    {
        [Subscribe]
        private TestCommandOne OnCommandReceived(TestEventOne message) => new TestCommandOne();

        [Subscribe]
        private IEnumerable<ICommand> OnCommandReceived(TestEventTwo message) => new ICommand[] { new TestCommandOne(), new TestCommandTwo () };
    }
}
