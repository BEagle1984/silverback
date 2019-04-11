// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Runtime.Serialization;

namespace Silverback
{
    public class SilverbackConcurrencyException : Exception
    {
        public SilverbackConcurrencyException()
        {
        }

        public SilverbackConcurrencyException(string message) : base(message)
        {
        }

        public SilverbackConcurrencyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SilverbackConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}