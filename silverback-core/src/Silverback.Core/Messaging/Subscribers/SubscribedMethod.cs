// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethod
    {
        public ISubscriber Instance { get; set; }
        public MethodInfo MethodInfo { get; set; }
        public ParameterInfo[] Parameters { get; set; }
        public Type SubscribedMessageType { get; set; }
    }
}