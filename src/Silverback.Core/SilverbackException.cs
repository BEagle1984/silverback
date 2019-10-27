// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback
{
    public class SilverbackException : Exception
    {
        public SilverbackException()
        {
        }

        public SilverbackException(string message) : base(message)
        {
        }

        public SilverbackException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SilverbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}