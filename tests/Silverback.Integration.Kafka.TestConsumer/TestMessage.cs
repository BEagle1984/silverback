// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Silverback.Integration.Kafka.Messages
{
    public class TestMessage : IMessage
    {
        public Guid Id { get; set; }

        public string Type { get; set; }

        public string Text { get; set; }
    }

    public class DiagnosticObserver : IObserver<KeyValuePair<string, object>>
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

    public class DiagnosticListenerObserver : IObserver<DiagnosticListener>
    {
        private readonly DiagnosticObserver _diagnosticObserver;

        public DiagnosticListenerObserver(DiagnosticObserver diagnosticObserver)
        {
            _diagnosticObserver = diagnosticObserver;
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
                case "SilverbackConsumerDiagnosticListener":
                    value.Subscribe(_diagnosticObserver);
                    break;
            }
        }
    }
}