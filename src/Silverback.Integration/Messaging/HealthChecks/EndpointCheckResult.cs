// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.HealthChecks;

/// <summary>
///     Encapsulates the result of a check performed against a consumer or a producer.
/// </summary>
public class EndpointCheckResult
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointCheckResult" /> class.
    /// </summary>
    /// <param name="endpointName">
    ///     The name of the checked endpoint.
    /// </param>
    /// <param name="isSuccessful">
    ///     A value indicating whether the check was successful.
    /// </param>
    /// <param name="errorMessage">
    ///     The error message, if not successful.
    /// </param>
    public EndpointCheckResult(string endpointName, bool isSuccessful, string? errorMessage = null)
    {
        EndpointName = endpointName;
        IsSuccessful = isSuccessful;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    ///     Gets the name of the checked endpoint.
    /// </summary>
    public string EndpointName { get; }

    /// <summary>
    ///     Gets a value indicating whether the check was successful.
    /// </summary>
    public bool IsSuccessful { get; }

    /// <summary>
    ///     Gets the error message, if not successful.
    /// </summary>
    public string? ErrorMessage { get; }
}
