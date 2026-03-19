// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Publishing;

internal partial class ApplicationPublisher : IApplicationPublisher
{
    private readonly IPublisher _publisher;

    public ApplicationPublisher(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public ISilverbackContext Context => _publisher.Context;
}
