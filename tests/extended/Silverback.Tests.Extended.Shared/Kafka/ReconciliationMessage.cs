// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Extended.Shared.Kafka;

public sealed record ReconciliationMessage(int Partition, int Sequence);
