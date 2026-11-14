// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Tests.Extended.Integration.TestHost.Mqtt;

// What the broker replays when the persistent session of a disconnected client is resumed: the payloads of the deliveries that were
// never acknowledged and the number of PUBREL packets sent again for the QoS 2 deliveries that were never completed
public sealed record SessionReplay(bool IsSessionPresent, IReadOnlyList<byte[]> Payloads, int Releases);
