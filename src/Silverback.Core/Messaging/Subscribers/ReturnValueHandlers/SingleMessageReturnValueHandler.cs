// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers.ReturnValueHandlers;

/// <summary>
///     Handles the returned message republishing it.
/// </summary>
public class SingleMessageReturnValueHandler : IReturnValueHandler
{
    private readonly MediatorOptions _mediatorOptions;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SingleMessageReturnValueHandler" /> class.
    /// </summary>
    /// <param name="mediatorOptions">
    ///     The <see cref="MediatorOptions" /> that specify which message types have to be handled.
    /// </param>
    public SingleMessageReturnValueHandler(MediatorOptions mediatorOptions)
    {
        _mediatorOptions = mediatorOptions;
    }

    /// <inheritdoc cref="IReturnValueHandler.CanHandle" />
    public bool CanHandle(object returnValue) =>
        returnValue != null &&
        _mediatorOptions.MessageTypes.Any(type => type.IsInstanceOfType(returnValue));

    /// <inheritdoc cref="IReturnValueHandler.Handle" />
    public void Handle(IPublisher publisher, object returnValue) =>
        Check.NotNull(publisher, nameof(publisher)).Publish<object>(returnValue);

    /// <inheritdoc cref="IReturnValueHandler.HandleAsync" />
    public async ValueTask HandleAsync(IPublisher publisher, object returnValue) =>
        await Check.NotNull(publisher, nameof(publisher)).PublishAsync<object>(returnValue).ConfigureAwait(false);
}
