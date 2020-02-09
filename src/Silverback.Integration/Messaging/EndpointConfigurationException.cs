﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback.Messaging
{
    public class EndpointConfigurationException : SilverbackException
    {
        public EndpointConfigurationException()
        {
        }

        public EndpointConfigurationException(string message)
            : base(message)
        {
        }

        public EndpointConfigurationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected EndpointConfigurationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}