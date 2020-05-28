// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers
{
    /// <summary>
    ///     These types are used to handle the subscribed methods return values (e.g. to republish the returned
    ///     messages).
    /// </summary>
    public interface IReturnValueHandler
    {
        /// <summary>
        ///     Returns a boolean value indicating whether this handler can handle the specified value.
        /// </summary>
        /// <param name="returnValue">
        ///     The value to be handled.
        /// </param>
        /// <returns>
        ///     A boolean value indicating whether the value can be handled.
        /// </returns>
        bool CanHandle(object returnValue);

        /// <summary>
        ///     Handles the specified return value.
        /// </summary>
        /// <param name="returnValue">
        ///     The value to be handled.
        /// </param>
        void Handle(object returnValue);

        /// <summary>
        ///     Handles the specified return value.
        /// </summary>
        /// <param name="returnValue">
        ///     The value to be handled.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task HandleAsync(object returnValue);
    }
}
