// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Configuration
{
    public class ConfigurationException : SilverbackException
    {
        public ConfigurationException()
        {
        }

        public ConfigurationException(string message)
            : base(message)
        {
        }

        public ConfigurationException(string message, Exception innerException)
            : base(message,
                innerException)
        {
        }

        protected ConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info,
                context)
        {
        }
    }
}