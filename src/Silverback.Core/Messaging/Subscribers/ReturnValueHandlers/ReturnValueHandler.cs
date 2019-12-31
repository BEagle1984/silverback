// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    public class ReturnValueHandler
    {
        private readonly IEnumerable<IReturnValueHandler> _returnValueHandlers;

        public ReturnValueHandler(IEnumerable<IReturnValueHandler> returnValueHandlers)
        {
            // Revert the handlers order, to give priority to the ones added after the 
            // default ones.
            _returnValueHandlers = returnValueHandlers.Reverse();
        }

        public async Task<IEnumerable<object>> HandleReturnValues(IEnumerable<object> returnValues, bool executeAsync)
        {
            var unhandledReturnValues = new List<object>();
            foreach (var returnValue in returnValues.Where(v => v != null && v.GetType().Name != "VoidTaskResult"))
            {
                var handler = _returnValueHandlers.FirstOrDefault(h => h.CanHandle(returnValue));

                if (handler != null)
                {
                    if (executeAsync)
                        await handler.HandleAsync(returnValue);
                    else
                        handler.Handle(returnValue);
                }
                else
                {
                    unhandledReturnValues.Add(returnValue);
                }
            }

            return unhandledReturnValues;
        }
    }
}