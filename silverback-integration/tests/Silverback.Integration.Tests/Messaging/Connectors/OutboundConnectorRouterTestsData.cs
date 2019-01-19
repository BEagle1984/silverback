using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.Messaging.Connectors
{
    public class OnMessageReceived_MultipleMessages_CorrectlyRoutedToEndpoints_TestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            // event 
            yield return new object[]{new TestEventOne(), new[] { "allMessages", "allEvents", "eventOne" }};
            yield return new object[] { new TestEventTwo(), new[] { "allMessages", "allEvents", "eventTwo" }};
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
