using System;
using System.Runtime.Serialization;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// The exception that is thrown after the whole error policy pipeline 
    /// has been applied but still failed to finally successfully process the message.
    /// </summary>
    public class ErrorPolicyException : Exception
    {
        public ErrorPolicyException()
        {
        }

        public ErrorPolicyException(string message) : base(message)
        {
        }

        public ErrorPolicyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ErrorPolicyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}