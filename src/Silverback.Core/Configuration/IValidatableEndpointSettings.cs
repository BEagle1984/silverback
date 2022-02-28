// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Configuration;

/// <summary>
///     Declares the <see cref="Validate"/> method that is used to check the provided settings.
/// </summary>
public interface IValidatableSettings
{
    /// <summary>
    ///     Throws a <see cref="SilverbackConfigurationException" /> if the configuration is not valid.
    /// </summary>
    void Validate();
}
