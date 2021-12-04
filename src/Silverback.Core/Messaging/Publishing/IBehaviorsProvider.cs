// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Provides the <see cref="Stack{T}" /> of <see cref="IBehavior" /> to be used in the
///     <see cref="IPublisher" /> pipeline.
/// </summary>
public interface IBehaviorsProvider
{
    /// <summary>
    ///     Creates a new <see cref="Stack{T}" /> of <see cref="IBehavior" /> to be used in the
    ///     <see cref="IPublisher" /> pipeline.
    /// </summary>
    /// <returns>
    ///     The ready-to-use <see cref="Stack{T}" /> of <see cref="IBehavior" />.
    /// </returns>
    Stack<IBehavior> CreateStack();
}
