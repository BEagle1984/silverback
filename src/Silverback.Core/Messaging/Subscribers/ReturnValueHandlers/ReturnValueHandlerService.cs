// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    /// <summary>
    ///     Calls the registered <see cref="IReturnValueHandler" />'s.
    /// </summary>
    // TODO: Test
    internal class ReturnValueHandlerService
    {
        private readonly IReadOnlyCollection<IReturnValueHandler> _returnValueHandlers;

        public ReturnValueHandlerService(IEnumerable<IReturnValueHandler> returnValueHandlers)
        {
            // Revert the handlers order, to give priority to the ones added after the
            // default ones.
            _returnValueHandlers = returnValueHandlers.Reverse().ToList();
        }

        public async Task<IReadOnlyCollection<object>> HandleReturnValues(
            IReadOnlyCollection<object?> returnValues,
            bool executeAsync)
        {
            var unhandledReturnValues = new List<object>();
            foreach (var returnValue in returnValues)
            {
                if (returnValue == null || returnValue.GetType().Name == "VoidTaskResult")
                    continue;

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
