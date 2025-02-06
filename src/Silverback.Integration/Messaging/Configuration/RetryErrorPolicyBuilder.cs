// Copyright (c) 2024 Sergio Aquilini
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

    private double _delayFactor = 1.0;

    private TimeSpan? _maxDelay;

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
        _delayIncrement = TimeSpan.Zero;
        _delayFactor = 1.0;
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
    /// <param name="maxDelay">
    ///     The maximum delay to be applied.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public RetryErrorPolicyBuilder WithIncrementalDelay(TimeSpan initialDelay, TimeSpan delayIncrement, TimeSpan? maxDelay = null)
    {
        _initialDelay = Check.GreaterThan(initialDelay, nameof(initialDelay), TimeSpan.Zero);
        _delayIncrement = Check.GreaterThan(delayIncrement, nameof(delayIncrement), TimeSpan.Zero);
        _delayFactor = 1.0;
        _maxDelay = maxDelay.HasValue ? Check.GreaterThan(maxDelay.Value, nameof(maxDelay), TimeSpan.Zero) : null;
        return this;
    }

    /// <summary>
    ///     Specifies an initial delay before the first retry and a factor to be applied to the delay at each retry.
    /// </summary>
    /// <param name="initialDelay">
    ///     The initial delay before the first retry.
    /// </param>
    /// <param name="delayFactor">
    ///     The factor to be applied at each retry.
    /// </param>
    /// <param name="maxDelay">
    ///     The maximum delay to be applied.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public RetryErrorPolicyBuilder WithExponentialDelay(TimeSpan initialDelay, double delayFactor, TimeSpan? maxDelay = null)
    {
        _initialDelay = Check.GreaterThan(initialDelay, nameof(initialDelay), TimeSpan.Zero);
        _delayIncrement = TimeSpan.Zero;
        _delayFactor = Check.GreaterThan(delayFactor, nameof(delayFactor), 0);
        _maxDelay = maxDelay.HasValue ? Check.GreaterThan(maxDelay.Value, nameof(maxDelay), TimeSpan.Zero) : null;
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
            DelayFactor = _delayFactor,
            MaxDelay = _maxDelay,
            MaxFailedAttempts = _maxFailedAttempts
        };
}
