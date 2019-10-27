// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Configuration
{
    public class SilverbackConfigurationException : SilverbackException
    {
        public SilverbackConfigurationException()
        {
        }

        public SilverbackConfigurationException(string message) : base(message)
        {
        }

        public SilverbackConfigurationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SilverbackConfigurationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
