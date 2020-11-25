// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Subscribers.Subscriptions
{
    /// <summary>
    ///     The subscription options such as the parallelism settings.
    /// </summary>
    public class SubscriptionOptions
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the method can be executed concurrently to other methods
        ///     handling the <b>same message></b>. The default value is <c>true</c> (the method will be executed
        ///     sequentially to other subscribers).
        /// </summary>
        public bool Exclusive { get; set; } = true;
    }
}
