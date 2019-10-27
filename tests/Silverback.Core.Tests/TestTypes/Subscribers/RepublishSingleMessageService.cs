// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class RepublishMessagesTestService : ISubscriber
    {
        [Subscribe]
        private TestCommandOne OnCommandReceived(TestEventOne message) => new TestCommandOne();

        [Subscribe]
        private IEnumerable<ICommand> OnCommandReceived(TestEventTwo message) => new ICommand[] { new TestCommandOne(), new TestCommandTwo () };
    }
}
