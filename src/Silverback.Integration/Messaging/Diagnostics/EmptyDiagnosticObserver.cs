// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Diagnostics
{
    internal class EmptyDiagnosticObserver : IObserver<KeyValuePair<string, object>>
    {
        public void OnCompleted()
        {
            // Empty because this class is just a marker.
        }

        public void OnError(Exception error)
        {
            // Empty because this class is just a marker.
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            // Empty because this class is just a marker.
        }
    }
}