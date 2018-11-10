using System;
using System.Runtime.Serialization;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// The exception that is thrown after the whole error policy pipeline 
    /// has been applied but still failed to finally successfully process the message.
    /// </summary>
    /// <seealso cref="System.Exception" />
    public class ErrorPolicyException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPolicyException"/> class.
        /// </summary>
        public ErrorPolicyException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPolicyException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ErrorPolicyException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPolicyException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public ErrorPolicyException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPolicyException"/> class.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"></see> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext"></see> that contains contextual information about the source or destination.</param>
        protected ErrorPolicyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}