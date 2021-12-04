// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Declares the <c>Validate</c> method that is used to check the provided settings.
/// </summary>
public interface IValidatableEndpointSettings
{
    /// <summary>
    ///     Throws an <see cref="EndpointConfigurationException" /> if the current configuration is not valid.
    /// </summary>
    void Validate();
}
