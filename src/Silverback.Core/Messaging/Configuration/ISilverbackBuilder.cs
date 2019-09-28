// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// An interface for configuring Silverback services.
    /// </summary>
    public interface ISilverbackBuilder
    {
        IServiceCollection Services { get; }
    }
}