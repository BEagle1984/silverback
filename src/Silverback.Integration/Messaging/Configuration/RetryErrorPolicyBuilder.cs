// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="RetryErrorPolicy" />.
/// </summary>
public class RetryErrorPolicyBuilder : ErrorPolicyBaseBuilder<RetryErrorPolicyBuilder>
{
    private TimeSpan _initialDelay = TimeSpan.Zero;

    private TimeSpan _delayIncrement = TimeSpan.Zero;

    private int? _maxFailedAttempts;

    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.This" />
    protected override RetryErrorPolicyBuilder This => this;

    /// <summary>
    ///     Specifies the interval between retries.
    /// </summary>
    /// <param name="interval">
    ///     The interval between retries.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public RetryErrorPolicyBuilder WithInterval(TimeSpan interval)
    {
        _initialDelay = Check.GreaterThan(interval, nameof(interval), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Specifies an initial delay before the first retry and an increment to be added at each subsequent retry.
    /// </summary>
    /// <param name="initialDelay">
    ///     The initial delay before the first retry.
    /// </param>
    /// <param name="delayIncrement">
    ///     The increment to be added at each subsequent retry.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public RetryErrorPolicyBuilder WithIncrementalDelay(TimeSpan initialDelay, TimeSpan delayIncrement)
    {
        _initialDelay = Check.GreaterThan(initialDelay, nameof(initialDelay), TimeSpan.Zero);
        _delayIncrement = Check.GreaterThan(delayIncrement, nameof(delayIncrement), TimeSpan.Zero);
        return this;
    }

    /// <summary>
    ///     Sets the number of times this policy should be applied to the same message in case of multiple failed attempts.
    /// </summary>
    /// <param name="retries">
    ///     The number of times this policy should be applied.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public RetryErrorPolicyBuilder WithMaxRetries(int retries)
    {
        _maxFailedAttempts = Check.GreaterOrEqualTo(retries, nameof(retries), 1);
        return this;
    }

    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.BuildCore" />
    protected override ErrorPolicyBase BuildCore() =>
        new RetryErrorPolicy
        {
            InitialDelay = _initialDelay,
            DelayIncrement = _delayIncrement,
            MaxFailedAttempts = _maxFailedAttempts
        };
}
