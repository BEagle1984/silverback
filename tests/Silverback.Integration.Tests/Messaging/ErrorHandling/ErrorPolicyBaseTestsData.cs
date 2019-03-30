// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.TestTypes.Domain;

namespace Silverback.Tests.Integration.Messaging.ErrorHandling
{
    public class ApplyTo_TestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new ArgumentException(), true };
            yield return new object[] { new ArgumentOutOfRangeException(), true };
            yield return new object[] { new InvalidCastException(), true };
            yield return new object[] { new FormatException(), false };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class Exclude_TestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new ArgumentException(), false };
            yield return new object[] { new ArgumentOutOfRangeException(), false };
            yield return new object[] { new InvalidCastException(), false };
            yield return new object[] { new FormatException(), true };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ApplyToAndExclude_TestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new ArgumentException(), true };
            yield return new object[] { new ArgumentNullException(), true };
            yield return new object[] { new ArgumentOutOfRangeException(), false };
            yield return new object[] { new InvalidCastException(), false };
            yield return new object[] { new FormatException(), true };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ApplyWhen_TestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new FailedMessage(new TestEventOne(), 3), new ArgumentException(), true };
            yield return new object[] { new FailedMessage(new TestEventOne(), 6), new ArgumentException(), false };
            yield return new object[] { new FailedMessage(new TestEventOne(), 3), new ArgumentException("no"), false };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}