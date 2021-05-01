// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class RepublishMessagesTestService
    {
        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private TestCommandOne OnEventOne(TestEventOne message) => new();

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1801", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "CA1822", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("", "IDE0051", Justification = Justifications.CalledBySilverback)]
        private IEnumerable<ICommand> OnEventTwo(TestEventTwo message) =>
            new ICommand[] { new TestCommandOne(), new TestCommandTwo() };
    }
}
