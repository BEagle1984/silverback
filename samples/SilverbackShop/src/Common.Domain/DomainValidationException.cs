using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Domain
{
    // TODO: Middelware to return this exception as a 400
    public class DomainValidationException : Exception
    {
        public DomainValidationException(string message) : base(message)
        {
        }
    }
}
