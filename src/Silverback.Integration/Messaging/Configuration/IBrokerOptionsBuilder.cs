// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Exposes the methods to configure the connection with the message broker(s) and add the needed
    ///     services to the <see cref="IServiceCollection" />.
    /// </summary>
    public interface IBrokerOptionsBuilder
    {
        /// <summary>
        ///     Gets the <see cref="ISilverbackBuilder" /> (that in turn references the
        ///     <see cref="IServiceCollection" />).
        /// </summary>
        ISilverbackBuilder SilverbackBuilder { get; }

        /// <summary>
        ///     Gets the <see cref="ILogTemplates"/>.
        /// </summary>
        ILogTemplates LogTemplates { get; }
    }
}
