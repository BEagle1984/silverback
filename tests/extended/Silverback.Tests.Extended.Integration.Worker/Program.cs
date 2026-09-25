// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Tests.Extended.Integration.Worker.Kafka;

if (Environment.GetEnvironmentVariable("SCENARIO") == "reconciliation")
    await ReconciliationConsumer.RunAsync();
else
    await MixedWorkload.RunAsync(args);
