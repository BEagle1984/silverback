// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics;

namespace Silverback.Messaging.Diagnostics
{
    internal class AllSilverbackDiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        private readonly EmptyDiagnosticObserver _diagnosticObserver = new EmptyDiagnosticObserver();

        public AllSilverbackDiagnosticListenerObserver()
        {
        }

        public void OnCompleted()
        {
            // Intentionally do nothing.
        }

        public void OnError(Exception error)
        {
            // Intentionally do nothing.
        }

        public void OnNext(DiagnosticListener value)
        {
            switch (value.Name)
            {
                case DiagnosticsConstants.DiagnosticListenerNameProducer:
                    value.Subscribe(_diagnosticObserver);
                    break;

                case DiagnosticsConstants.DiagnosticListenerNameConsumer:
                    value.Subscribe(_diagnosticObserver);
                    break;
            }
        }
    }
}