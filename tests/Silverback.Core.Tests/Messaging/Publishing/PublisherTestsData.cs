// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections;
using System.Collections.Generic;
using Silverback.Tests.Core.TestTypes.Messages;

namespace Silverback.Tests.Core.Messaging.Publishing
{
    public class Publish_SubscribedMessage_ReceivedRepublishedMessages_TestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            // event 
            yield return new object[] { new TestEventOne(), 1, 0 };
            yield return new object[] { new TestEventTwo(), 1, 1 };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
