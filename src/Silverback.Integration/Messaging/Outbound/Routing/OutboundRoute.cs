// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Outbound.Routing;

internal record OutboundRoute(Type MessageType, ProducerConfiguration ProducerConfiguration) : IOutboundRoute;
