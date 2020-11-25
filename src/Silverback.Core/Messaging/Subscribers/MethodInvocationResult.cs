// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Subscribers
{
    internal class MethodInvocationResult
    {
        public MethodInvocationResult(
            IReadOnlyCollection<object> handledMessages,
            IReadOnlyCollection<object?>? returnValues = null)
        {
            HandledMessages = handledMessages;
            ReturnValues = returnValues ?? Array.Empty<object>();
        }

        public static MethodInvocationResult Empty { get; } =
            new(Array.Empty<object>(), Array.Empty<object>());

        public IReadOnlyCollection<object> HandledMessages { get; }

        public IReadOnlyCollection<object?> ReturnValues { get; }
    }
}
