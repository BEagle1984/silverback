// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Subscribers
{
    public class SubscribedMethodInvocationException : SilverbackException
    {
        public SubscribedMethodInvocationException()
        {
        }

        public SubscribedMethodInvocationException(MethodInfo methodInfo, string message)
            : base($"Cannot invoke the subscribed method {methodInfo.Name} " +
                   $"in type {methodInfo.DeclaringType.FullName}. --> " +
                   message)
        {
        }

        public SubscribedMethodInvocationException(string message) : base(message)
        {
        }

        public SubscribedMethodInvocationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SubscribedMethodInvocationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}