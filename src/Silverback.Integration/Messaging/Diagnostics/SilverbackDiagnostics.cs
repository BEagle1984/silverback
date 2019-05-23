// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics;

namespace Silverback.Messaging.Diagnostics
{
    public static class SilverbackDiagnostics
    {
        public static void Enable()
        {
            DiagnosticListener.AllListeners.Subscribe(new AllSilverbackDiagnosticListenerObserver());
        }
    }
}