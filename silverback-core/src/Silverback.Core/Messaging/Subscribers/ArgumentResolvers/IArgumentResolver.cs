using System;
using System.Collections.Generic;
using System.Text;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public interface IArgumentResolver
    {
        bool CanResolve(Type parameterType);
    }
}
